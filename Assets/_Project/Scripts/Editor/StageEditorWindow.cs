using System.Collections.Generic;
using System.IO;
using System.Linq;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;
using UnityEditor;
using UnityEngine;

namespace PuzzleKingdoms.EditorTools
{
    /// <summary>
    /// PB-19: makes a stage without hand-editing assets. Pick a stage on the left; on the right edit its
    /// numbers, goal and enemies, paint the start board, test its difficulty with the balance bots
    /// (the same <see cref="BalanceRunner"/> as <c>Tools/Balance</c>), copy it into a new stage, put it in
    /// the campaign, or play it straight away. Menu: Puzzle Kingdoms → Stage Editor.
    /// </summary>
    public sealed class StageEditorWindow : EditorWindow
    {
        public const string StagesFolder = "Assets/_Project/Data/Stages";
        public const string ContentPath = "Assets/_Project/Data/GameContent.asset";
        public const int DefaultTestRuns = 100;
        private const int BoardSize = 6;

        private static readonly Color[] GemColors =
        {
            new Color32(0xE8, 0x5B, 0x55, 0xFF), new Color32(0x52, 0xC9, 0x8A, 0xFF),
            new Color32(0xA9, 0x7F, 0xE6, 0xFF), new Color32(0xF4, 0xBE, 0x4F, 0xFF),
            new Color32(0x4C, 0x9B, 0xE8, 0xFF), new Color32(0xF0, 0x8B, 0xB4, 0xFF),
        };

        private enum Brush { Random, Gem0, Gem1, Gem2, Gem3, Gem4, Gem5 }

        private List<StageDefinition> _stages = new List<StageDefinition>();
        private StageDefinition _selected;
        private SerializedObject _serialized;
        private Brush _brush = Brush.Gem0;
        private bool _stoneBrush;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _report = "";

        [MenuItem("Puzzle Kingdoms/Stage Editor")]
        public static void Open()
        {
            var window = GetWindow<StageEditorWindow>("Stage Editor");
            window.minSize = new Vector2(620, 480);
            window.Refresh();
        }

        private void OnEnable() => Refresh();

        private void OnProjectChange() => Refresh();

        private static GameContent Content => AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);

        private void Refresh()
        {
            _stages = AllStages();
            if (_selected == null && _stages.Count > 0)
            {
                Select(_stages[0]);
            }
            Repaint();
        }

        /// <summary>Every stage asset, campaign stages first in campaign order.</summary>
        public static List<StageDefinition> AllStages()
        {
            List<StageDefinition> all = AssetDatabase.FindAssets("t:StageDefinition", new[] { StagesFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(s => s != null)
                .ToList();
            GameContent content = Content;
            List<StageDefinition> campaign = content == null ? new List<StageDefinition>() : content.Campaign.ToList();
            return campaign.Where(all.Contains).Concat(all.Where(s => !campaign.Contains(s)).OrderBy(s => s.name)).ToList();
        }

        private void Select(StageDefinition stage)
        {
            _selected = stage;
            _serialized = stage == null ? null : new SerializedObject(stage);
            _report = "";
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawList();
            DrawDetail();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(190));
            EditorGUILayout.LabelField("Stages", EditorStyles.boldLabel);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            GameContent content = Content;
            foreach (StageDefinition stage in _stages)
            {
                bool inCampaign = content != null && content.Campaign.Contains(stage);
                string label = (inCampaign ? "" : "· ") + $"{stage.Id}  {stage.DisplayName}";
                GUIStyle style = stage == _selected ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(label, style))
                {
                    Select(stage);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("· = not in the campaign", MessageType.None);
            EditorGUI.BeginDisabledGroup(_selected == null);
            if (GUILayout.Button("New stage (copy of selected)"))
            {
                StageDefinition copy = CreateCopy(_selected, null, null);
                Refresh();
                Select(copy);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetail()
        {
            EditorGUILayout.BeginVertical();
            if (_selected == null || _serialized == null)
            {
                EditorGUILayout.HelpBox("No stage selected.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            _serialized.Update();
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(_selected), EditorStyles.miniLabel);
            foreach (string field in new[] { "id", "displayName", "moves", "goldReward", "stoneGems", "enemyHpMultiplier",
                         "enemyDamageMultiplier", "goalKind", "goalTarget", "mapPosition" })
            {
                EditorGUILayout.PropertyField(_serialized.FindProperty(field));
            }
            EditorGUILayout.PropertyField(_serialized.FindProperty("enemies"), true);

            EditorGUILayout.Space();
            DrawBoard();
            _serialized.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawActions();
            if (_report.Length > 0)
            {
                EditorGUILayout.HelpBox(_report, MessageType.None);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ---- Start board ----

        private int GemTypes => Content == null ? 4 : Content.RecruitableUnits.Count + 1;

        private void DrawBoard()
        {
            EditorGUILayout.LabelField("Start board (empty = random)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush", GUILayout.Width(40));
            Brush[] brushes = Enumerable.Range(0, GemTypes + 1).Select(i => (Brush)i).ToArray();
            string[] names = brushes.Select(BrushName).ToArray();
            _brush = brushes[Mathf.Max(0, GUILayout.Toolbar(System.Array.IndexOf(brushes, _brush), names))];
            _stoneBrush = GUILayout.Toggle(_stoneBrush, "in stone", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            SerializedProperty layout = _serialized.FindProperty("layout");
            char[,] cells = ReadCells(layout);
            const float size = 34f;
            for (int y = 0; y < BoardSize; y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < BoardSize; x++)
                {
                    char c = cells[x, y];
                    int type = c >= '0' && c <= '9' ? c - '0' : c >= 'a' && c <= 'j' ? c - 'a' : -1;
                    bool stone = c == BoardLayout.RandomStone || c >= 'a' && c <= 'j';
                    Color old = GUI.backgroundColor;
                    GUI.backgroundColor = type >= 0 && type < GemColors.Length ? GemColors[type] : new Color(0.35f, 0.35f, 0.35f);
                    if (GUILayout.Button(stone ? "▣" : type >= 0 ? type.ToString() : "·", GUILayout.Width(size), GUILayout.Height(size)))
                    {
                        cells[x, y] = Paint();
                        WriteCells(layout, cells);
                    }
                    GUI.backgroundColor = old;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear board", GUILayout.Width(110)))
            {
                layout.ClearArray();
            }
            EditorGUILayout.EndHorizontal();

            List<string> rows = Rows(layout);
            if (!BoardLayout.IsEmpty(rows) &&
                !BoardLayout.TryParse(rows, BoardSize, BoardSize, GemTypes, out _, out _, out string error))
            {
                EditorGUILayout.HelpBox("Board: " + error, MessageType.Error);
            }
        }

        private string BrushName(Brush brush)
        {
            if (brush == Brush.Random)
            {
                return "random";
            }
            int type = (int)brush - 1;
            GameContent content = Content;
            if (content != null && type < content.RecruitableUnits.Count)
            {
                return content.RecruitableUnits[type].Def.DisplayName;
            }
            return type == GemTypes - 1 ? "Banner" : type.ToString();
        }

        private char Paint()
        {
            if (_brush == Brush.Random)
            {
                return _stoneBrush ? BoardLayout.RandomStone : BoardLayout.RandomCell;
            }
            int type = (int)_brush - 1;
            return (char)((_stoneBrush ? 'a' : '0') + type);
        }

        private static List<string> Rows(SerializedProperty layout) =>
            Enumerable.Range(0, layout.arraySize).Select(i => layout.GetArrayElementAtIndex(i).stringValue).ToList();

        private static char[,] ReadCells(SerializedProperty layout)
        {
            var cells = new char[BoardSize, BoardSize];
            List<string> rows = Rows(layout);
            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    string row = y < rows.Count ? rows[y] ?? "" : "";
                    cells[x, y] = x < row.Length ? row[x] : BoardLayout.RandomCell;
                }
            }
            return cells;
        }

        private static void WriteCells(SerializedProperty layout, char[,] cells)
        {
            var rows = new List<string>();
            for (int y = 0; y < BoardSize; y++)
            {
                var row = new char[BoardSize];
                for (int x = 0; x < BoardSize; x++)
                {
                    row[x] = cells[x, y];
                }
                rows.Add(new string(row));
            }
            if (BoardLayout.IsEmpty(rows))
            {
                layout.ClearArray(); // all random: no layout at all
                return;
            }
            layout.arraySize = BoardSize;
            for (int y = 0; y < BoardSize; y++)
            {
                layout.GetArrayElementAtIndex(y).stringValue = rows[y];
            }
        }

        // ---- Actions ----

        private void DrawActions()
        {
            GameContent content = Content;
            bool inCampaign = content != null && content.Campaign.Contains(_selected);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Test difficulty ({DefaultTestRuns} runs per bot)"))
            {
                _report = TestDifficulty(_selected, DefaultTestRuns);
            }
            if (GUILayout.Button(inCampaign ? "Remove from campaign" : "Add to campaign"))
            {
                SetInCampaign(_selected, !inCampaign);
            }
            if (GUILayout.Button("Play in editor"))
            {
                Play(_selected);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Plays the stage with the balance bots (random and greedy puzzle player, Rally used at once) and
        /// returns a short report: win rate, average stars and seconds, goals missed.
        /// </summary>
        public static string TestDifficulty(StageDefinition stage, int runs)
        {
            GameContent content = Content;
            var lines = new List<string> { $"{stage.Id}: {runs} runs per bot, Rally used at once" };
            foreach (PuzzleBot bot in new[] { PuzzleBot.Random, PuzzleBot.Greedy })
            {
                BalanceStats stats = BalanceRunner.Run(seed => content.CreateRun(stage, new SeededRandom(seed)),
                    bot, RallyBot.Eager, runs);
                string goal = stage.GoalKind == StageGoalKind.DefeatArmy
                    ? ""
                    : $", goal missed {100.0 * stats.GoalMissed / stats.Runs:0}%";
                lines.Add($"{bot,-7} bot: wins {100.0 * stats.WinRate:0}%, stars {stats.AverageStars:0.0}, " +
                          $"{stats.AverageSeconds:0.0} s, stones broken {stats.StonesBroken / stats.Runs:0.0}{goal}");
            }
            return string.Join("\n", lines);
        }

        /// <summary>Copies a stage into a new asset next to it (new id and name); returns the copy.</summary>
        public static StageDefinition CreateCopy(StageDefinition source, string newId, string newName)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string id = string.IsNullOrEmpty(newId) ? source.Id + "_copy" : newId;
            string fileName = "Stage_" + id.Replace("stage_", "");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{StagesFolder}/{fileName}.asset");
            if (!AssetDatabase.CopyAsset(sourcePath, path))
            {
                throw new IOException($"Could not copy {sourcePath} to {path}");
            }
            var copy = AssetDatabase.LoadAssetAtPath<StageDefinition>(path);
            var serialized = new SerializedObject(copy);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = string.IsNullOrEmpty(newName) ? source.DisplayName + " (copy)" : newName;
            serialized.FindProperty("tutorial").ClearArray(); // tutorial ids must stay unique (D-040)
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return copy;
        }

        /// <summary>Adds the stage to the end of the campaign or takes it out.</summary>
        public static void SetInCampaign(StageDefinition stage, bool inCampaign)
        {
            GameContent content = Content;
            var serialized = new SerializedObject(content);
            SerializedProperty campaign = serialized.FindProperty("campaign");
            int index = -1;
            for (int i = 0; i < campaign.arraySize; i++)
            {
                if (campaign.GetArrayElementAtIndex(i).objectReferenceValue == stage)
                {
                    index = i;
                }
            }
            if (inCampaign && index < 0)
            {
                campaign.arraySize++;
                campaign.GetArrayElementAtIndex(campaign.arraySize - 1).objectReferenceValue = stage;
            }
            else if (!inCampaign && index >= 0)
            {
                campaign.GetArrayElementAtIndex(index).objectReferenceValue = null;
                campaign.DeleteArrayElementAtIndex(index);
            }
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Enters Play mode straight into this stage (the map is skipped; after it, the map as usual).</summary>
        public static void Play(StageDefinition stage)
        {
            SessionState.SetString(PuzzleKingdoms.Flow.GameApp.PlayStageKey, AssetDatabase.GetAssetPath(stage));
            EditorApplication.EnterPlaymode();
        }
    }
}

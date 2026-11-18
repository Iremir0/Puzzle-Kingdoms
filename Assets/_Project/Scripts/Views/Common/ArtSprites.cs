using System.Collections.Generic;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Painted UI art (Meshy images in Resources/Art/UI): building pictures and chapter map paintings.
    /// Loaded as textures and wrapped into sprites once; a missing file gives null and callers fall back to
    /// the procedural look.
    /// </summary>
    public static class ArtSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (Cache.TryGetValue(name, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }
            var texture = Resources.Load<Texture2D>("Art/UI/" + name);
            sprite = texture == null ? null
                : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[name] = sprite;
            return sprite;
        }

        /// <summary>Picture of a Kingdom building by its rule id, or null.</summary>
        public static Sprite Building(string buildingId)
        {
            switch (buildingId)
            {
                case "barracks": return Get("Building_Barracks");
                case "archer_tower": return Get("Building_ArcherTower");
                case "mage_tower": return Get("Building_MageTower");
                case "treasury": return Get("Building_Treasury");
                default: return null;
            }
        }


        /// <summary>Portrait of a commander by id ("" = the classic Rally's king), or null.</summary>
        public static Sprite Commander(string commanderId)
        {
            switch (commanderId)
            {
                case "": return Get("Commander_Classic");
                case "shield_wall": return Get("Commander_ShieldWall");
                case "arrow_rain": return Get("Commander_ArrowRain");
                case "fireball": return Get("Commander_Fireball");
                default: return null;
            }
        }

        /// <summary>Raw texture of an art file, or null.</summary>
        public static Texture2D Texture(string name) => Resources.Load<Texture2D>("Art/UI/" + name);

        /// <summary>Painted battle ground of a stage: the meadow, the goblin camp, or the fort gate from stage 5 of the chapter on; null = procedural.</summary>
        public static Texture2D Arena(string stageId, int chapter) =>
            chapter >= 2 && StageNumber(stageId) >= 5 ? Texture("Battle_Gate") : chapter >= 2 ? Texture("Battle_Chapter2") : Texture("Battle_Chapter1");

        /// <summary>The number after the last underscore of "stage_2_7", or 0.</summary>
        private static int StageNumber(string stageId)
        {
            int cut = stageId.LastIndexOf('_');
            return cut >= 0 && int.TryParse(stageId.Substring(cut + 1), out int number) ? number : 0;
        }

        /// <summary>Painting behind the stage map of a chapter, or null.</summary>
        public static Sprite ChapterMap(int chapter) => Get("Map_Chapter" + chapter);
    }
}

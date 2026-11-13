using System;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// A 3×3 formation drawn as squares. Lanes run left to right. The front row
    /// (y = 0) faces the other army: drawn at the top for the player, at the
    /// bottom for the enemy shown above it.
    /// </summary>
    public sealed class FormationGridView
    {
        public readonly RectTransform Root;
        public event Action<GridPos> CellClicked;

        private readonly Image[] _cells = new Image[Formation.Size * Formation.Size];
        private readonly Text[] _labels = new Text[Formation.Size * Formation.Size];
        private readonly Outline[] _outlines = new Outline[Formation.Size * Formation.Size];

        public FormationGridView(Transform parent, string name, bool frontAtBottom, bool interactive,
            float xMin, float yMin, float xMax, float yMax)
        {
            RectTransform area = Ui.Rect(parent, name, xMin, yMin, xMax, yMax);
            Root = Ui.Stretch(area, "Square");
            var fitter = Root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            const float step = 1f / Formation.Size;
            for (int y = 0; y < Formation.Size; y++)
            {
                int row = frontAtBottom ? Formation.Size - 1 - y : y;
                for (int x = 0; x < Formation.Size; x++)
                {
                    var cell = new GridPos(x, y);
                    float left = x * step;
                    float top = 1f - row * step;
                    Image image = Ui.Box(Root, $"Cell {x},{y}", Palette.Panel,
                        left + 0.01f, top - step + 0.01f, left + step - 0.01f, top - 0.01f);
                    int index = Index(cell);
                    _cells[index] = image;
                    _labels[index] = Ui.Label(image.transform, "", 40, TextAnchor.MiddleCenter, Palette.Text);
                    _outlines[index] = image.gameObject.AddComponent<Outline>();
                    _outlines[index].effectColor = Palette.Highlight;
                    _outlines[index].effectDistance = new Vector2(6, 6);
                    _outlines[index].enabled = false;
                    if (interactive)
                    {
                        var button = image.gameObject.AddComponent<Button>();
                        button.transition = Selectable.Transition.None;
                        button.onClick.AddListener(() => CellClicked?.Invoke(cell));
                    }
                }
            }
        }

        /// <summary>Draws every cell of <paramref name="formation"/>.</summary>
        public void Show(Formation formation, ArmyUnit selected = null)
        {
            for (int y = 0; y < Formation.Size; y++)
            {
                for (int x = 0; x < Formation.Size; x++)
                {
                    var cell = new GridPos(x, y);
                    ArmyUnit unit = formation.GetUnit(cell);
                    SetCell(cell, unit, unit != null && unit == selected);
                }
            }
        }

        public void SetCell(GridPos cell, ArmyUnit unit, bool highlighted)
        {
            int index = Index(cell);
            _cells[index].color = unit == null ? Palette.Panel : Palette.For(unit.Def);
            _labels[index].text = Palette.Tag(unit);
            _outlines[index].enabled = highlighted;
        }

        private static int Index(GridPos cell) => cell.Y * Formation.Size + cell.X;
    }
}

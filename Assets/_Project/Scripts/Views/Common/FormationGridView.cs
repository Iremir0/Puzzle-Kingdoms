using System;
using PuzzleKingdoms.Data;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// A formation drawn as squares in the unit's class colour with its
    /// silhouette and tier (PB-3). Lanes run left to right. The front row
    /// (y = 0) faces the other army: drawn at the top for the player, at the
    /// bottom for the enemy shown above it.
    /// </summary>
    public sealed class FormationGridView
    {
        public readonly RectTransform Root;
        public event Action<GridPos> CellClicked;

        private readonly Image[] _cells = new Image[Formation.Size * Formation.Size];
        private readonly Image[] _icons = new Image[Formation.Size * Formation.Size];
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
                    _icons[index] = UnitSilhouettes.AddIcon(image.transform, UnitLook.Generic, 1);
                    _icons[index].enabled = false;
                    _labels[index] = UnitSilhouettes.AddTierLabel(image.transform, 1, 30);
                    _labels[index].text = "";
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
            _icons[index].enabled = unit != null;
            _labels[index].text = unit == null ? "" : "T" + unit.Tier;
            if (unit != null)
            {
                _icons[index].sprite = UnitSilhouettes.Get(Palette.LookFor(unit.Def));
                UnitSilhouettes.FitIcon(_icons[index].rectTransform, unit.Tier);
            }
            _outlines[index].enabled = highlighted;
        }

        private static int Index(GridPos cell) => cell.Y * Formation.Size + cell.X;
    }
}

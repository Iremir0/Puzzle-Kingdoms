using NUnit.Framework;
using PuzzleKingdoms.Views.Common;
using UnityEngine;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>
    /// PB-16: text must stay readable on the colours it is drawn on (WCAG contrast ratio). Button captions are
    /// 38 px or larger, which counts as large text (3:1); small captions on panels need 4.5:1.
    /// </summary>
    public class ContrastTests
    {
        private static float Luminance(Color color)
        {
            static float Channel(float c) => c <= 0.03928f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * Channel(color.r) + 0.7152f * Channel(color.g) + 0.0722f * Channel(color.b);
        }

        private static float Ratio(Color a, Color b)
        {
            float x = Luminance(a);
            float y = Luminance(b);
            return (Mathf.Max(x, y) + 0.05f) / (Mathf.Min(x, y) + 0.05f);
        }

        [TestCase(Ui.ButtonStyle.Primary)]
        [TestCase(Ui.ButtonStyle.Secondary)]
        [TestCase(Ui.ButtonStyle.Danger)]
        public void ButtonCaptionsAreReadable(Ui.ButtonStyle style)
        {
            Assert.GreaterOrEqual(Ratio(Palette.Text, Ui.StyleColor(style)), 3f, $"{style} button: white caption needs 3:1 (large text)");
        }

        [Test]
        public void RallyButtonCaptionIsReadable()
        {
            Assert.GreaterOrEqual(Ratio(Palette.Background, Palette.Gold), 4.5f);
        }

        [Test]
        public void SmallTextOnPanelsIsReadable()
        {
            Assert.GreaterOrEqual(Ratio(Palette.Text, Palette.Panel), 4.5f);
            Assert.GreaterOrEqual(Ratio(Palette.Muted, Palette.Panel), 4.5f);
            Assert.GreaterOrEqual(Ratio(Palette.Muted, Palette.Background), 4.5f);
            Assert.GreaterOrEqual(Ratio(Palette.Gold, Palette.Background), 4.5f);
        }
    }
}

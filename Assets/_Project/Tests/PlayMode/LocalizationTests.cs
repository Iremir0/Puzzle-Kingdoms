using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Views.Common;
using UnityEngine;

namespace PuzzleKingdoms.Tests.PlayMode
{
    /// <summary>PB-32: the Turkish table loads from Resources and the UI font can draw all of it.</summary>
    public class LocalizationTests
    {
        [TearDown]
        public void TearDown() => Loc.Use(Language.English);

        [Test]
        public void TurkishTableLoadsFromResources()
        {
            Loc.Use(Language.Turkish);
            Assert.AreEqual(Language.Turkish, Loc.Language);
            Assert.AreEqual("Hepsini birleştir", Loc.T("Merge all"));
            Assert.AreEqual("Hamle: 7", Loc.F("Moves: {0}", 7));
            Loc.Use(Language.English);
            Assert.AreEqual("Merge all", Loc.T("Merge all"));
        }

        [Test]
        public void UiFontDrawsEveryTurkishCharacter()
        {
            Font font = Ui.Font;
            Assert.IsNotNull(font);
            string all = "çÇğĞıİöÖşŞüÜ" + string.Concat(Localizer.ParseTable(Loc.LoadTable(Language.Turkish), null).Values);
            var missing = new SortedSet<char>(all.Where(c => !char.IsWhiteSpace(c) && !font.HasCharacter(c)));
            CollectionAssert.IsEmpty(missing, $"{font.name} cannot draw these characters");
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using PuzzleKingdoms.Logic.Localization;
using PuzzleKingdoms.Logic.Progression;

namespace PuzzleKingdoms.Tests.Localization
{
    public class LocalizerTests
    {
        private static Localizer Turkish(string table) => Localizer.FromTable(Language.Turkish, table);

        [Test]
        public void EnglishShowsTheSourceText()
        {
            Assert.AreEqual("Merge all", Localizer.English.T("Merge all"));
            Assert.AreEqual("Moves: 12", Localizer.English.F("Moves: {0}", 12));
            Assert.IsTrue(Localizer.English.Has("anything"));
        }

        [Test]
        public void TurkishLooksUpTheTableAndFallsBackToEnglish()
        {
            Localizer tr = Turkish("Merge all\tHepsini birleştir\nMoves: {0}\tHamle: {0}");
            Assert.AreEqual("Hepsini birleştir", tr.T("Merge all"));
            Assert.AreEqual("Hamle: 12", tr.F("Moves: {0}", 12));
            Assert.AreEqual("Undo", tr.T("Undo"), "no translation: the English text");
            Assert.IsFalse(tr.Has("Undo"));
            Assert.AreEqual("", tr.T(""));
            Assert.AreEqual("", tr.T(null));
        }

        [Test]
        public void NumbersAreWrittenTheLanguagesWay()
        {
            Localizer tr = Turkish("Battle time: {0:0.0} s\tSavaş süresi: {0:0.0} sn");
            Assert.AreEqual("Savaş süresi: 2,5 sn", tr.F("Battle time: {0:0.0} s", 2.5f));
            Assert.AreEqual("Battle time: 2.5 s", Localizer.English.F("Battle time: {0:0.0} s", 2.5f));
            Assert.AreEqual("1.250", 1250.ToString("#,0", tr.Numbers));
        }

        [Test]
        public void ABrokenTranslationFallsBackToTheEnglishPattern()
        {
            Localizer tr = Turkish("Moves: {0}\tHamle: {1}");
            Assert.AreEqual("Moves: 3", tr.F("Moves: {0}", 3));
        }

        [Test]
        public void TableSkipsNotesAndReportsBadLines()
        {
            var problems = new List<string>();
            Dictionary<string, string> table = Localizer.ParseTable(
                "﻿# a note\n\nA\tB\r\nno tab here\nA\tC\nEmpty\t\n  # indented note\nTwo\\nlines\tİki\\nsatır\n", problems);
            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("B", table["A"], "the first pair of a source wins");
            Assert.AreEqual("İki\nsatır", table["Two\nlines"], "\\n is a line break in both columns");
            Assert.AreEqual(3, problems.Count, string.Join("; ", problems));
            StringAssert.Contains("line 4", problems[0]);
        }

        [Test]
        public void PlaceholdersAreFoundByNumber()
        {
            CollectionAssert.AreEqual(new[] { 0, 1 }, Localizer.Placeholders("Total: +{0} gold ({1})"));
            CollectionAssert.AreEqual(new[] { 0, 4 }, Localizer.Placeholders("{0} and {4:0.#}"));
            CollectionAssert.IsEmpty(Localizer.Placeholders("{{0}} is literal, {x} is not one"));
        }

        [TestCase("tr", false, Language.Turkish)]
        [TestCase("EN", true, Language.English)]
        [TestCase(" tr ", false, Language.Turkish)]
        [TestCase("", true, Language.Turkish)]
        [TestCase("", false, Language.English)]
        [TestCase("de", true, Language.Turkish)]
        [TestCase(null, false, Language.English)]
        public void LanguageResolvesFromTheSettingThenThePhone(string setting, bool systemIsTurkish, Language expected)
        {
            Assert.AreEqual(expected, Languages.Resolve(setting, systemIsTurkish));
        }

        [Test]
        public void SettingsKeepAKnownLanguageAndForgetAnUnknownOne()
        {
            Assert.AreEqual(Languages.Auto, new GameSettings().language, "new settings follow the phone");
            Assert.AreEqual("tr", new GameSettings { language = " TR " }.Normalize().language);
            Assert.AreEqual(Languages.Auto, new GameSettings { language = "klingon" }.Normalize().language);
            Assert.AreEqual(Languages.Auto, new GameSettings { language = null }.Normalize().language);
            Assert.AreEqual(Language.Turkish, new GameSettings { language = "tr" }.LanguageFor(false));
        }

        [Test]
        public void EveryLanguageHasACodeAndItsOwnName()
        {
            foreach (Language language in Languages.All)
            {
                Assert.IsTrue(Languages.TryParse(Languages.Code(language), out Language back));
                Assert.AreEqual(language, back);
            }
            Assert.AreEqual("Türkçe", Languages.NativeName(Language.Turkish));
            Assert.AreEqual("English", Languages.NativeName(Language.English));
        }
    }
}

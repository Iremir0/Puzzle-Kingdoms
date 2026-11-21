using System.Linq;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Army;
using PuzzleKingdoms.Logic.Combat;
using PuzzleKingdoms.Logic.Flow;
using PuzzleKingdoms.Logic.Progression;
using PuzzleKingdoms.Logic.Puzzle;
using PuzzleKingdoms.Logic.Stages;
using PuzzleKingdoms.Tests.Army;

namespace PuzzleKingdoms.Tests.Flow
{
    /// <summary>PB-39 (D-072): after each won battle one offer is a perk that lasts the run.</summary>
    public class ExpeditionPerkTests
    {
        private static ExpeditionRules Rules()
        {
            var rules = new ExpeditionRules { Steps = 3 };
            rules.PlayerClasses.Add(ArmyFixtures.UnitDef("warrior", 5000, 40, 1f, 1f, 1.2f));
            rules.PlayerClasses.Add(ArmyFixtures.UnitDef("archer", 2500, 30, 0.8f, 4f, 1f));
            rules.PlayerClasses.Add(ArmyFixtures.UnitDef("mage", 2500, 50, 1.6f, 3.5f, 0.9f));
            rules.EnemyPool.Add(ArmyFixtures.UnitDef("goblin_grunt", 40, 2, 1f, 1f, 1.2f));
            rules.Boss = ArmyFixtures.UnitDef("goblin_chief", 160, 4, 1f, 1f, 1f);
            return rules;
        }

        /// <summary>Wins the first battle (a strong army against weak goblins) and returns the expedition waiting for a pick.</summary>
        private static Expedition AfterFirstWin(ulong seed)
        {
            var expedition = new Expedition(Rules(), seed);
            StageDef stage = expedition.StageForStep();
            var run = new StageRun(stage, expedition.Rules.PlayerClasses, new PuzzleRules(), new ArmyRules(), new BattleRules(),
                new ProgressionRules(), new SeededRandom(stage.BoardSeed), expedition.Bonuses(), null, expedition.Army);
            while (!run.Puzzle.IsFinished)
            {
                Assert.IsTrue(run.Puzzle.Board.TryFindValidMove(out GridPos a, out GridPos b));
                run.Puzzle.PlaySwap(a, b);
            }
            AutoFormation.Fill(run.Roster);
            Assert.IsTrue(run.StartBattle());
            while (!run.Battle.IsFinished)
            {
                run.Battle.Step();
            }
            expedition.Finish(run);
            Assert.AreEqual(ExpeditionPhase.Reward, expedition.Phase, "the first battle is won");
            return expedition;
        }

        [Test]
        public void EveryDrawHasOnePerkForAClassInTheArmy()
        {
            for (ulong seed = 1; seed <= 6; seed++)
            {
                Expedition expedition = AfterFirstWin(seed);
                Assert.AreEqual(3, expedition.Offers.Count);
                ExpeditionReward[] perks = expedition.Offers.Where(o => o.IsPerk).ToArray();
                Assert.AreEqual(1, perks.Length, $"seed {seed}");
                Assert.IsTrue(expedition.Army.Any(u => u.Def.Id == perks[0].UnitId), "a perk for a class the army has");
                Assert.AreEqual(20, perks[0].Amount);
            }
        }

        [Test]
        public void APickedPerkLastsAndBecomesABattleBonus()
        {
            Expedition expedition = AfterFirstWin(3);
            int index = expedition.Offers.FindIndex(o => o.IsPerk);
            ExpeditionReward perk = expedition.Offers[index];
            Assert.IsTrue(expedition.Pick(index));
            Assert.AreEqual(1, expedition.Perks.Count);

            float hp = expedition.Bonuses().HpMultiplier(perk.UnitId);
            float damage = expedition.Bonuses().DamageMultiplier(perk.UnitId);
            float speed = expedition.Bonuses().AttackSpeedMultiplier(perk.UnitId);
            Assert.AreEqual(1.2f, hp * damage * speed, 0.0001f, "exactly one of the three is +20%");
            Assert.AreEqual(1f, expedition.Bonuses().HpMultiplier("nobody"), 0.0001f);
        }

        [Test]
        public void PerksSurviveSavingAndOldSavesHaveNone()
        {
            Expedition expedition = AfterFirstWin(5);
            expedition.Pick(expedition.Offers.FindIndex(o => o.IsPerk));
            ExpeditionSave save = expedition.ToSave();

            Expedition loaded = Expedition.FromSave(save, Rules());
            Assert.AreEqual(1, loaded.Perks.Count);
            Assert.AreEqual(expedition.Perks[0].Kind, loaded.Perks[0].Kind);
            Assert.AreEqual(expedition.Perks[0].UnitId, loaded.Perks[0].UnitId);

            save.perkKinds = null;
            save.perkUnits = null;
            save.perkAmounts = null;
            Assert.AreEqual(0, Expedition.FromSave(save, Rules()).Perks.Count, "a save from before PB-39");
        }

        [Test]
        public void APerkMakesTheUnitStrongerInBattle()
        {
            var expedition = new Expedition(Rules(), 9);
            var plain = new Combatant(1, Side.Player, new ArmyUnit(expedition.Rules.PlayerClasses[0]), new GridPos(0, 0), new ArmyRules(),
                expedition.Bonuses());
            ExpeditionSave save = expedition.ToSave();
            save.perkKinds.Add((int)ExpeditionRewardKind.PerkHp);
            save.perkUnits.Add("warrior");
            save.perkAmounts.Add(20);
            Expedition withPerk = Expedition.FromSave(save, Rules());
            var strong = new Combatant(2, Side.Player, new ArmyUnit(withPerk.Rules.PlayerClasses[0]), new GridPos(0, 0), new ArmyRules(),
                withPerk.Bonuses());
            Assert.AreEqual(plain.MaxHp * 6 / 5, strong.MaxHp);
        }
    }
}

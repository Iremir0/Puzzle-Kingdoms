using System.Collections.Generic;
using NUnit.Framework;
using PuzzleKingdoms.Logic;
using PuzzleKingdoms.Logic.Puzzle;

namespace PuzzleKingdoms.Tests.Puzzle
{
    public class RecruitmentTests
    {
        private static readonly string[] Keys = { "warrior", "archer", "mage", "rally" };

        [Test]
        public void ThreePointsMakeOneReward()
        {
            var meter = new RecruitmentMeter(3);
            Assert.AreEqual(1, meter.AddPoints(3));
            Assert.AreEqual(1, meter.Rewards);
            Assert.AreEqual(0, meter.Points);
        }

        [Test]
        public void RemainderCarriesOver()
        {
            var meter = new RecruitmentMeter(3);
            Assert.AreEqual(1, meter.AddPoints(4));
            Assert.AreEqual(1, meter.Points);
            Assert.AreEqual(1, meter.AddPoints(2));
            Assert.AreEqual(2, meter.Rewards);
        }

        [Test]
        public void CapSendsPointsToOverflow()
        {
            var meter = new RecruitmentMeter(3, 1);
            Assert.AreEqual(1, meter.AddPoints(5));
            Assert.IsTrue(meter.IsFull);
            Assert.AreEqual(2, meter.OverflowPoints);
        }

        private static PuzzleSession MakeSession(int moves)
        {
            var board = new BoardModel(6, 6, 4, new SeededRandom(21));
            board.Generate();
            var meters = new Dictionary<string, RecruitmentMeter>
            {
                { "warrior", new RecruitmentMeter(3) },
                { "archer", new RecruitmentMeter(3) },
                { "mage", new RecruitmentMeter(3) },
                { "rally", new RecruitmentMeter(3, 3) },
            };
            return new PuzzleSession(board, moves, Keys, meters);
        }

        [Test]
        public void SessionSpendsMovesOnlyOnValidSwaps()
        {
            PuzzleSession session = MakeSession(2);
            Assert.IsFalse(session.PlaySwap(new GridPos(0, 0), new GridPos(3, 3)).IsValid);
            Assert.AreEqual(2, session.MovesLeft);

            session.Board.TryFindValidMove(out GridPos a, out GridPos b);
            Assert.IsTrue(session.PlaySwap(a, b).IsValid);
            Assert.AreEqual(1, session.MovesLeft);
        }

        [Test]
        public void SessionFinishesAndRejectsFurtherSwaps()
        {
            PuzzleSession session = MakeSession(1);
            int finishedEvents = 0;
            session.Finished += () => finishedEvents++;
            session.Board.TryFindValidMove(out GridPos a, out GridPos b);
            session.PlaySwap(a, b);
            Assert.IsTrue(session.IsFinished);
            Assert.AreEqual(1, finishedEvents);
            session.Board.TryFindValidMove(out a, out b);
            Assert.IsFalse(session.PlaySwap(a, b).IsValid);
        }

        [Test]
        public void SessionRewardsMatchClearedGems()
        {
            PuzzleSession session = MakeSession(12);
            var totalCleared = new Dictionary<int, int>();
            int earnedEvents = 0;
            session.RewardsEarned += (key, amount) => earnedEvents += amount;
            while (!session.IsFinished)
            {
                session.Board.TryFindValidMove(out GridPos a, out GridPos b);
                MoveResult result = session.PlaySwap(a, b);
                foreach (KeyValuePair<int, int> pair in result.ClearedByType)
                {
                    totalCleared[pair.Key] = (totalCleared.TryGetValue(pair.Key, out int n) ? n : 0) + pair.Value;
                }
            }
            int totalRewards = 0;
            for (int gemType = 0; gemType < 3; gemType++)
            {
                int cleared = totalCleared.TryGetValue(gemType, out int n) ? n : 0;
                Assert.AreEqual(cleared / 3, session.RewardCount(Keys[gemType]), Keys[gemType]);
            }
            foreach (string key in Keys)
            {
                totalRewards += session.RewardCount(key);
            }
            Assert.AreEqual(totalRewards, earnedEvents, "events report every reward");
        }
    }
}

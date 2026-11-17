using System;
using System.Collections.Generic;

namespace PuzzleKingdoms.Logic.Progression
{
    /// <summary>The moments a tutorial prompt can appear at (PB-13).</summary>
    public enum TutorialTrigger
    {
        /// <summary>The puzzle screen opened.</summary>
        PuzzleStarted = 0,
        /// <summary>The puzzle recruited a unit.</summary>
        UnitRecruited = 1,
        /// <summary>The War Camp opened.</summary>
        WarCampOpened = 2,
        /// <summary>The battle screen opened.</summary>
        BattleStarted = 3,
        /// <summary>The results screen opened after a lost battle.</summary>
        BattleLost = 4,
    }

    /// <summary>One prompt: shown once per player when its trigger fires in its stage.</summary>
    public sealed class TutorialStep
    {
        /// <summary>Unique across the game; the profile remembers it once seen.</summary>
        public string Id;
        public TutorialTrigger Trigger;
        public string Text;
        /// <summary>Name of the UI element to point at, or empty for none.</summary>
        public string Target = "";
    }

    /// <summary>
    /// Picks the prompts to show for a trigger: the stage's steps with that trigger
    /// that this player has not seen, in order, none once the tutorial is skipped.
    /// </summary>
    public static class TutorialGuide
    {
        public static List<TutorialStep> StepsFor(IEnumerable<TutorialStep> stageSteps, TutorialTrigger trigger,
            PlayerProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            var result = new List<TutorialStep>();
            if (stageSteps == null || profile.TutorialSkipped)
            {
                return result;
            }
            foreach (TutorialStep step in stageSteps)
            {
                if (step != null && step.Trigger == trigger && !string.IsNullOrEmpty(step.Id)
                    && !profile.HasSeenStep(step.Id) && !result.Exists(s => s.Id == step.Id))
                {
                    result.Add(step);
                }
            }
            return result;
        }
    }
}

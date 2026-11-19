using System;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// A rewarded-ad offer a screen may show (PB-30): whether it is available now, what the button says and what
    /// to call when the player taps it. The screen does not know about ads; <c>GameApp</c> builds these.
    /// </summary>
    public sealed class AdOffer
    {
        public Func<bool> Available = () => false;
        public string Caption = "";

        /// <summary>Starts the reward; calls the argument when it was granted (after the ad, or at once for a Supporter).</summary>
        public Action<Action> Request = granted => { };
    }
}

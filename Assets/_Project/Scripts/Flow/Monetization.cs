using System;

namespace PuzzleKingdoms.Flow
{
    /// <summary>
    /// Rewarded ads behind an interface (PB-30, D-064). The real SDK comes later and must only show an ad the
    /// player asked for, with personalised ads only after consent (PB-29).
    /// </summary>
    public interface IAds
    {
        /// <summary>True when an ad can be shown now.</summary>
        bool IsReady { get; }

        /// <summary>Shows a rewarded ad; <paramref name="onFinished"/> gets true when the player earned the reward.</summary>
        void ShowRewarded(Action<bool> onFinished);
    }

    /// <summary>In-app purchases behind an interface (PB-30). One product so far: the Supporter pack.</summary>
    public interface IBilling
    {
        bool IsAvailable { get; }

        /// <summary>The price as the store shows it, or empty when unknown.</summary>
        string SupporterPrice { get; }

        void BuySupporter(Action<bool> onFinished);

        /// <summary>Asks the store whether the Supporter pack was bought before (new phone, reinstall).</summary>
        void RestoreSupporter(Action<bool> onFinished);
    }

    /// <summary>No ads: the game ships like this until an ad SDK is added. Ad buttons stay hidden.</summary>
    public sealed class NoAds : IAds
    {
        public bool IsReady => false;

        public void ShowRewarded(Action<bool> onFinished) => onFinished?.Invoke(false);
    }

    /// <summary>A stand-in that grants at once, for the editor, dev builds and tests.</summary>
    public sealed class TestAds : IAds
    {
        /// <summary>How many ads were "shown".</summary>
        public int Shown { get; private set; }

        public bool Succeeds = true;

        public bool IsReady => true;

        public void ShowRewarded(Action<bool> onFinished)
        {
            Shown++;
            onFinished?.Invoke(Succeeds);
        }
    }

    public sealed class NoBilling : IBilling
    {
        public bool IsAvailable => false;

        public string SupporterPrice => "";

        public void BuySupporter(Action<bool> onFinished) => onFinished?.Invoke(false);

        public void RestoreSupporter(Action<bool> onFinished) => onFinished?.Invoke(false);
    }

    /// <summary>A stand-in store that "sells" the Supporter pack at once, for the editor, dev builds and tests.</summary>
    public sealed class TestBilling : IBilling
    {
        public bool Succeeds = true;
        public bool Owned;

        public bool IsAvailable => true;

        public string SupporterPrice => "$2.99";

        public void BuySupporter(Action<bool> onFinished)
        {
            Owned = Owned || Succeeds;
            onFinished?.Invoke(Succeeds);
        }

        public void RestoreSupporter(Action<bool> onFinished) => onFinished?.Invoke(Owned);
    }
}

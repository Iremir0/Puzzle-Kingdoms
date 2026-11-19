using System;

namespace PuzzleKingdoms.Flow
{
    /// <summary>A save as the cloud holds it: the profile JSON and when it was written.</summary>
    public sealed class CloudData
    {
        public string Json = "";
        public DateTime SavedUtc;
    }

    /// <summary>
    /// The cloud save behind an interface (PB-31, D-065). The real implementation (Google Play Games saved games)
    /// needs the plugin and a Play Console app; until then releases ship <see cref="NoCloud"/> and the editor and dev
    /// builds use <see cref="TestCloud"/>. Sign-in is always optional.
    /// </summary>
    public interface ICloudSave
    {
        /// <summary>True when a cloud exists on this device (the Settings row shows only then).</summary>
        bool IsAvailable { get; }

        bool IsSignedIn { get; }

        /// <summary>The player's name in the cloud, or empty.</summary>
        string AccountName { get; }

        void SignIn(Action<bool> done);

        void SignOut();

        /// <summary>Reads the stored save; the callback gets null when there is none or it could not be read.</summary>
        void Read(Action<CloudData> done);

        void Write(CloudData data, Action<bool> done);
    }

    public sealed class NoCloud : ICloudSave
    {
        public bool IsAvailable => false;

        public bool IsSignedIn => false;

        public string AccountName => "";

        public void SignIn(Action<bool> done) => done?.Invoke(false);

        public void SignOut() { }

        public void Read(Action<CloudData> done) => done?.Invoke(null);

        public void Write(CloudData data, Action<bool> done) => done?.Invoke(false);
    }

    /// <summary>An in-memory cloud for the editor, dev builds and tests; two phones can share one instance.</summary>
    public sealed class TestCloud : ICloudSave
    {
        public CloudData Stored;
        public int Writes { get; private set; }
        public bool SignInSucceeds = true;

        public bool IsAvailable => true;

        public bool IsSignedIn { get; private set; }

        public string AccountName => IsSignedIn ? "Test Player" : "";

        public void SignIn(Action<bool> done)
        {
            IsSignedIn = SignInSucceeds;
            done?.Invoke(IsSignedIn);
        }

        public void SignOut() => IsSignedIn = false;

        public void Read(Action<CloudData> done) => done?.Invoke(IsSignedIn ? Stored : null);

        public void Write(CloudData data, Action<bool> done)
        {
            if (!IsSignedIn)
            {
                done?.Invoke(false);
                return;
            }
            Stored = data;
            Writes++;
            done?.Invoke(true);
        }
    }
}

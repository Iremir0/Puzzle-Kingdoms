using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Short, light vibrations for moments that matter (PB-38, D-071): a merge, Rally, a victory. Android only
    /// (API 26+ one-shot effects through the Vibrator service); nothing elsewhere. Off when the player turns it off.
    /// Research: few, crisp pulses tied to success feel good; constant buzzing does not.
    /// </summary>
    public static class Haptics
    {
        /// <summary>Follows the Vibration setting.</summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>A light tick: a merge.</summary>
        public static void Light() => Vibrate(15, 80);

        /// <summary>A firmer pulse: Rally.</summary>
        public static void Medium() => Vibrate(30, 160);

        /// <summary>A victory.</summary>
        public static void Success() => Vibrate(60, 200);

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static bool _failed;

        private static void Vibrate(long milliseconds, int amplitude)
        {
            if (!Enabled || _failed)
            {
                return;
            }
            try
            {
                if (_vibrator == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                using var effects = new AndroidJavaClass("android.os.VibrationEffect");
                using AndroidJavaObject effect = effects.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
                _vibrator.Call("vibrate", effect);
            }
            catch (System.Exception e)
            {
                _failed = true; // no vibrator or no permission: stay quiet from now on
                Debug.LogWarning("[Haptics] " + e.Message);
            }
        }

        /// <summary>Never called: it makes Unity add the VIBRATE permission to the Android manifest.</summary>
        private static void KeepVibratePermission() => Handheld.Vibrate();
#else
        private static void Vibrate(long milliseconds, int amplitude)
        {
        }
#endif
    }
}

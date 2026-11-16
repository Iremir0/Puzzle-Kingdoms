using PuzzleKingdoms.Data;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Plays sound effects and the music loop from the <see cref="SoundBank"/>
    /// (PB-8). One instance lives on the GameApp object; screens call the static
    /// methods. Without a bank, or while muted, nothing plays.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        /// <summary>Effects can overlap (a cascade, a crack and a ding); each voice keeps its own pitch.</summary>
        private const int Voices = 8;

        private static Sfx _instance;
        private static bool _muted;

        private SoundBank _bank;
        private AudioSource[] _voices;
        private AudioSource _music;
        private int _next;

        /// <summary>The sounds in use, or null when there are none.</summary>
        public static SoundBank Bank => _instance != null ? _instance._bank : null;

        /// <summary>How many effects actually started playing (tests use it to check mute).</summary>
        public static int PlayedCount { get; private set; }

        /// <summary>Silences effects and music. Kept for the session; saved with the settings later (PB-14).</summary>
        public static bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                if (_instance != null && _instance._music != null)
                {
                    _instance._music.mute = value;
                }
            }
        }

        /// <summary>Adds the player to <paramref name="host"/> and starts the music.</summary>
        public static Sfx Setup(GameObject host, SoundBank bank)
        {
            var sfx = host.GetComponent<Sfx>();
            if (sfx == null)
            {
                sfx = host.AddComponent<Sfx>();
            }
            sfx.Init(bank);
            return sfx;
        }

        /// <summary>Plays <paramref name="clip"/> once; returns false when muted, without a bank or without a clip.</summary>
        public static bool Play(AudioClip clip, float pitch = 1f)
        {
            if (_muted || _instance == null || _instance._bank == null || clip == null)
            {
                return false;
            }
            AudioSource voice = _instance._voices[_instance._next];
            _instance._next = (_instance._next + 1) % Voices;
            voice.pitch = pitch;
            voice.PlayOneShot(clip, _instance._bank.EffectsVolume);
            PlayedCount++;
            return true;
        }

        private void Init(SoundBank bank)
        {
            _instance = this;
            _bank = bank;
            if (_voices == null)
            {
                _voices = new AudioSource[Voices];
                for (int i = 0; i < Voices; i++)
                {
                    _voices[i] = gameObject.AddComponent<AudioSource>();
                    _voices[i].playOnAwake = false;
                }
                _music = gameObject.AddComponent<AudioSource>();
                _music.playOnAwake = false;
                _music.loop = true;
            }
            // The scene camera has no listener (screens are UI only), so bring one.
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
            _music.mute = _muted;
            if (bank != null && bank.Music != null)
            {
                _music.clip = bank.Music;
                _music.volume = bank.MusicVolume;
                _music.Play();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

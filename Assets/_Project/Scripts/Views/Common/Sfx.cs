using System;
using PuzzleKingdoms.Data;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Plays sound effects and the music from the <see cref="SoundBank"/> (PB-8). One instance lives on the
    /// GameApp object; screens call the static methods. Music has a theme per kind of screen and cross-fades
    /// between them (PB-33): two music players, one fading out while the other fades in, so only one or two
    /// streams ever play. Without a bank, or while muted, nothing plays.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        /// <summary>Effects can overlap (a cascade, a crack and a ding); each voice keeps its own pitch.</summary>
        private const int Voices = 8;

        private static Sfx _instance;
        private static bool _effectsOn = true;
        private static bool _musicOn = true;

        private SoundBank _bank;
        private AudioSource[] _voices;
        private AudioSource[] _music;
        private int _active;
        private int _next;
        private AudioClip _playing;
        private float _fade = 1f;

        /// <summary>The sounds in use, or null when there are none.</summary>
        public static SoundBank Bank => _instance != null ? _instance._bank : null;

        /// <summary>How many effects actually started playing (tests use it to check mute).</summary>
        public static int PlayedCount { get; private set; }

        /// <summary>The theme last asked for (also when it has no clip of its own), or null before the first.</summary>
        public static MusicTheme? CurrentTheme { get; private set; }

        /// <summary>The clip that is playing (or fading in) as music; null when none.</summary>
        public static AudioClip CurrentMusic => _instance != null ? _instance._playing : null;

        /// <summary>True while one music fades into another.</summary>
        public static bool IsFading => _instance != null && _instance._fade < 1f;

        /// <summary>Raised when effects or music are switched on or off, so the settings can be saved (PB-14).</summary>
        public static event Action Changed;

        /// <summary>Sound effects on or off (settings: "Sound effects").</summary>
        public static bool EffectsOn
        {
            get => _effectsOn;
            set => Set(value, _musicOn);
        }

        /// <summary>The music on or off (settings: "Music").</summary>
        public static bool MusicOn
        {
            get => _musicOn;
            set => Set(_effectsOn, value);
        }

        /// <summary>Everything silent (the puzzle screen's sound button switches both).</summary>
        public static bool Muted
        {
            get => !_effectsOn && !_musicOn;
            set => Set(!value, !value);
        }

        private static void Set(bool effectsOn, bool musicOn)
        {
            bool changed = effectsOn != _effectsOn || musicOn != _musicOn;
            _effectsOn = effectsOn;
            _musicOn = musicOn;
            if (_instance != null && _instance._music != null)
            {
                foreach (AudioSource source in _instance._music)
                {
                    source.mute = !musicOn;
                }
            }
            if (changed)
            {
                Changed?.Invoke();
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
            if (!_effectsOn || _instance == null || _instance._bank == null || clip == null)
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

        /// <summary>
        /// Switches the music to the theme of the screen now showing, with a cross-fade. Asking for the theme that
        /// already plays (or one that resolves to the same clip) changes nothing.
        /// </summary>
        public static void PlayTheme(MusicTheme theme)
        {
            CurrentTheme = theme;
            if (_instance != null)
            {
                _instance.StartMusic(theme, false);
            }
        }

        private void StartMusic(MusicTheme theme, bool instant)
        {
            AudioClip clip = _bank != null ? _bank.ClipFor(theme) : null;
            if (clip == null || clip == _playing)
            {
                return;
            }
            AudioSource from = _music[_active];
            _active = 1 - _active;
            AudioSource to = _music[_active];
            _playing = clip;
            to.clip = clip;
            to.loop = theme != MusicTheme.Results;
            to.volume = instant ? _bank.MusicVolume : 0f;
            to.Play();
            _fade = instant || !from.isPlaying ? 1f : 0f;
            if (_fade >= 1f)
            {
                from.Stop();
                to.volume = _bank.MusicVolume;
            }
        }

        private void Update()
        {
            if (_fade >= 1f || _bank == null)
            {
                return;
            }
            // Real time: the fade must not stand still when a screen pauses the game.
            _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / Mathf.Max(0.05f, _bank.MusicFadeSeconds));
            _music[_active].volume = _bank.MusicVolume * _fade;
            _music[1 - _active].volume = _bank.MusicVolume * (1f - _fade);
            if (_fade >= 1f)
            {
                _music[1 - _active].Stop();
            }
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
                _music = new AudioSource[2];
                for (int i = 0; i < _music.Length; i++)
                {
                    _music[i] = gameObject.AddComponent<AudioSource>();
                    _music[i].playOnAwake = false;
                    _music[i].loop = true;
                }
            }
            // The scene camera has no listener (screens are UI only), so bring one.
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
            foreach (AudioSource source in _music)
            {
                source.mute = !_musicOn;
            }
            _playing = null;
            StartMusic(CurrentTheme ?? MusicTheme.Map, true);
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

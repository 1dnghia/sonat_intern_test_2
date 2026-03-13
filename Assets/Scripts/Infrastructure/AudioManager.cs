using TapAway.Infrastructure;
using UnityEngine;

namespace TapAway.Infrastructure
{
    /// <summary>
    /// Manages background music (BGM) and sound effects (SFX).
    /// Attach to an AudioManager GameObject in the scene.
    /// Volumes are persisted via PlayerPrefs.
    /// </summary>
    public class AudioManager : SingletonMonoBehaviour<AudioManager>
    {
        // ── Constants ─────────────────────────────────────────
        private const string PREFS_MASTER = "Vol_Master";
        private const string PREFS_BGM    = "Vol_BGM";
        private const string PREFS_SFX    = "Vol_SFX";

        // ── Serialized Fields ──────────────────────────────────
        [SerializeField, Tooltip("AudioSource used for looping background music")]
        private AudioSource _bgmSource;

        [SerializeField, Tooltip("AudioSource used for one-shot sound effects")]
        private AudioSource _sfxSource;

        // ── Properties ────────────────────────────────────────
        public float MasterVolume { get; private set; }
        public float BGMVolume    { get; private set; }
        public float SFXVolume    { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            MasterVolume = PlayerPrefs.GetFloat(PREFS_MASTER, 1f);
            BGMVolume    = PlayerPrefs.GetFloat(PREFS_BGM,    0.7f);
            SFXVolume    = PlayerPrefs.GetFloat(PREFS_SFX,    1f);
            ApplyVolumes();
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Starts playing BGM. Loops automatically.</summary>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || _bgmSource == null) return;
            _bgmSource.clip = clip;
            _bgmSource.loop = true;
            _bgmSource.Play();
        }

        public void StopBGM()
        {
            if (_bgmSource != null) _bgmSource.Stop();
        }

        /// <summary>Plays a one-shot SFX clip.</summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip, MasterVolume * SFXVolume);
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREFS_MASTER, MasterVolume);
            ApplyVolumes();
        }

        public void SetBGMVolume(float volume)
        {
            BGMVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREFS_BGM, BGMVolume);
            ApplyVolumes();
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREFS_SFX, SFXVolume);
        }

        // ── Private ───────────────────────────────────────────

        private void ApplyVolumes()
        {
            if (_bgmSource != null)
                _bgmSource.volume = MasterVolume * BGMVolume;
        }
    }
}

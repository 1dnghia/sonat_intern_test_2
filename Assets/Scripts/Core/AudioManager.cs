using UnityEngine;
using TapAway.Core;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TapAway
{
    public class AudioManager : SingletonMonoBehaviour<AudioManager>
    {
        private enum BgmState
        {
            None = 0,
            Gameplay = 1,
            Win = 2,
            Lose = 3,
        }

        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Addressables")]
        [SerializeField] private AssetReferenceT<AudioClip> _gameplayBgmRef;
        [SerializeField] private AssetReferenceT<AudioClip> _winBgmRef;
        [SerializeField] private AssetReferenceT<AudioClip> _loseBgmRef;
        [SerializeField] private AssetReferenceT<AudioClip> _tapNormalSfxRef;
        [SerializeField] private AssetReferenceT<AudioClip> _tapRotatorSfxRef;
        [SerializeField] private AssetReferenceT<AudioClip> _normalHitGearSfxRef;
        [SerializeField] private AssetReferenceT<AudioClip> _bombExplodeSfxRef;
        [SerializeField] private AssetReferenceT<AudioClip> _uiClickSfxRef;

        private readonly System.Collections.Generic.List<AsyncOperationHandle<AudioClip>> _loadedClipHandles
            = new System.Collections.Generic.List<AsyncOperationHandle<AudioClip>>();

        private AudioClip _gameplayBgm;
        private AudioClip _winBgm;
        private AudioClip _loseBgm;
        private AudioClip _tapNormalSfx;
        private AudioClip _tapRotatorSfx;
        private AudioClip _normalHitGearSfx;
        private AudioClip _bombExplodeSfx;
        private AudioClip _uiClickSfx;
        private bool _hasRequestedGameplayBgmBeforeLoad;
        private BgmState _currentBgmState;

        protected override void Awake()
        {
            base.Awake();
            EnsureAudioSources();
            RefreshMuteState();

            // Audio chạy theo Addressables-only để đồng bộ pipeline content.
            StartCoroutine(PreloadAddressableAudio());
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _loadedClipHandles.Count; i++)
            {
                AsyncOperationHandle<AudioClip> handle = _loadedClipHandles[i];
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _loadedClipHandles.Clear();
        }

        public void RefreshMuteState()
        {
            if (_bgmSource != null)
            {
                _bgmSource.mute = !GameSettingsStore.IsMusicEnabled;
            }

            if (_sfxSource != null)
            {
                _sfxSource.mute = !GameSettingsStore.IsSoundEnabled;
            }
        }

        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (_bgmSource == null || clip == null)
            {
                return;
            }

            RefreshMuteState();
            _bgmSource.loop = loop;
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            RefreshMuteState();
            _sfxSource.PlayOneShot(clip);
        }

        public void PlayGameplayBgm()
        {
            if (_gameplayBgm == null)
            {
                Debug.LogWarning("[AudioManager] Gameplay BGM is not loaded yet or missing Addressable reference.", this);
                _hasRequestedGameplayBgmBeforeLoad = true;
                return;
            }

            // Đang chạy gameplay BGM rồi thì giữ nguyên để retry/reload level không bị restart nhạc.
            if (_currentBgmState == BgmState.Gameplay
                && _bgmSource != null
                && _bgmSource.isPlaying
                && _bgmSource.clip == _gameplayBgm)
            {
                return;
            }

            PlayBgm(_gameplayBgm);
            _currentBgmState = BgmState.Gameplay;
        }

        public void PlayWinBgm()
        {
            PlayBgm(_winBgm);
            _currentBgmState = BgmState.Win;
        }

        public void PlayLoseBgm()
        {
            PlayBgm(_loseBgm);
            _currentBgmState = BgmState.Lose;
        }

        public void PlayTapNormal()
        {
            PlaySfx(_tapNormalSfx);
        }

        public void PlayTapRotator()
        {
            PlaySfx(_tapRotatorSfx);
        }

        public void PlayNormalHitGear()
        {
            PlaySfx(_normalHitGearSfx);
        }

        public void PlayBombExplode()
        {
            PlaySfx(_bombExplodeSfx);
        }

        public void PlayUiClick()
        {
            PlaySfx(_uiClickSfx);
        }

        private System.Collections.IEnumerator PreloadAddressableAudio()
        {
            // Load tuần tự để kiểm soát thứ tự và dễ debug khi thiếu reference.
            yield return LoadClipFromAddressable(_gameplayBgmRef, "GameplayBgm", clip => _gameplayBgm = clip);
            yield return LoadClipFromAddressable(_winBgmRef, "WinBgm", clip => _winBgm = clip);
            yield return LoadClipFromAddressable(_loseBgmRef, "LoseBgm", clip => _loseBgm = clip);
            yield return LoadClipFromAddressable(_tapNormalSfxRef, "TapNormal", clip => _tapNormalSfx = clip);
            yield return LoadClipFromAddressable(_tapRotatorSfxRef, "TapRotator", clip => _tapRotatorSfx = clip);
            yield return LoadClipFromAddressable(_normalHitGearSfxRef, "NormalHitGear", clip => _normalHitGearSfx = clip);
            yield return LoadClipFromAddressable(_bombExplodeSfxRef, "BombExplode", clip => _bombExplodeSfx = clip);
            yield return LoadClipFromAddressable(_uiClickSfxRef, "UiClick", clip => _uiClickSfx = clip);

            if (_hasRequestedGameplayBgmBeforeLoad && _gameplayBgm != null)
            {
                _hasRequestedGameplayBgmBeforeLoad = false;
                PlayBgm(_gameplayBgm);
            }
        }

        private System.Collections.IEnumerator LoadClipFromAddressable(
            AssetReferenceT<AudioClip> clipReference,
            string clipLabel,
            System.Action<AudioClip> onLoaded)
        {
            if (clipReference == null || !clipReference.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"[AudioManager] Missing Addressable AudioClip reference: {clipLabel}", this);
                yield break;
            }

            AsyncOperationHandle<AudioClip> handle = clipReference.LoadAssetAsync<AudioClip>();
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onLoaded?.Invoke(handle.Result);
                _loadedClipHandles.Add(handle);
                yield break;
            }

            Debug.LogWarning($"[AudioManager] Failed to load Addressable AudioClip: {clipLabel}", this);
        }

        private void EnsureAudioSources()
        {
            if (_bgmSource == null)
            {
                _bgmSource = GetComponent<AudioSource>();
            }

            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
            }

            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;

            if (_sfxSource == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != _bgmSource)
                    {
                        _sfxSource = sources[i];
                        break;
                    }
                }
            }

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }

            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;
        }
    }
}

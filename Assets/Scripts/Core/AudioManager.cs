using UnityEngine;

namespace TapAway
{
    public class AudioManager : SingletonMonoBehaviour<AudioManager>
    {
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (_bgmSource == null || clip == null)
            {
                return;
            }

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

            _sfxSource.PlayOneShot(clip);
        }
    }
}

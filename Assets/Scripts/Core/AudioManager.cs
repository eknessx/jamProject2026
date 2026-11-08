using UnityEngine;

namespace Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource.clip == clip) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}

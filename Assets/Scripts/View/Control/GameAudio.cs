using UnityEngine;

namespace View.Control
{
    /// <summary>
    /// The main controller for the game's audio. Handles SFX along with looping music.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        private const string MusicStatusKey = "music.status";
        private const string SfxStatusKey = "sfx.status";
        
        private const float MusicVolume = 0.2f;
        
        public AudioClip[] MusicClips;
        public AudioClip[] SfxClips;

        private AudioSource _musicSource;
        private int _musicVolumeTweenId;
        private bool _musicEnabled = true;
        public bool MusicEnabled
        {
            get { return _musicEnabled; }
            set {
                if (_musicEnabled != value) {
                    LeanTween.cancel(_musicVolumeTweenId);
                }
                
                _musicEnabled = value;
                _musicSource.volume = value ? MusicVolume : 0f;
                PlayerPrefs.SetInt(MusicStatusKey, value ? 0 : 1);
            }
        }

        private bool _sfxEnabled = true;
        public bool SfxEnabled
        {
            get { return _sfxEnabled; }
            set {
                _sfxEnabled = value;
                PlayerPrefs.SetInt(SfxStatusKey, value ? 0 : 1);
            }
        }

        private void Start()
        {
            StartMusic();

            if (!PlayerPrefs.HasKey(MusicStatusKey)) {
                PlayerPrefs.SetInt(MusicStatusKey, 0);
            }
            if (!PlayerPrefs.HasKey(SfxStatusKey)) {
                PlayerPrefs.SetInt(SfxStatusKey, 0);
            }

            MusicEnabled = PlayerPrefs.GetInt(MusicStatusKey) == 0;
            SfxEnabled = PlayerPrefs.GetInt(SfxStatusKey) == 0;
        }

        /// <summary>
        /// Plays the given sound clip with the specified parameters.
        /// </summary>
        public void Play(GameClip clip, float delay = 0f, float volume = 1f, float startTime = 0f)
{
    if (!enabled || !SfxEnabled) return;
    NodulusAudioManager.Instance?.PlaySFX(clip.ToString());
}

public void Play(MusicClip clip, float fadeTime = 0f, float delay = 0f, float volume = 1f, float startTime = 0f)
{
    if (!enabled || !MusicEnabled) return;
    NodulusAudioManager.Instance?.StartAmbientMusic();
}

private void StartMusic()
{
    NodulusAudioManager.Instance?.StartAmbientMusic();
}
    }

    /// <summary>
    /// A one-to-one map of all sound clips
    /// </summary>
    public enum GameClip
    {
        GameStart,
        WinBoard,
        NodeEnter,
        NodeLeave,
        MovePushHigh,
        MovePullHigh,
        MovePullMid,
        MovePushMid,
        MovePullLow,
        MovePushLow,
        ArcMoveHigh,
        NodeRotate90,
        InvalidRotate,
        MenuSelect,
        GameEnd,
        LevelEnable,
        ArcMoveMid,
        ArcMoveLow
    }

    /// <summary>
    /// A one-to-one map of all music clips
    /// </summary>
    public enum MusicClip
    {
        Ambient01,
        Ambient02
    }
}

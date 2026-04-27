using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodulusAudioManager : MonoBehaviour
{
    public static NodulusAudioManager Instance;

    [Header("Voces SFX")]
    public OSC[] sfxVoices;
    private int sfxIndex = 0;

    [Header("Voz Música")]
    public OSC musicVoice;

    private bool musicEnabled = true;
    private Coroutine musicCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartAmbientMusic();
    }

    // ==========================================
    // 🎵 MÚSICA AMBIENT — Wavetable
    // ==========================================
   private (string note, float duration)[] ambientMelody = new (string, float)[]
{
    ("G4",  0.5f), ("A4",  0.5f), ("B4",  0.5f), ("C5",  0.5f),
    ("B4",  0.5f), ("A4",  0.5f), ("G4",  1.0f),
    ("F4",  0.5f), ("G4",  0.5f), ("A4",  0.5f), ("G4",  0.5f),
    ("F4",  0.5f), ("E4",  0.5f), ("D4",  1.0f),
    ("D4",  0.5f), ("E4",  0.5f), ("F4",  0.5f), ("G4",  0.5f),
    ("A4",  0.5f), ("B4",  0.5f), ("C5",  1.5f),
    ("B4",  0.5f), ("A4",  0.5f), ("G4",  0.5f), ("F4",  0.5f),
    ("E4",  0.5f), ("D4",  0.5f), ("C4",  2.0f),
    ("REST",1.0f),
};

    public void StartAmbientMusic()
    {
        if (musicVoice == null) return;
        if (musicCoroutine != null) StopCoroutine(musicCoroutine);
        musicCoroutine = StartCoroutine(PlayAmbient());
    }

    IEnumerator PlayAmbient()
{
    musicVoice.waveType     = OSC.WaveType.SA;
    musicVoice.Armonicos    = 4;
    musicVoice.AmplitudesSA = new float[10] { 1f, 0.4f, 0.2f, 0.1f, 0f, 0f, 0f, 0f, 0f, 0f };
    musicVoice.vibratoEnabled   = true;
    musicVoice.vibratoRate      = 4f;
    musicVoice.vibratoIntensity = 0.008f;
    musicVoice.A  = 80;
    musicVoice.D  = 100;
    musicVoice.SL = 0.7f;

    float secondsPerBeat = 60f / 110f; // 110 BPM

    while (true)
    {
        foreach (var (note, duration) in ambientMelody)
        {
            float noteDuration  = duration * secondsPerBeat;
            float soundDuration = noteDuration * 0.9f;

            if (note == "REST")
            {
                yield return new WaitForSeconds(noteDuration);
                continue;
            }

            float freq = NoteToFreq(note);
            if (freq > 0)
            {
                musicVoice.S = Mathf.RoundToInt(soundDuration * 1000f);
                musicVoice.UpdateADSR();
                musicVoice.f         = freq;
                musicVoice.TimeIndex = 0;
                musicVoice.Aud.volume = 0.3f;
                musicVoice.Aud.Play();
            }

            yield return new WaitForSeconds(soundDuration);
            musicVoice.Aud.Stop();
            yield return new WaitForSeconds(noteDuration - soundDuration);
        }
    }
}

float NoteToFreq(string note)
{
    switch (note)
    {
        case "C4": return 261.63f;
        case "D4": return 293.66f;
        case "E4": return 329.63f;
        case "F4": return 349.23f;
        case "G4": return 392.00f;
        case "A4": return 440.00f;
        case "B4": return 493.88f;
        case "C5": return 523.25f;
        case "D5": return 587.33f;
        case "E5": return 659.25f;
        default:   return 0f;
    }
}

    // ==========================================
    // 🔊 PLAY SFX
    // ==========================================
    public void PlaySFX(string eventName)
    {
        OSC voice = GetVoice();
        if (voice == null) return;
        ApplyPreset(voice, eventName);
        voice.Aud.volume = 0.8f;
        voice.PlayOneShot();
    }

    OSC GetVoice()
    {
        if (sfxVoices == null || sfxVoices.Length == 0) return null;
        OSC v = sfxVoices[sfxIndex % sfxVoices.Length];
        sfxIndex++;
        return v;
    }

    // ==========================================
    // 🎨 PRESETS POR EVENTO
    // ==========================================
    void ApplyPreset(OSC v, string eventName)
    {
        v.fmEnabled      = false;
        v.vibratoEnabled = false;
        v.tremoloEnabled = false;

        switch (eventName)
        {
            case "NodeEnter":
                // Sine suave — entrar a un nodo
                v.SetSFX(880f, OSC.WaveType.Sine, 10, 50, 80, 0.6f);
                break;

            case "NodeLeave":
                // Sine descendente — salir de un nodo
                v.SetSFX(660f, OSC.WaveType.Sine, 5, 30, 60, 0.5f);
                break;

            case "MovePushHigh":
            case "MovePullHigh":
                // FM — movimiento pieza alta
                v.waveType     = OSC.WaveType.SA;
                v.f            = 523f;
                v.Armonicos    = 4;
                v.AmplitudesSA = new float[10] { 1f, 0.5f, 0.2f, 0.1f, 0f, 0f, 0f, 0f, 0f, 0f };
                v.A = 5; v.D = 80; v.S = 100; v.SL = 0.4f;
                v.vibratoEnabled   = true;
                v.vibratoRate      = 8f;
                v.vibratoIntensity = 0.012f;
                v.UpdateADSR();
                break;

            case "MovePushMid":
            case "MovePullMid":
                // FM — movimiento pieza media
                v.waveType     = OSC.WaveType.SA;
                v.f            = 392f;
                v.Armonicos    = 4;
                v.AmplitudesSA = new float[10] { 1f, 0.5f, 0.2f, 0.1f, 0f, 0f, 0f, 0f, 0f, 0f };
                v.A = 5; v.D = 80; v.S = 100; v.SL = 0.4f;
                v.vibratoEnabled   = true;
                v.vibratoRate      = 8f;
                v.vibratoIntensity = 0.012f;
                v.UpdateADSR();
                break;

            case "MovePushLow":
            case "MovePullLow":
                // FM — movimiento pieza baja
                v.waveType     = OSC.WaveType.SA;
                v.f            = 261f;
                v.Armonicos    = 4;
                v.AmplitudesSA = new float[10] { 1f, 0.5f, 0.2f, 0.1f, 0f, 0f, 0f, 0f, 0f, 0f };
                v.A = 5; v.D = 80; v.S = 100; v.SL = 0.4f;
                v.vibratoEnabled   = true;
                v.vibratoRate      = 8f;
                v.vibratoIntensity = 0.012f;
                v.UpdateADSR();
                break;

            case "ArcMoveHigh":
                // Síntesis aditiva — arco girando
                v.waveType     = OSC.WaveType.SA;
                v.f            = 523f;
                v.Armonicos    = 5;
                v.AmplitudesSA = new float[10] { 1f, 0.6f, 0.3f, 0.15f, 0.08f, 0f, 0f, 0f, 0f, 0f };
                v.A = 10; v.D = 60; v.S = 120; v.SL = 0.5f;
                v.UpdateADSR();
                break;

            case "ArcMoveMid":
                v.waveType     = OSC.WaveType.SA;
                v.f            = 392f;
                v.Armonicos    = 5;
                v.AmplitudesSA = new float[10] { 1f, 0.6f, 0.3f, 0.15f, 0.08f, 0f, 0f, 0f, 0f, 0f };
                v.A = 10; v.D = 60; v.S = 120; v.SL = 0.5f;
                v.UpdateADSR();
                break;

            case "ArcMoveLow":
                v.waveType     = OSC.WaveType.SA;
                v.f            = 261f;
                v.Armonicos    = 5;
                v.AmplitudesSA = new float[10] { 1f, 0.6f, 0.3f, 0.15f, 0.08f, 0f, 0f, 0f, 0f, 0f };
                v.A = 10; v.D = 60; v.S = 120; v.SL = 0.5f;
                v.UpdateADSR();
                break;

            case "NodeRotate":
                // Square — rotación mecánica
                v.SetSFX(659f, OSC.WaveType.Sine, 5, 60, 80, 0.5f);
                break;

            case "InvalidRotate":
                // FM disonante — error
                v.SetSFX(150f, OSC.WaveType.Sine, 5, 60, 80, 0.7f,
                         fm: true, fmRatio: 1.5f, fmIndex: 4f);
                break;

            case "GameStart":
                // Síntesis aditiva brillante con vibrato
                v.waveType     = OSC.WaveType.SA;
                v.f            = 523f;
                v.Armonicos    = 6;
                v.AmplitudesSA = new float[10] { 1f, 0.8f, 0.5f, 0.3f, 0.2f, 0.1f, 0f, 0f, 0f, 0f };
                v.A = 50; v.D = 100; v.S = 400; v.SL = 0.8f;
                v.vibratoEnabled   = true;
                v.vibratoRate      = 6f;
                v.vibratoIntensity = 0.01f;
                v.UpdateADSR();
                break;

            case "WinBoard":
                // Wavetable — victoria con tremolo
                v.waveType     = OSC.WaveType.SA;
                v.f            = 659f;
                v.Armonicos    = 8;
                v.AmplitudesSA = new float[10] { 1f, 0.9f, 0.7f, 0.5f, 0.3f, 0.2f, 0.1f, 0.05f, 0f, 0f };
                v.A = 30; v.D = 80; v.S = 600; v.SL = 0.9f;
                v.tremoloEnabled   = true;
                v.tremoloRate      = 8f;
                v.tremoloIntensity = 0.3f;
                v.UpdateADSR();
                break;

            case "GameEnd":
                v.SetSFX(330f, OSC.WaveType.Sine, 100, 200, 500, 0.7f,
                         fm: true, fmRatio: 1.5f, fmIndex: 0.5f);
                break;

            case "MenuSelect":
                // Sine corto — UI click
                v.SetSFX(660f, OSC.WaveType.Sine, 5, 20, 40, 0.5f);
                break;

            case "LevelEnable":
                // Triangle suave — nivel desbloqueado
                v.SetSFX(440f, OSC.WaveType.Triangle, 20, 60, 100, 0.6f);
                break;

            default:
                v.SetSFX(440f, OSC.WaveType.Sine, 5, 30, 60, 0.5f);
                break;
        }
    }
}
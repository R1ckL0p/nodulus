using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodulusAudioManager : MonoBehaviour
{
    public static NodulusAudioManager Instance;

    [Header("Voces SFX (pool de 6)")]
    public OSC[] sfxVoices;
    private int sfxIndex = 0;

    [Header("Voz Música")]
    public OSC musicVoice;

    [Header("Configuración")]
    [Range(40f, 120f)] public float musicBPM = 80f;
    [Range(0f, 1f)] public float musicVolume = 0.12f; // Música más "pasito"
    [Range(0f, 1f)] public float sfxVolume = 0.70f;

    private Coroutine musicCoroutine;

    // ==========================================
    // 🎵 TABLA DE NOTAS — Melodía Armónica (Sol Mayor)
    // ==========================================
    private (string note, float beats)[] ambientMelody = new (string, float)[]
    {
        ("G3", 2.0f), ("B3", 2.0f), ("D4", 2.0f), ("G4", 2.0f),
        ("E4", 2.0f), ("C4", 2.0f), ("G3", 4.0f),
        ("D4", 2.0f), ("F#4", 2.0f), ("A4", 2.0f), ("D4", 2.0f),
        ("C4", 2.0f), ("B3", 2.0f), ("A3", 4.0f),
        ("REST", 2.0f)
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartAmbientMusic();
    }

    // ==========================================
    // 🎵 MÚSICA AMBIENT (Mejorada para ser continua)
    // ==========================================
    public void StartAmbientMusic()
    {
        if (musicVoice == null) return;
        if (musicCoroutine != null) StopCoroutine(musicCoroutine);
        musicCoroutine = StartCoroutine(PlayAmbient());
    }

    IEnumerator PlayAmbient()
    {
        // Configuración de Pad Atmosférico (Síntesis Aditiva Suave)
        musicVoice.waveType = OSC.WaveType.Sine;
        musicVoice.Armonicos = 3;
        musicVoice.AmplitudesSA = new float[10] { 1f, 0.2f, 0.05f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };

        // LFO para calidez
        musicVoice.vibratoEnabled = true;
        musicVoice.vibratoRate = 2.5f;
        musicVoice.vibratoIntensity = 0.004f;

        // ADSR para Legato: Attack y Decay largos para transiciones invisibles
        musicVoice.A = 250;
        musicVoice.D = 400;
        musicVoice.SL = 0.8f;

        float spb = 60f / musicBPM;

        while (true)
        {
            foreach (var (note, beats) in ambientMelody)
            {
                float noteDur = beats * spb;

                if (note == "REST")
                {
                    yield return new WaitForSeconds(noteDur);
                    continue;
                }

                float freq = NoteToFreq(note);
                if (freq <= 0f) { yield return new WaitForSeconds(noteDur); continue; }

                // CAMBIO CLAVE: No hay Stop(). La nota fluye a la siguiente.
                musicVoice.S = Mathf.RoundToInt(noteDur * 1000f);
                musicVoice.UpdateADSR();

                musicVoice.f = freq;
                musicVoice.TimeIndex = 0;
                musicVoice.Aud.volume = musicVolume;

                if (!musicVoice.Aud.isPlaying) musicVoice.Aud.Play();

                yield return new WaitForSeconds(noteDur);
            }
        }
    }

    static float NoteToFreq(string note)
    {
        switch (note)
        {
            case "G3": return 196.00f;
            case "A3": return 220.00f;
            case "B3": return 246.94f;
            case "C4": return 261.63f;
            case "D4": return 293.66f;
            case "E4": return 329.63f;
            case "F#4": return 369.99f;
            case "G4": return 392.00f;
            case "A4": return 440.00f;
            case "B4": return 493.88f;
            case "C5": return 523.25f;
            case "D5": return 587.33f;
            default: return 0f;
        }
    }

    // ==========================================
    // 🔊 PLAY SFX — API pública
    // ==========================================
    public void PlaySFX(string eventName)
    {
        OSC voice = GetVoice();
        if (voice == null) return;

        voice.fmEnabled = false;
        voice.vibratoEnabled = false;
        voice.tremoloEnabled = false;
        voice.detune = 0f;

        ApplyPreset(voice, eventName);
        voice.Aud.volume = sfxVolume;
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
    // 🎨 PRESETS POR EVENTO (Lógica original completa)
    // ==========================================
    void ApplyPreset(OSC v, string eventName)
    {
        switch (eventName)
        {
            case "NodeEnter":
                v.SetSFX(880f, OSC.WaveType.Sine, 8, 50, 80, 0.6f);
                break;
            case "NodeLeave":
                v.SetSFX(660f, OSC.WaveType.Sine, 5, 30, 60, 0.5f);
                break;
            case "NodeSelect":
                v.SetSFX(740f, OSC.WaveType.Sine, 5, 25, 50, 0.55f);
                break;
            case "NodeDeselect":
                v.SetSFX(580f, OSC.WaveType.Sine, 5, 20, 40, 0.45f);
                break;

            case "MovePushHigh":
            case "MovePullHigh":
                v.waveType = OSC.WaveType.SA;
                v.f = 523f;
                v.Armonicos = 4;
                v.AmplitudesSA = new float[10] { 1f, 0.5f, 0.2f, 0.08f, 0f, 0f, 0f, 0f, 0f, 0f };
                v.A = 5; v.D = 80; v.S = 100; v.SL = 0.4f;
                v.vibratoEnabled = true; v.vibratoRate = 8f; v.vibratoIntensity = 0.012f;
                v.UpdateADSR();
                break;

            case "MovePushMid":
            case "MovePullMid":
                v.waveType = OSC.WaveType.SA;
                v.f = 392f;
                v.Armonicos = 4;
                v.AmplitudesSA = new float[10] { 1f, 0.5f, 0.2f, 0.08f, 0f, 0f, 0f, 0f, 0f, 0f };
                v.A = 5; v.D = 80; v.S = 100; v.SL = 0.4f;
                v.vibratoEnabled = true; v.vibratoRate = 8f; v.vibratoIntensity = 0.012f;
                v.UpdateADSR();
                break;

            case "MovePushLow":
            case "MovePullLow":
                v.waveType = OSC.WaveType.SA;
                v.f = 261f;
                v.Armonicos = 4;
                v.AmplitudesSA = new float[10] { 1f, 0.5f, 0.2f, 0.08f, 0f, 0f, 0f, 0f, 0f, 0f };
                v.A = 5; v.D = 80; v.S = 100; v.SL = 0.4f;
                v.vibratoEnabled = true; v.vibratoRate = 8f; v.vibratoIntensity = 0.012f;
                v.UpdateADSR();
                break;

            case "ArcMoveHigh":
                v.waveType = OSC.WaveType.SA;
                v.f = 523f;
                v.Armonicos = 5;
                v.AmplitudesSA = new float[10] { 1f, 0.6f, 0.3f, 0.15f, 0.06f, 0f, 0f, 0f, 0f, 0f };
                v.A = 10; v.D = 60; v.S = 120; v.SL = 0.5f;
                v.UpdateADSR();
                break;

            case "ArcMoveMid":
                v.waveType = OSC.WaveType.SA;
                v.f = 392f;
                v.Armonicos = 5;
                v.AmplitudesSA = new float[10] { 1f, 0.6f, 0.3f, 0.15f, 0.06f, 0f, 0f, 0f, 0f, 0f };
                v.A = 10; v.D = 60; v.S = 120; v.SL = 0.5f;
                v.UpdateADSR();
                break;

            case "ArcMoveLow":
                v.waveType = OSC.WaveType.SA;
                v.f = 261f;
                v.Armonicos = 5;
                v.AmplitudesSA = new float[10] { 1f, 0.6f, 0.3f, 0.15f, 0.06f, 0f, 0f, 0f, 0f, 0f };
                v.A = 10; v.D = 60; v.S = 120; v.SL = 0.5f;
                v.UpdateADSR();
                break;

            case "NodeRotate":
                v.SetSFX(659f, OSC.WaveType.Sine, 5, 60, 80, 0.5f);
                break;
            case "InvalidRotate":
                v.SetSFX(140f, OSC.WaveType.Sine, 5, 80, 100, 0.65f, fm: true, fmRatio: 1.5f, fmIndex: 4f);
                break;

            case "GameStart":
                v.waveType = OSC.WaveType.SA; v.f = 523f; v.Armonicos = 6;
                v.AmplitudesSA = new float[10] { 1f, 0.8f, 0.5f, 0.3f, 0.15f, 0.08f, 0f, 0f, 0f, 0f };
                v.A = 50; v.D = 100; v.S = 400; v.SL = 0.8f;
                v.vibratoEnabled = true; v.vibratoRate = 6f; v.vibratoIntensity = 0.010f;
                v.UpdateADSR();
                break;

            case "WinBoard":
                v.waveType = OSC.WaveType.SA; v.f = 659f; v.Armonicos = 8;
                v.AmplitudesSA = new float[10] { 1f, 0.9f, 0.7f, 0.5f, 0.3f, 0.2f, 0.1f, 0.05f, 0f, 0f };
                v.A = 30; v.D = 80; v.S = 700; v.SL = 0.9f;
                v.tremoloEnabled = true; v.tremoloRate = 7f; v.tremoloIntensity = 0.28f;
                v.UpdateADSR();
                break;

            case "GameEnd":
                v.SetSFX(330f, OSC.WaveType.Sine, 100, 200, 500, 0.7f, fm: true, fmRatio: 1.5f, fmIndex: 0.5f);
                break;
            case "LevelComplete":
                v.waveType = OSC.WaveType.SA; v.f = 587f; v.Armonicos = 5;
                v.AmplitudesSA = new float[10] { 1f, 0.7f, 0.4f, 0.2f, 0.1f, 0f, 0f, 0f, 0f, 0f };
                v.A = 20; v.D = 80; v.S = 300; v.SL = 0.75f;
                v.vibratoEnabled = true; v.vibratoRate = 5f; v.vibratoIntensity = 0.008f;
                v.UpdateADSR();
                break;

            case "MenuSelect":
                v.SetSFX(660f, OSC.WaveType.Sine, 5, 20, 40, 0.5f);
                break;
            case "MenuBack":
                v.SetSFX(500f, OSC.WaveType.Sine, 5, 20, 40, 0.45f);
                break;
            case "LevelEnable":
                v.SetSFX(440f, OSC.WaveType.Triangle, 20, 60, 120, 0.6f);
                break;
            case "MenuOpen":
                v.waveType = OSC.WaveType.SA; v.f = 392f; v.Armonicos = 3;
                v.AmplitudesSA = new float[10] { 1f, 0.3f, 0.1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
                v.A = 30; v.D = 60; v.S = 80; v.SL = 0.4f;
                v.UpdateADSR();
                break;
            case "MenuClose":
                v.SetSFX(349f, OSC.WaveType.Sine, 10, 40, 60, 0.4f);
                break;

            default:
                v.SetSFX(440f, OSC.WaveType.Sine, 5, 30, 60, 0.5f);
                break;
        }
    }
}
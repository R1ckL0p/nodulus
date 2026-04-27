using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OSC : MonoBehaviour
{
    public float FM = 44100f, f = 440;
    public AudioSource Aud;

    public enum WaveType { Sine, Square, Triangle, Saw, SA }
    public WaveType waveType = WaveType.Sine;

    public int Armonicos = 10;
    public float[] AmplitudesSA = new float[10];

    // ==========================================
    // 🎛️ DETUNE
    // ==========================================
    [Header("Detune")]
    public float detune = 0f;

    float GetDetunedFrequency()
    {
        return f * Mathf.Pow(2f, detune / 1200f);
    }

    // ==========================================
    // 🌊 VIBRATO
    // ==========================================
    [Header("Vibrato")]
    public bool vibratoEnabled = false;
    public float vibratoRate      = 5f;
    public float vibratoIntensity = 0.01f;

    float VibratoMod(int t)
    {
        if (!vibratoEnabled) return 1f;
        return 1f + vibratoIntensity * Mathf.Sin(2 * Mathf.PI * vibratoRate * t / FM);
    }

    // ==========================================
    // 🔊 TREMOLO
    // ==========================================
    [Header("Tremolo")]
    public bool tremoloEnabled = false;
    public float tremoloRate      = 4f;
    public float tremoloIntensity = 0.5f;

    float TremoloMod(int t)
    {
        if (!tremoloEnabled) return 1f;
        return 1f - tremoloIntensity * 0.5f * (1f + Mathf.Sin(2 * Mathf.PI * tremoloRate * t / FM));
    }

    // ==========================================
    // 📡 FM SYNTHESIS
    // ==========================================
    [Header("FM Synthesis")]
    public bool fmEnabled = false;
    public float fmRatio  = 2f;
    public float fmIndex  = 1f;

    float FMWave(float freq, int t)
    {
        float modFreq   = freq * fmRatio;
        float modSignal = fmIndex * freq * Mathf.Sin(2 * Mathf.PI * modFreq * t / FM);
        return Mathf.Sin(2 * Mathf.PI * freq * t / FM + modSignal);
    }

    // ==========================================
    // 🚀 START
    // ==========================================
    void Awake()
    {
        if (Aud == null) Aud = GetComponent<AudioSource>();
        if (Aud == null) Aud = gameObject.AddComponent<AudioSource>();
        Aud.playOnAwake  = false;
        Aud.spatialBlend = 0f;
        Aud.volume       = 1f;

        for (int i = 0; i < AmplitudesSA.Length; i++)
            AmplitudesSA[i] = 1f;
    }

    // ==========================================
    // 🎵 GENERACIÓN DE ONDA
    // ==========================================
    float SineWave(float freq, int t)         => Mathf.Sin(2 * Mathf.PI * freq * t / FM);
    float SineWaveSA(float freq, int t, int n, float An) => Mathf.Sin(2 * Mathf.PI * n * freq * t / FM) * An;

    public float SA(float freq, int t, int armonicos, bool normalize = true)
    {
        float X = 0f, totalAmplitude = 0f;
        for (int n = 1; n <= armonicos; n++)
        {
            float An = AmplitudesSA[n - 1];
            X += SineWaveSA(freq, t, n, An);
            totalAmplitude += An;
        }
        return normalize && totalAmplitude > 0f ? X / totalAmplitude : Mathf.Clamp(X, -1f, 1f);
    }

    float SquareWave(float freq, int t)  => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * freq * t / FM));
    float TringleWave(float freq, int t) => Mathf.PingPong(t * freq / FM, 1f) * 2f - 1f;

    float LinearInterpolation(float x, float x0, float x1, float y0, float y1)
        => y0 + (y1 - y0) * (x - x0) / (x1 - x0);

    float SawWave(float freq, int t)
    {
        var T   = FM / freq;
        var mod = t % T;
        return LinearInterpolation(mod, 0, T, 1f, -1f);
    }

    bool useNormalize = true;

    float GenerateWave(WaveType type, float freq, int t)
    {
        if (fmEnabled) return FMWave(freq, t);

        switch (type)
        {
            case WaveType.Sine:     return SineWave(freq, t);
            case WaveType.Square:   return SquareWave(freq, t);
            case WaveType.Triangle: return TringleWave(freq, t);
            case WaveType.Saw:      return SawWave(freq, t);
            case WaveType.SA:       return SA(freq, t, Armonicos, useNormalize);
            default:                return 0f;
        }
    }

    // ==========================================
    // 🔁 AUDIO LOOP
    // ==========================================
    float X = 0f;
    public int TimeIndex = 0;

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            float freq  = GetDetunedFrequency() * VibratoMod(TimeIndex);
            float E     = getADSR(TimeIndex) * TremoloMod(TimeIndex);
            X           = GenerateWave(waveType, freq, TimeIndex);
            data[i]     = X * E;
            if (channels > 1) data[i + 1] = X * E;
            TimeIndex++;
        }
    }

    // ==========================================
    // 🎚️ ADSR
    // ==========================================
    public int A = 5, D = 5, S = 5;
    public float SL = 0.7f;

    private Dictionary<int, float> adsrCache = new Dictionary<int, float>();
    private bool adsrUpdate = true;

    public void UpdateADSR()
    {
        adsrCache.Clear();
        adsrUpdate = true;
    }

    float getADSR(int t)
    {
        if (adsrUpdate) { adsrCache.Clear(); adsrUpdate = false; }
        if (adsrCache.TryGetValue(t, out float value)) return value;

        int attack  = Mathf.RoundToInt((A / 1000f) * FM);
        int decay   = Mathf.RoundToInt((D / 1000f) * FM);
        int sustain = Mathf.RoundToInt((S / 1000f) * FM);

        int attackEnd  = attack;
        int decayEnd   = attack + decay;
        int sustainEnd = attack + decay + sustain;

        if (t < attackEnd)       value = attack > 0 ? (float)t / attack : 1f;
        else if (t < decayEnd)   value = Mathf.Lerp(1f, SL, (float)(t - attackEnd) / decay);
        else if (t < sustainEnd) value = SL;
        else                     value = 0f;

        adsrCache[t] = value;
        return value;
    }

    // ==========================================
    // 🎹 PRESETS DE INSTRUMENTOS
    // ==========================================
    public void SetInstrument(int instrument)
    {
        waveType     = WaveType.SA;
        useNormalize = false;
        fmEnabled    = false;

        switch (instrument)
        {
            case 1: // Flauta
                Armonicos    = 6;
                AmplitudesSA = new float[10] { 1.0f, 0.18f, 0.07f, 0.03f, 0.015f, 0.008f, 0f, 0f, 0f, 0f };
                A = 250; D = 200; S = 1000; SL = 0.9f;
                break;
            case 2: // Acordeón
                Armonicos    = 8;
                AmplitudesSA = new float[10] { 1.0f, 0.75f, 0.6f, 0.45f, 0.35f, 0.25f, 0.18f, 0.12f, 0f, 0f };
                A = 80; D = 150; S = 1200; SL = 0.85f;
                break;
            case 3: // Violín
                Armonicos    = 10;
                AmplitudesSA = new float[10] { 1.0f, 0.85f, 0.75f, 0.65f, 0.55f, 0.45f, 0.35f, 0.28f, 0.2f, 0.15f };
                A = 350; D = 250; S = 1500; SL = 0.8f;
                break;
            default:
                Debug.LogWarning($"Instrumento {instrument} no reconocido.");
                break;
        }
        UpdateADSR();
    }

    // ==========================================
    // 🎮 MÉTODOS PARA EL JUEGO
    // ==========================================
    public void SetSFX(float freq, WaveType wave, int attack, int decay, int sustain, float sustainLevel,
                       bool fm = false, float fmRatio = 2f, float fmIndex = 1f)
    {
        f            = freq;
        waveType     = wave;
        fmEnabled    = fm;
        this.fmRatio = fmRatio;
        this.fmIndex = fmIndex;
        A = attack; D = decay; S = sustain; SL = sustainLevel;
        UpdateADSR();
        TimeIndex = 0;
    }

    public void PlayOneShot()
    {
        TimeIndex = 0;
        Aud.Play();
    }

    public void StopSound() => Aud.Stop();

    public void SetManualMode() => useNormalize = true;
}
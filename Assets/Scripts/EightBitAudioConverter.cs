using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (AudioListener))]
public class EightBitAudioConverter : MonoBehaviour {
    private enum WaveType {
        Sine = 0,
        Square = 1,
        Triangle = 2,
        Sawtooth = 3,
    }

    [SerializeField] private string BGMName = "";
    [SerializeField] private bool IsPlaying = false;
    [SerializeField] private WaveType waveType = WaveType.Sine;
    [SerializeField, Range (0.0002f, 1)] private float Threshold = 0.005f;
    [SerializeField, Range (0, 1)] private float Ratio = 0.5f;
    [SerializeField, Range (0, 1)] private double EightBitVolume = 0.2d;

    private readonly int NUM_SAMPLES = 2048;

    private float[] _bgmSpectrum = null;
    List<double> _waveTime = new List<double> ();
    private int _sampleRate = 4800; //AudioSettings.outputSampleRate

    void Awake () {
        _sampleRate = AudioSettings.outputSampleRate;

        for (int i = 0; i < NUM_SAMPLES; i++) {
            _waveTime.Add (0);
        }
    }

    void Start () {
        AudioManager.Instance.PlayBGM (BGMName, true, 1);
    }

    void Update () {
        _bgmSpectrum = AudioManager.Instance.GetBGMSpectrumData (BGMName, NUM_SAMPLES);

        //スペクトル表示
        for (int i = 1; i < _bgmSpectrum.Length - 1; i++) {
            Debug.DrawLine (new Vector3 (Mathf.Log (i - 1) * 3, Mathf.Log (_bgmSpectrum[i - 1]), 3), new Vector3 (Mathf.Log (i) * 3, Mathf.Log (_bgmSpectrum[i]), 3), Color.blue);
        }
    }

    void OnAudioFilterRead (float[] data, int channels) {
        if (!IsPlaying) return;

        for (int i = 0; i < NUM_SAMPLES; i++) {
            data[i] *= (1 - Ratio);
        }

        float[] wave = new float[NUM_SAMPLES];
        for (int i = 0; i < NUM_SAMPLES; i++) {
            if (_bgmSpectrum[i] < Threshold) continue;

            wave = GetWave (
                waveType,
                i,
                channels,
                ((float) _sampleRate / (float) NUM_SAMPLES) * i
            );

            for (int j = 0; j < data.Length; j++) {
                data[j] += wave[j] * _bgmSpectrum[i] * Ratio;
            }

        }
    }

    /// <summary>
    /// 波を取得
    /// </summary>
    private float[] GetWave (WaveType type, int index, int channels, float freqency) {
        float[] data = new float[NUM_SAMPLES];

        double increment = 2.0 * Math.PI * freqency / _sampleRate;
        for (int i = 0; i < NUM_SAMPLES; i += channels) {
            _waveTime[index] += increment;
            data[i] += CalcWave (type, index, increment);

            if (channels == 2) data[i + 1] = data[i];
        }

        return data;
    }

    private float CalcWave (WaveType waveType, int index, double increment) {
        float ret = 0;
        switch (waveType) {
            case WaveType.Sine:
                _waveTime[index] += increment;
                ret = (float) (EightBitVolume * Math.Sin (_waveTime[index]));
                if (_waveTime[index] > 2 * Math.PI) _waveTime[index] = 0;
                return ret;

            case WaveType.Square:
                _waveTime[index] += increment;
                ret = (float) (EightBitVolume * ((_waveTime[index] % Math.PI * 2) < Math.PI * 2 * 0.5 ? 1.0 : -1.0));
                if (_waveTime[index] > 2 * Math.PI) _waveTime[index] = 0;
                return ret;

            case WaveType.Triangle:
                _waveTime[index] += increment;
                if (_waveTime[index] > 2 * Math.PI) _waveTime[index] = 0;
                double t = (_waveTime[index] + Math.PI * 2) % Math.PI;
                ret = (float) (EightBitVolume * ((t < Math.PI ? t - Math.PI : Math.PI - t) / Math.PI * 2 + 1.0));
                return ret;

            case WaveType.Sawtooth:
                _waveTime[index] += increment;
                ret = (float) (EightBitVolume * ((_waveTime[index] + Math.PI) % Math.PI * 2) / Math.PI - 1.0);
                if (_waveTime[index] > 2 * Math.PI) _waveTime[index] = 0;
                return ret;

            default:
                return ret;
        }
    }
}
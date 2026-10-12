using System;
using UnityEngine;

[AddComponentMenu("Audio/SoundVis/SoundVis")]
public class SoundVis : MonoBehaviour
{
    [Tooltip("Number of samples for FFT. Will be clamped to a power of two (64..8192).")]
    [SerializeField] private int sampleSize = 1024;

    [Tooltip("FFT window type used by GetSpectrumData.")]
    [SerializeField] private FFTWindow fftWindow = FFTWindow.BlackmanHarris;

    [Tooltip("How often (in seconds) to update the spectrum. Lower = more frequent updates.")]
    [SerializeField, Min(0.01f)] private float updateInterval = 0.05f;

    [Tooltip("If true, reads left and right channel separately and averages them into output.")]
    [SerializeField] private bool averageStereo = true;

    // Public read-only access to the latest spectrum (length == sampleSize)
    public float[] Spectrum => spectrumOutput;

    // Optional callback invoked after each spectrum update
    public Action OnSpectrumUpdated;

    private float[] spectrumLeft;
    private float[] spectrumRight;
    private float[] spectrumOutput;

    private float timeSinceLastUpdate = 0f;

    private void Awake()
    {
        // Ensure sampleSize is a power of two within reasonable bounds
        sampleSize = Mathf.Clamp(sampleSize, 64, 8192);
        sampleSize = Mathf.ClosestPowerOfTwo(sampleSize);

        spectrumLeft = new float[sampleSize];
        spectrumRight = new float[sampleSize];
        spectrumOutput = new float[sampleSize];
    }

    private void Start()
    {
        // Do an initial sample so Spectrum is populated immediately
        SampleSpectrum();
    }

    private void Update()
    {
        timeSinceLastUpdate += Time.unscaledDeltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            timeSinceLastUpdate = 0f;
            SampleSpectrum();
        }
    }

    /// <summary>
    /// Force immediate sampling of audio spectrum on the next frame.
    /// </summary>
    public void ForceSample() => SampleSpectrum();

    private void SampleSpectrum()
    {
        // Read left channel (channel 0)
        AudioListener.GetSpectrumData(spectrumLeft, 0, fftWindow);

        if (averageStereo)
        {
            // Try to read right channel (channel 1). If the audio system is mono the second call will still return valid data.
            AudioListener.GetSpectrumData(spectrumRight, 1, fftWindow);

            for (int i = 0; i < sampleSize; i++)
            {
                // simple average of two channels
                spectrumOutput[i] = (spectrumLeft[i] + spectrumRight[i]) * 0.5f;
            }
        }
        else
        {
            // Use left channel as output
            Array.Copy(spectrumLeft, spectrumOutput, sampleSize);
        }

        OnSpectrumUpdated?.Invoke();
    }

    /// <summary>
    /// Returns a safe band value at index (0..sampleSize-1). Returns 0 if out of range.
    /// </summary>
    public float GetBand(int index)
    {
        if (spectrumOutput == null || index < 0 || index >= spectrumOutput.Length)
            return 0f;
        return spectrumOutput[index];
    }

    /// <summary>
    /// Convenience: computes average energy across a frequency band [fromIndex..toIndex] (inclusive).
    /// </summary>
    public float GetBandAverage(int fromIndex, int toIndex)
    {
        if (spectrumOutput == null) return 0f;
        fromIndex = Mathf.Clamp(fromIndex, 0, sampleSize - 1);
        toIndex = Mathf.Clamp(toIndex, 0, sampleSize - 1);
        if (toIndex < fromIndex) (fromIndex, toIndex) = (toIndex, fromIndex);

        float sum = 0f;
        for (int i = fromIndex; i <= toIndex; i++)
            sum += spectrumOutput[i];
        return sum / (toIndex - fromIndex + 1);
    }
}

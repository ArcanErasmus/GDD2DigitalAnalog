﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneInput : MonoBehaviour
{
    public float minThreshold = 0;
    public float frequency = 0.0f;
    public int audioSampleRate = 44100;
    public string microphone;
    public FFTWindow fftWindow;
    public Dropdown micDropdown;
    public Slider thresholdSlider;

    private List<string> options = new List<string>();
    private int samples = 8192;
    private AudioSource audioSource;

    void Start()
    {
        // Get components you'll need
        audioSource = GetComponent<AudioSource>();

        // Get all available microphones
        foreach (string device in Microphone.devices)
        {
            if (microphone == null)
            {
                //set default mic to first mic found.
                microphone = device;
            }
            options.Add(device);
        }
        microphone = options[PlayerPrefsManager.GetMicrophone()];
        minThreshold = PlayerPrefsManager.GetThreshold();

        // Add mics to dropdown
        micDropdown.AddOptions(options);
        micDropdown.onValueChanged.AddListener(delegate
        {
            MicDropdownValueChangedHandler(micDropdown);
        });

        thresholdSlider.onValueChanged.AddListener(delegate
        {
            ThresholdValueChangedHandler(thresholdSlider);
        });

        // Initialize input with default mic
        UpdateMicrophone();
    }
    
    void UpdateMicrophone()
    {
        // Stop if currently recording
        audioSource.Stop();

        // Start recording mic to audioClip
        audioSource.clip = Microphone.Start(microphone, true, 10, audioSampleRate);
        audioSource.loop = true;

        Debug.Log(Microphone.IsRecording(microphone).ToString()); // Is mic recording?

        // Check that the mic is recording, otherwise you'll get stuck in an infinite loop waiting for it to start
        if (Microphone.IsRecording(microphone))
        {
            // Wait until the recording has started
            while (!(Microphone.GetPosition(microphone) > 0)) ;

            Debug.Log("recording started with " + microphone);

            // Play audio source
            audioSource.Play();
        }
        else
        {
            // Mic doesn't work
            Debug.Log(microphone + " doesn't work!");
        }
    }

    public void MicDropdownValueChangedHandler(Dropdown mic)
    {
        microphone = options[mic.value];
        UpdateMicrophone();
    }

    public void ThresholdValueChangedHandler(Slider thresholdSlider)
    {
        minThreshold = thresholdSlider.value;
        Debug.Log("Threshold: " + thresholdSlider.value);
    }

    public float GetAveragedVolume()
    {
        float[] data = new float[256];
        float a = 0;
        audioSource.GetOutputData(data, 0);
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        return a / 256;
    }

    public float GetFundamentalFrequency()
    {
        float fundamentalFrequency = 0.0f;
        float[] data = new float[samples];
        audioSource.GetSpectrumData(data, 0, fftWindow);
        float s = 0.0f;
        int i = 0;
        for (int j = 1; j < samples; j++)
        {
            if (data[j] > minThreshold) // volume must meet minimum threshold
            {
                if (s < data[j])
                {
                    s = data[j];
                    i = j;
                }
            }
        }
        fundamentalFrequency = i * audioSampleRate / samples;
        frequency = fundamentalFrequency;
        return fundamentalFrequency;
    }
}
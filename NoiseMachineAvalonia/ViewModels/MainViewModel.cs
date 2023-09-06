using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using ManagedBass;
using ManagedBass.Fx;
using System.Security.Cryptography;
using System;
using System.Runtime.InteropServices;
using System.Timers;
using Avalonia.Threading;
using System.Diagnostics;
using System.IO;
using Meadow.Units;
using System.Threading;
using ManagedBass.Mix;
using ManagedBass.DirectX8;
using System.Threading.Channels;
using NAudio.Dsp;

namespace NoiseMachineAvalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Init();
    }

    [ObservableProperty]
    private double filterGain;
    [ObservableProperty]
    private double filterBandwidth;
    [ObservableProperty]
    private double filterCutoff;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoisePercentage))]
    private double _noiseVolume;
    public string NoisePercentage => "Noise Volume: " + (100 * NoiseVolume).ToString() + "%";
    [ObservableProperty]
    private bool _whiteNoiseChecked;
    [ObservableProperty]
    private bool _pinkNoiseChecked;
    [ObservableProperty]
    private bool _perlinNoiseChecked;
    [ObservableProperty]
    private bool _brownianNoiseChecked;
    [ObservableProperty]
    private bool lowFilterChecked;
    [ObservableProperty]
    private bool bandFilterChecked;
    [ObservableProperty]
    private bool highFilterChecked;
    [ObservableProperty]
    private bool notchFilterChecked;
    [ObservableProperty]
    private bool allPassFilterChecked;
    [ObservableProperty]
    private bool peakingFilterChecked;
    [ObservableProperty]
    private bool lowShelfFilterChecked;
    [ObservableProperty]
    private bool highShelfFilterChecked;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TonePercentage))]
    private double toneVolume;
    public string TonePercentage => "Tone Volume: " + (100 * ToneVolume).ToString() + "%";
    [ObservableProperty]
    private bool binauralChecked;
    [ObservableProperty]
    private bool isochronicChecked;
    [ObservableProperty]
    private bool solfeggioChecked;
    [ObservableProperty]
    private double toneFreq;
    [ObservableProperty]
    private string? buttonText;

    static StreamProcedure? toneStreamCreate;
    static StreamProcedure? noiseStreamCreate;
    static long bufferLength;
    static long noiseBufferLength;

    const double twoPI = 2 * Math.PI;
    const int sampleRate = 44100;

    static int tonechannel;
    static int noisechannel;

    static short[]? tonedata; //buffer
    static short[]? noisedata;

    const double frequency = 104;
    static double wavePosition = 0;
    static double wavePosition2 = 0;
    static double noisePosition = 0;
    private static DispatcherTimer? aTimer;

    [ObservableProperty]
    double isoVolume;

    private void OnTimedEvent(object? source, EventArgs e)
    {
        if (aTimer == null)
            throw new ArgumentNullException(nameof(aTimer), "The timer is null");
        if (ToneFreq > 0)
            aTimer.Interval = TimeSpan.FromSeconds(1 / (ToneFreq * 2));
        else
            aTimer.Interval = TimeSpan.FromSeconds(0.5);
        if (IsochronicChecked)
            if (IsoVolume == 1)
            {
                IsoVolume = 0;
                Bass.ChannelStop(tonechannel);
            }
                
            else
            {
                IsoVolume = 1;
                Bass.ChannelPlay(tonechannel, false);
            }
                
        else
        {
            if (IsoVolume == 0)
            {
                IsoVolume = 1;
                Bass.ChannelPlay(tonechannel, false);
            }
        }           
    }
    public void StartStopSounds()
    {
        if (ButtonText == "Start")
        {
            ButtonText = "Stop";
            RandomNumberGenerator.Create();
            if (!Bass.Init(-1, sampleRate, DeviceInitFlags.Default, IntPtr.Zero))
                throw new Exception("BASS failed to start");
            
            toneStreamCreate = new (ToneProc);
            tonechannel = Bass.CreateStream(sampleRate, 2, BassFlags.Default, toneStreamCreate, IntPtr.Zero);

            bufferLength = Bass.ChannelSeconds2Bytes(tonechannel, Bass.GetConfig(Configuration.PlaybackBufferLength) / 1000d);
            tonedata = new short[bufferLength];

            noiseStreamCreate = new(NoiseProc);
            noisechannel = Bass.CreateStream(sampleRate, 2, BassFlags.Default, noiseStreamCreate, IntPtr.Zero);

            noiseBufferLength = Bass.ChannelSeconds2Bytes(tonechannel, Bass.GetConfig(Configuration.PlaybackBufferLength) / 1000d);
            noisedata = new short[noiseBufferLength];
            source = new(alpha, poles);
            var fx = Bass.ChannelSetFX(noisechannel, EffectType.BQF, 1);
            var eq = new BQFParameters
            {
                lFilter = BQFType.LowPass,
                fCenter = 200, // center frequency of the filter
                fBandwidth = 18, // bandwidth of the filter
                fGain = 100 // gain of the filter
            };
            Bass.FXSetParameters(fx, eq);
            Bass.ChannelPlay(tonechannel, false);
            Bass.ChannelPlay(noisechannel, false);
            // Create a timer with a two second interval.
            aTimer = new();
            // Hook up the Elapsed event for the timer. 
            aTimer.Tick += new EventHandler(OnTimedEvent);
            aTimer.Start();
        }
        else
        {
            ButtonText = "Start";
            aTimer?.Stop();
            Bass.ChannelStop(tonechannel);
            Bass.StreamFree(tonechannel);
            Bass.ChannelStop(noisechannel);
            Bass.StreamFree(noisechannel);
            Bass.Free();
        }

    }

    double randomStart = Random.Shared.NextDouble() * 32768;
    int brownpos;
    PinkNoise source;

    const double alpha = 1.0;
    const int poles = 5;
    private int NoiseProc(int handle, IntPtr buffer, int waveCalculated, IntPtr user)
    {
        double step = 1d / 2048;
        int octave = 11;
        int length = waveCalculated / 2;
        if (noisedata == null)
            throw new ArgumentNullException(nameof(noisedata), "The buffer array is null");
        
        for (int a = 0; a < length; a += 2)
        {
            /*if (LowFilterChecked)
                eq.lFilter = BQFType.LowPass;
            else if (BandFilterChecked)
                eq.lFilter = BQFType.BandPass;
            else if (HighFilterChecked)
                eq.lFilter = BQFType.HighPass;
            else if (NotchFilterChecked)
                eq.lFilter = BQFType.Notch;
            else if (AllPassFilterChecked)
                eq.lFilter = BQFType.AllPass;
            else if (PeakingFilterChecked)
                eq.lFilter = BQFType.PeakingEQ;
            else if (LowShelfFilterChecked)
                eq.lFilter = BQFType.Notch;
            else if (HighShelfFilterChecked)
                eq.lFilter = BQFType.Notch;*/

            if (WhiteNoiseChecked)
            {
                noisedata[a] = (short)(NoiseVolume * 0.5 * Random.Shared.Next(-32767, 32767));
            }
            else if (PinkNoiseChecked)
            {
                noisedata[a] = (short)(NoiseVolume * 8192 * source.NextValue());
            }
            else if (PerlinNoiseChecked)
            {
                noisedata[a] = (short)(NoiseVolume * 32767 * PerlinNoise.ReturnFracBrownNoise(randomStart, octave));
                randomStart += step;
            }
            else if (BrownianNoiseChecked)
            {
                if (brownpos >= 256)
                    brownpos += Random.Shared.Next(-50, 0);
                else if (brownpos <= -256)
                    brownpos += Random.Shared.Next(0, 50);
                else
                    brownpos += Random.Shared.Next(-50, 50);
                noisedata[a] = (short)(NoiseVolume * 64 * brownpos);
            }
            noisedata[a + 1] = noisedata[a]; //Keep it mono, by copying one channel to the other
            noisePosition += frequency / sampleRate; //Reserved for noise types that use the X coordinate of the graph
        }

        Marshal.Copy(noisedata, 0, buffer, length);

        return waveCalculated;
    }

    private int ToneProc(int handle, IntPtr buffer, int length, IntPtr user)
    {
        int waveCalculated = length;
        //waveCalculated |= (int)BASSStreamProc.BASS_STREAMPROC_END;

        length /= 2;
        if (tonedata == null)
            throw new ArgumentNullException(nameof(tonedata), "The buffer array is null");

        for (int a = 0; a < length; a += 2)
        {

            //Stereo
            tonedata[a] = (short)(ToneVolume * 32767 * Math.Sin(wavePosition));
            tonedata[a + 1] = (short)(ToneVolume * 32767 * Math.Sin(wavePosition2));

            if (BinauralChecked)
            {
                wavePosition += twoPI * frequency / sampleRate;

                if (wavePosition > twoPI)
                {
                    wavePosition -= twoPI;
                }

                wavePosition2 += twoPI * (frequency + ToneFreq) / sampleRate;

                if (wavePosition2 > twoPI)
                {
                    wavePosition2 -= twoPI;
                }
            }
            else if (IsochronicChecked)
            {
                wavePosition += twoPI * frequency / sampleRate;

                if (wavePosition > twoPI)
                {
                    wavePosition -= twoPI;
                }

                wavePosition2 = wavePosition;

            }
            else if (SolfeggioChecked)
            {
                wavePosition += twoPI * ToneFreq / sampleRate;

                if (wavePosition > twoPI)
                {
                    wavePosition -= twoPI;
                }

                wavePosition2 = wavePosition;

            }
        }

        Marshal.Copy(tonedata, 0, buffer, length);

        return waveCalculated;
    }

    public void Init()
    {
        NoiseVolume = 0.5;
        WhiteNoiseChecked = true;
        FilterCutoff = 200;
        FilterGain = 100;
        FilterBandwidth = 18;
        LowFilterChecked = true;
        ToneVolume = 1;
        BinauralChecked = true;
        ToneFreq = 2.5;
        ButtonText = "Start";
    }
    public void SetToneFreq(double val)
    {
        ToneFreq = val;
    }

    public void SetBrainWaves(string val)
    {
        RandomNumberGenerator.Create();
        switch (val)
        {
            case "Delta":
                ToneFreq = RandomNumberGenerator.GetInt32(5000, 40000) / 10000d;
                break;
            case "Theta":
                ToneFreq = RandomNumberGenerator.GetInt32(40000, 80000) / 10000d;
                break;
            case "Alpha":
                ToneFreq = RandomNumberGenerator.GetInt32(80000, 140000) / 10000d;
                break;
            case "Beta":
                ToneFreq = RandomNumberGenerator.GetInt32(140000, 300000) / 10000d;
                break;
            case "Gamma":
                ToneFreq = RandomNumberGenerator.GetInt32(300000, 500000) / 10000d;
                break;
        }
    }
}

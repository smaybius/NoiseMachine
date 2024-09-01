using CommunityToolkit.Mvvm.ComponentModel;
using NoiseMachineDotNet.libs.bindings.Miniaudio;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NoiseMachineDotNet.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    private bool filterEnabled;

    public unsafe bool FilterEnabled
    {
        get => filterEnabled;
        set
        {
            _ = SetProperty(ref filterEnabled, value);
            if (!Playing)
            {
                return;
            }

            if (value)
            {
                if (Miniaudio.ma_node_attach_output_bus(filter, 0, Miniaudio.ma_node_graph_get_endpoint(pNodeGraph), 0) != ma_result.MA_SUCCESS)
                {
                    StopSounds();
                    throw new Exception("Why won't the filter work?");
                }

                if (Miniaudio.ma_node_attach_output_bus(&pNoise->node, 0, filter, 0) != ma_result.MA_SUCCESS)
                {
                    StopSounds();
                    throw new Exception("White noise turned into silence? LOLWUT????");
                }
            }
            else
            {
                _ = Miniaudio.ma_node_detach_all_output_buses(pNodeGraph);
                if (Miniaudio.ma_node_attach_output_bus(&pNoise->node, 0, Miniaudio.ma_node_graph_get_endpoint(pNodeGraph), 0) != ma_result.MA_SUCCESS)
                {
                    StopSounds();
                    throw new Exception("White noise turned into silence? LOLWUT????");
                }
            }
        }
    }

    [ObservableProperty]
    private bool playing = false;


    private double filterGain = 30;

    public double FilterGain
    {
        get => filterGain;
        set
        {
            _ = SetProperty(ref filterGain, value);
            FilterChanged();
        }
    }

    private double filterWidth = 100;

    public double FilterWidth
    {
        get => filterWidth;
        set
        {
            _ = SetProperty(ref filterWidth, value);
            FilterChanged();
        }
    }

    private double filterCutoff = 200;

    public double FilterCutoff
    {
        get => filterCutoff;
        set
        {
            _ = SetProperty(ref filterCutoff, value);
            FilterChanged();
        }
    }


    private double a0;
    public double A0
    {
        get => a0;
        set
        {
            _ = SetProperty(ref a0, value);
            LowFilterChanged();
        }
    }

    private double a1;
    public double A1
    {
        get => a1;
        set
        {
            _ = SetProperty(ref a1, value);
            LowFilterChanged();
        }
    }

    private double a2;
    public double A2
    {
        get => a2;
        set
        {
            _ = SetProperty(ref a2, value);
            LowFilterChanged();
        }
    }

    private double b0;
    public double B0
    {
        get => b0;
        set
        {
            _ = SetProperty(ref b0, value);
            LowFilterChanged();
        }
    }

    private double b1;
    public double B1
    {
        get => b1;
        set
        {
            _ = SetProperty(ref b1, value);
            LowFilterChanged();
        }
    }

    private double b2;
    public double B2
    {
        get => b2;
        set
        {
            _ = SetProperty(ref b2, value);
            LowFilterChanged();
        }
    }

    private double noiseVolume;
    public unsafe double NoiseVolume
    {
        get => noiseVolume;
        set
        {
            if (SetProperty(ref noiseVolume, value))
            {
                OnPropertyChanged(nameof(NoisePercentage));

                if (Playing)
                {
                    _ = Miniaudio.ma_device_set_master_volume(pNoiseDevice, (float)value);
                }
            }
        }
    }
    public string NoisePercentage => "Noise Volume: " + (100 * NoiseVolume).ToString() + "%";
    [ObservableProperty]
    private bool whiteNoiseChecked;
    [ObservableProperty]
    private bool pinkNoiseChecked;
    [ObservableProperty]
    private bool brownianNoiseChecked;

    private static double toneVolume;
    public unsafe double ToneVolume
    {
        get => toneVolume;
        set
        {
            if (SetProperty(ref toneVolume, value))
            {
                OnPropertyChanged(nameof(TonePercentage));

                if (Playing)
                {
                    _ = Miniaudio.ma_device_set_master_volume(pToneDevice, (float)value);
                }
            }
        }
    }


    public string TonePercentage => "Tone Volume: " + (100 * ToneVolume).ToString() + "%";
    [ObservableProperty]
    private static bool binauralChecked;
    [ObservableProperty]
    private static bool isochronicChecked;
    [ObservableProperty]
    private static bool solfeggioChecked;
    [ObservableProperty]
    private static double toneFreq;
    [ObservableProperty]
    private string? buttonText;
    private const int sampleRate = 44100;
    private const double frequency = 104;

    [ObservableProperty]
    private static string? filterName;



    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void ToneCallback(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
    {
        float* pOut = (float*)pOutput;
        for (uint i = 0; i < frameCount; i++)
        {
            pOut[2 * i] = (float)(Math.Sin(2 * Math.PI * phase) * toneVolume * isoVolume);      // Left channel
            pOut[(2 * i) + 1] = (float)(Math.Sin(2 * Math.PI * phase2) * toneVolume * isoVolume); // Right channel

            if (binauralChecked)
            {
                isoVolume = 1;
                phase += frequency / sampleRate;
                if (phase >= 1.0)
                {
                    phase -= 1.0;
                }

                phase2 += (frequency + toneFreq) / sampleRate;
                if (phase2 >= 1.0)
                {
                    phase2 -= 1.0;
                }
            }
            else if (isochronicChecked)
            {
                phase += frequency / sampleRate;
                if (phase >= 1.0)
                {
                    phase -= 1.0;
                }

                phase2 += frequency / sampleRate;
                if (phase2 >= 1.0)
                {
                    phase2 -= 1.0;
                }

                isophase += Math.PI * toneFreq / sampleRate;
                isoVolume = Math.Clamp(Math.Sin(isophase), 0, 0.25) * 4;
            }
            else if (solfeggioChecked)
            {
                isoVolume = 1;
                phase += toneFreq / sampleRate;
                phase2 += toneFreq / sampleRate;
            }
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void NoiseCallback(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
    {
        _ = Miniaudio.ma_node_graph_read_pcm_frames(pNodeGraph, pOutput, frameCount, null);
    }

    private static unsafe ma_biquad_node* filter;

    private static double isoVolume = 1;
    private static double isophase;
    private static double phase = 0;
    private static double phase2 = 0;
    private static unsafe ma_device* pToneDevice;
    private static unsafe ma_device* pNoiseDevice;
    private static unsafe ma_noise_node* pNoise;
    private static unsafe ma_node_graph* pNodeGraph;

    public struct ma_noise_node
    {
        public ma_data_source_node node;
        public ma_noise noise;
    }
    public MainViewModel()
    {
        Init();
    }
    unsafe ~MainViewModel()
    {
        StopSounds();
        Miniaudio.ma_device_uninit(pToneDevice);
        _ = Miniaudio.ma_node_detach_all_output_buses(pNodeGraph);
        Miniaudio.ma_device_uninit(pNoiseDevice);
        Miniaudio.ma_node_uninit(pNodeGraph, null);
        Miniaudio.ma_noise_uninit(&pNoise->noise, null);
        Miniaudio.ma_biquad_node_uninit(filter, null);
        NativeMemory.Free(pNodeGraph);
        NativeMemory.Free(pNoiseDevice);
        NativeMemory.Free(pNoise);
        NativeMemory.Free(filter);
        NativeMemory.Free(pToneDevice);
    }

    public unsafe void StartSounds()
    {

        if (Miniaudio.ma_device_start(pToneDevice) != ma_result.MA_SUCCESS)
        {
            throw new Exception("The future is now thanks to science! Clemontic gear on! O NOES!!! PLAYBACC DEVICE ABOUT 2 AXPLODE!!!!");
        }

        if (Miniaudio.ma_device_start(pNoiseDevice) != ma_result.MA_SUCCESS)
        {
            StopSounds();
            throw new Exception("Ain't science so amazing, that Clemont's playback device invention ain't startin'?\n");
        }

        _ = Miniaudio.ma_device_set_master_volume(pNoiseDevice, (float)NoiseVolume);

        Playing = true;
    }
    public unsafe void StopSounds()
    {
        Playing = false;
        FilterEnabled = false;
        _ = Miniaudio.ma_device_stop(pToneDevice);
        _ = Miniaudio.ma_device_stop(pNoiseDevice);
    }
    public void StartStopSounds()
    {

        if (ButtonText == "Start")
        {
            StartSounds();
            ButtonText = "Stop";
        }
        else
        {
            StopSounds();
            ButtonText = "Start";
        }

    }


    public unsafe void Init()
    {
        Playing = false;
        NoiseVolume = 0.5f;
        WhiteNoiseChecked = true;
        FilterCutoff = 500;
        FilterGain = 30;
        FilterWidth = 100;
        ToneVolume = 1;
        BinauralChecked = true;
        ToneFreq = 2.5;
        ButtonText = "Start";
        FilterName = "lpf";
        FilterEnabled = false;
        FilterChanged();

        #region Tone
        pToneDevice = (ma_device*)NativeMemory.Alloc((nuint)sizeof(ma_device));

        ma_device_config toneDeviceConfig = Miniaudio.ma_device_config_init(ma_device_type.ma_device_type_playback);
        toneDeviceConfig.playback.format = ma_format.ma_format_f32;
        toneDeviceConfig.playback.channels = 2;
        toneDeviceConfig.sampleRate = sampleRate;
        toneDeviceConfig.dataCallback = &ToneCallback;

        if (Miniaudio.ma_device_init(null, &toneDeviceConfig, pToneDevice) != ma_result.MA_SUCCESS)
        {
            throw new Exception("Your playback device gives Clark Griswold's Christmas lights a run for its money!");
        }

        #endregion

        #region noise

        pNodeGraph = (ma_node_graph*)NativeMemory.Alloc((nuint)sizeof(ma_node_graph));
        ma_node_graph_config nodeConfig = Miniaudio.ma_node_graph_config_init(2);
        if (Miniaudio.ma_node_graph_init(&nodeConfig, null, pNodeGraph) != ma_result.MA_SUCCESS)
        {
            StopSounds();
            throw new Exception("Graphs, graphs, needs more graphs! Ain't no graphs in here! This is completely worthless!");
        }


        pNoiseDevice = (ma_device*)NativeMemory.Alloc((nuint)sizeof(ma_device));
        pNoise = (ma_noise_node*)NativeMemory.Alloc((nuint)sizeof(ma_noise_node));

        filter = (ma_biquad_node*)NativeMemory.Alloc((nuint)sizeof(ma_biquad_node));

        ma_device_config NoiseDeviceConfig = Miniaudio.ma_device_config_init(ma_device_type.ma_device_type_playback);
        NoiseDeviceConfig.playback.format = ma_format.ma_format_f32;
        NoiseDeviceConfig.playback.channels = 2;
        NoiseDeviceConfig.sampleRate = sampleRate;
        NoiseDeviceConfig.dataCallback = &NoiseCallback;

        ma_noise_config noiseconfig = Miniaudio.ma_noise_config_init(ma_format.ma_format_f32, 2, ma_noise_type.ma_noise_type_white, 4321, 1);
        if (Miniaudio.ma_noise_init(&noiseconfig, null, &pNoise->noise) != ma_result.MA_SUCCESS)
        {
            StopSounds();
            throw new Exception("Noise turned into silence? LOLWUT????");
        }

        ma_data_source_node_config dataconfig = Miniaudio.ma_data_source_node_config_init(&pNoise->noise);

        if (Miniaudio.ma_data_source_node_init(pNodeGraph, &dataconfig, null, &pNoise->node) != ma_result.MA_SUCCESS)
        {
            StopSounds();
            throw new Exception("Y DERE AINT NO SAUCE????");
        }

        ma_biquad_node_config filterconfig = Miniaudio.ma_biquad_node_config_init(2, (float)B0, (float)B1, (float)B2, (float)A0, (float)A1, (float)A2);
        if (Miniaudio.ma_biquad_node_init(pNodeGraph, &filterconfig, null, filter) != ma_result.MA_SUCCESS)
        {
            StopSounds();
            throw new Exception("Filter failed to start. Wonder what happened?");
        }

        if (Miniaudio.ma_node_attach_output_bus(&pNoise->node, 0, Miniaudio.ma_node_graph_get_endpoint(pNodeGraph), 0) != ma_result.MA_SUCCESS)
        {
            StopSounds();
            throw new Exception("White noise turned into silence? LOLWUT????");
        }

        if (Miniaudio.ma_device_init(null, &NoiseDeviceConfig, pNoiseDevice) != ma_result.MA_SUCCESS)
        {
            StopSounds();
            throw new Exception("Did your playback device come from Bigmotor or smth? Kinda sus ngl\n");
        }
        #endregion
    }
    public void SetToneFreq(double val)
    {
        ToneFreq = val;
    }

    public void SetBrainWaves(string val)
    {
        switch (val)
        {
            case "Delta":
                ToneFreq = NextDouble(0.5, 4); //0.5 to 4
                break;
            case "Theta":
                ToneFreq = NextDouble(4, 8); //4 to 8
                break;
            case "Alpha":
                ToneFreq = NextDouble(8, 14); //8 to 14
                break;
            case "Beta":
                ToneFreq = NextDouble(14, 30); //14 to 30
                break;
            case "Gamma":
                ToneFreq = NextDouble(30, 50); //30 to 50
                break;
        }
    }

    public static double NextDouble(double min, double max)
    {
        return (Random.Shared.NextDouble() * (max - min)) + min;
    }

    public void SetFilter(string filt)
    {
        FilterName = filt;
        FilterChanged();
    }

    private static bool highLevel = false;

    public unsafe void FilterChanged()
    {
        double w, s, c, A, S, a, sqrtA;
        highLevel = true;
        w = 2 * Math.PI * FilterCutoff / sampleRate;
        s = Math.Sin(w);
        c = Math.Cos(w);
        A = Math.Pow(10, FilterGain / 40);
        S = FilterWidth / 100;
        a = s / 2 * Math.Sqrt(((A + (1 / A)) * ((1 / S) - 1)) + 2);
        sqrtA = 2 * Math.Sqrt(A) * a;
        switch (FilterName)
        {
            case "bpf":
                double q = filterWidth;
                a = s / (2 * q);

                B0 = 1 + (a * A);
                B1 = -2 * c;
                B2 = 1 - (a * A);
                A0 = 1 + (a / A);
                A1 = -2 * c;
                A2 = 1 - (a / A);
                break;
            case "lpf":
                B0 = A * (A + 1 - ((A - 1) * c) + sqrtA);
                B1 = 2 * A * (A - 1 - ((A + 1) * c));
                B2 = A * (A + 1 - ((A - 1) * c) - sqrtA);
                A0 = A + 1 + ((A - 1) * c) + sqrtA;
                A1 = -2 * (A - 1 + ((A + 1) * c));
                A2 = A + 1 + ((A - 1) * c) - sqrtA;
                break;
            case "hpf":
                B0 = A * (A + 1 + ((A - 1) * c) + sqrtA);
                B1 = -2 * A * (A - 1 + ((A + 1) * c));
                B2 = A * (A + 1 + ((A - 1) * c) - sqrtA);
                A0 = A + 1 - ((A - 1) * c) + sqrtA;
                A1 = 2 * (A - 1 - ((A + 1) * c));
                A2 = A + 1 - ((A - 1) * c) - sqrtA;
                break;
        }
        highLevel = false;
        if (!Playing)
        {
            return;
        }

        ma_biquad_config config = Miniaudio.ma_biquad_config_init(ma_format.ma_format_f32, 2, B0, B1, B2, A0, A1, A2);
        _ = Miniaudio.ma_biquad_node_reinit(&config, filter);
    }

    public unsafe void LowFilterChanged()
    {
        if (!Playing)
        {
            return;
        }

        if (highLevel)
        {
            return;
        }

        ma_biquad_config config = Miniaudio.ma_biquad_config_init(ma_format.ma_format_f32, 2, B0, B1, B2, A0, A1, A2);
        _ = Miniaudio.ma_biquad_node_reinit(&config, filter);
    }
}
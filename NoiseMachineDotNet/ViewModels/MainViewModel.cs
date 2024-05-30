using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using NoiseMachineDotNet.libs.bindings.Miniaudio;
using Silk.NET.Vulkan;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using YamlDotNet.Core.Tokens;

namespace NoiseMachineDotNet.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    
    public MainViewModel()
    {
        Init();
    }


    private static bool filterEnabled;

    public unsafe bool FilterEnabled
    {
        get => filterEnabled;
        set
        {
            SetProperty(ref filterEnabled, value);
            if (!Playing) return;
            if (value)
            {
                Miniaudio.ma_node_set_output_bus_volume(filter, 0, (float)FilterGain / 100);
            }
            else
            {
                Miniaudio.ma_node_set_output_bus_volume(filter, 0, 0);
            }
        }
    }

    [ObservableProperty]
    private static bool playing = false;
    
    private static double filterGain = 100;

    public unsafe double FilterGain
    {
        get => filterGain;
        set
        {
            SetProperty(ref filterGain, value);
            if (!FilterEnabled) return;
            Miniaudio.ma_node_set_output_bus_volume(filter, 0, (float)value / 100);
        }
    }


    private static double filterOrder = 1;

    public double FilterOrder
    {
        get => filterOrder;
        set
        {
            SetProperty(ref filterOrder, value);
            FilterChanged();
        }
    }

    private static double filterCutoff = 200;

    public double FilterCutoff
    {
        get => filterCutoff;
        set
        {
            SetProperty(ref filterCutoff, value);
            FilterChanged();
        }
    }


    private static double a0;
    public double A0
    {
        get => a0;
        set
        {
            SetProperty(ref a0, value);
            LowFilterChanged();
        }
    }

    private static double a1;
    public double A1
    {
        get => a1;
        set
        {
            SetProperty(ref a1, value);
            LowFilterChanged();
        }
    }

    private static double a2;
    public double A2
    {
        get => a2;
        set
        {
            SetProperty(ref a2, value);
            LowFilterChanged();
        }
    }

    private static double b0;
    public double B0
    {
        get => b0;
        set
        {
            SetProperty(ref b0, value);
            LowFilterChanged();
        }
    }

    private static double b1;
    public double B1
    {
        get => b1;
        set
        {
            SetProperty(ref b1, value);
            LowFilterChanged();
        }
    }

    private static double b2;
    public double B2
    {
        get => b2;
        set
        {
            SetProperty(ref b2, value);
            LowFilterChanged();
        }
    }

    private static double noiseVolume;
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
    private static bool whiteNoiseChecked;
    [ObservableProperty]
    private static bool pinkNoiseChecked;
    [ObservableProperty]
    private static bool brownianNoiseChecked;

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
    private static string? buttonText;
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
        Miniaudio.ma_node_graph_read_pcm_frames(pNodeGraph, pOutput, frameCount, null);
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

    ~MainViewModel()
    {
        StopSounds();
    }

    public unsafe void StartSounds()
    {
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

        if (Miniaudio.ma_device_start(pToneDevice) != ma_result.MA_SUCCESS)
        {
            throw new Exception("The future is now thanks to science! Clemontic gear on! O NOES!!! PLAYBACC DEVICE ABOUT 2 AXPLODE!!!!");
        }

        #endregion
        #region Noise

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
        if (Miniaudio.ma_device_init(null, &NoiseDeviceConfig, pNoiseDevice) != ma_result.MA_SUCCESS)
        {
            StopSounds();
            throw new Exception("Did your playback device come from Bigmotor or smth? Kinda sus ngl\n");
        }

        if (Miniaudio.ma_device_start(pNoiseDevice) != ma_result.MA_SUCCESS)
        {
            StopSounds();
            throw new Exception("Ain't science so amazing, that Clemont's playback device invention ain't startin'?\n");
        }

        Miniaudio.ma_device_set_master_volume(pNoiseDevice, (float)NoiseVolume);

        #endregion
        Playing = true;
    }
    public unsafe void StopSounds()
    {
        Playing = false;
        Miniaudio.ma_device_uninit(pToneDevice);
        NativeMemory.Free(pToneDevice);

        Miniaudio.ma_node_detach_all_output_buses(pNodeGraph);
        Miniaudio.ma_device_uninit(pNoiseDevice);
        Miniaudio.ma_node_uninit(pNodeGraph, null);
        Miniaudio.ma_noise_uninit(&pNoise->noise, null);
        Miniaudio.ma_biquad_node_uninit(filter, null);
        NativeMemory.Free(pNodeGraph);
        NativeMemory.Free(pNoiseDevice);
        NativeMemory.Free(pNoise);
        NativeMemory.Free(filter);
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


    public void Init()
    {
        Playing = false;
        NoiseVolume = 0.5f;
        WhiteNoiseChecked = true;
        FilterCutoff = 200;
        FilterGain = 100;
        FilterOrder = 1;
        ToneVolume = 1;
        BinauralChecked = true;
        ToneFreq = 2.5;
        ButtonText = "Start";
        FilterName = "lpf";
        FilterEnabled = false;
        FilterChanged();
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
    }

    private static bool highLevel = false;

    public unsafe void FilterChanged()
    {
        double omega = 2 * Math.PI * FilterCutoff / sampleRate;
        double sn = Math.Sin(omega);
        double cs = Math.Cos(omega);
        double alpha = sn / (2 * FilterOrder);
        highLevel = true;
        switch (FilterName)
        {
            case "bpf":
                B0 = alpha;
                B1 = 0;
                B2 = -alpha;
                A0 = 1 + alpha;
                A1 = -2 * cs;
                A2 = 1 - alpha;
                break;
            case "lpf":
                B0 = (1 - cs) / 2;
                B1 = 1 - cs;
                B2 = (1 - cs) / 2;
                A0 = 1 + alpha;
                A1 = -2 * cs;
                A2 = 1 - alpha;
                break;
            case "hpf":
                B0 = (1 + cs) / 2;
                B1 = -(1 + cs);
                B2 = (1 + cs) / 2;
                A0 = 1 + alpha;
                A1 = -2 * cs;
                A2 = 1 - alpha;
                break;
            case "notch":
                B0 = 1;
                B1 = -2 * cs;
                B2 = 1;
                A0 = 1 + alpha;
                A1 = -2 * cs;
                A2 = 1 - alpha;
                break;
            case "peak":
                B0 = 1 + alpha;
                B1 = -2 * cs;
                B2 = 1 - alpha;
                A0 = 1 + alpha;
                A1 = -2 * cs;
                A2 = 1 - alpha;
                break;
        }
        highLevel = false;
        if (!Playing) return;
        ma_biquad_config config = Miniaudio.ma_biquad_config_init(ma_format.ma_format_f32, 2, B0, B1, B2, A0, A1, A2);
        Debug.WriteLine(Miniaudio.ma_biquad_node_reinit(&config, filter));
    }

    public unsafe void LowFilterChanged()
    {
        if (!Playing) return;
        if (highLevel) return;
        ma_biquad_config config = Miniaudio.ma_biquad_config_init(ma_format.ma_format_f32, 2, B0, B1, B2, A0, A1, A2);
        Debug.WriteLine(Miniaudio.ma_biquad_node_reinit(&config, filter));
    }
}
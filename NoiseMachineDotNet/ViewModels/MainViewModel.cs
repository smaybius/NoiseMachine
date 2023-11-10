using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Meadow.Gateways.Bluetooth;
using MiniAudioSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NoiseMachineDotNet.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private string LibraryName = "miniaudio";
    public MainViewModel()
    {
        Init();
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsWatchOS()) return; //conflicts with the non-desktop native libs
        string subfolder = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm32",
            _ => throw new Exception("Dude, wuts ur processor arch????"),
        };
        var libraryPath = "If dis string ain't change, dis app dunno ur pc or cpu. Thankies 4 comin 2 muh ted tawk";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // Are we all microsofty?
        {
            libraryPath = "libs/" + subfolder + $"/{LibraryName}.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) // Are we on Linux's pushover self?
        {
            libraryPath = "libs/" + subfolder + $"/{LibraryName}_bsd.so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) // Are we with the penguin army?
        {
            libraryPath = "libs/" + subfolder + $"/{LibraryName}_linux.so";
        }
        if (RuntimeInformation.ProcessArchitecture == Architecture.Wasm) libraryPath = "libs/wasm" + $"/{LibraryName}.wasm";

        NativeLibrary.SetDllImportResolver(typeof(MainViewModel).Assembly, (name, assembly, path) =>
        {
            if (name == LibraryName)
            {
                return NativeLibrary.Load(libraryPath);
            }

            throw new Exception("Where's miniaudio? TELL ME NOW NOW NOOOOWWW!!!!!!");
        });
        
    }

    [ObservableProperty]
    private static double filterGain;
    [ObservableProperty]
    private static double filterBandwidth;
    [ObservableProperty]
    private static double filterCutoff;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoisePercentage))]
    private static double noiseVolume;
    public string NoisePercentage => "Noise Volume: " + (100 * NoiseVolume).ToString() + "%";
    [ObservableProperty]
    private static bool whiteNoiseChecked;
    [ObservableProperty]
    private static bool pinkNoiseChecked;
    [ObservableProperty]
    private static bool perlinNoiseChecked;
    [ObservableProperty]
    private static bool brownianNoiseChecked;
    [ObservableProperty]
    private static bool lowFilterChecked;
    [ObservableProperty]
    private static bool bandFilterChecked;
    [ObservableProperty]
    private static bool highFilterChecked;
    [ObservableProperty]
    private static bool notchFilterChecked;
    [ObservableProperty]
    private static bool allPassFilterChecked;
    [ObservableProperty]
    private static bool peakingFilterChecked;
    [ObservableProperty]
    private static bool lowShelfFilterChecked;
    [ObservableProperty]
    private static bool highShelfFilterChecked;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TonePercentage))]
    private static double toneVolume;
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
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    static unsafe void ToneCallback(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
    {
        float* pOut = (float*)pOutput;
        for (uint i = 0; i < frameCount; i++)
        {
            pOut[2 * i] = (float)(Math.Sin(2 * Math.PI * phase) * toneVolume * isoVolume);      // Left channel
            pOut[2 * i + 1] = (float)(Math.Sin(2 * Math.PI * phase2) * toneVolume * isoVolume); // Right channel

            if (binauralChecked)
            {
                isoVolume = 1;
                phase += frequency / sampleRate;
                if (phase >= 1.0)
                    phase -= 1.0;
                phase2 += (frequency + toneFreq) / sampleRate;
                if (phase2 >= 1.0)
                    phase2 -= 1.0;
            }
            else if (isochronicChecked)
            {
                phase += frequency / sampleRate;
                if (phase >= 1.0)
                    phase -= 1.0;
                phase2 += frequency / sampleRate;
                if (phase2 >= 1.0)
                    phase2 -= 1.0;
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
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    static unsafe void NoiseCallback(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
    {
        double step = 1d / 2048;
        int octave = 11;
        float* pOut = (float*)pOutput;
        for (uint i = 0; i < frameCount; i++)
        {
            noisePosition += frequency / sampleRate; //Reserved for noise types that use the X coordinate of the graph
            if (whiteNoiseChecked)
            {
                pOut[2 * i] = (float)(noiseVolume * 0.5 * (Random.Shared.Next(-10000,10000) / 10000.0d));
            }
            else if (pinkNoiseChecked)
            {
                pOut[2 * i] = (float)(noiseVolume * 0.25 * source.NextValue());
            }
            else if (perlinNoiseChecked)
            {
                pOut[2 * i] = (float)(noiseVolume * PerlinNoise.ReturnFracBrownNoise(randomStart, octave));
                randomStart += step;
            }
            else if (brownianNoiseChecked)
            {
                if (brownpos >= 1)
                    brownpos += Random.Shared.Next(-50, 0) / 1000d;
                else if (brownpos <= -1)
                    brownpos += Random.Shared.Next(0, 50) / 1000d;
                else
                    brownpos += Random.Shared.Next(-50, 50) / 1000d;
                
                pOut[2 * i] = (float)(noiseVolume * brownpos);
            }
            pOut[2 * i + 1] = pOut[2 * i]; //Keep it mono, by copying one channel to the other
            noisePosition += frequency / sampleRate; //Reserved for noise types that use the X coordinate of the graph
            if (lowFilterChecked)
            {
            }
        }
    }

    private static unsafe ma_lpf* lpf;
    private static unsafe ma_hpf* hpf;
    private static unsafe ma_bpf* bpf;
    private static unsafe ma_notch2* notch2;
    private static unsafe ma_peak2* peak2;
    private static unsafe ma_loshelf2* loshelf2;
    private static unsafe ma_hishelf2* hishelf2;

    [ObservableProperty]
    private static double isoVolume = 1;
    private static double isophase;
    private static double phase = 0;
    private static double phase2 = 0;
    private unsafe static ma_device* pToneDevice;
    private unsafe static ma_context* pToneContext;
    private unsafe static ma_device* pNoiseDevice;
    private unsafe static ma_context* pNoiseContext;
    private static double noisePosition;

    ~MainViewModel()
    {
        StopSounds();
    }
    public static unsafe void StartSounds()
    {
        #region Tone
        ma_device_config toneDeviceConfig;
        pToneDevice = (ma_device*)NativeMemory.Alloc((nuint)sizeof(ma_device));
        pToneContext = (ma_context*)NativeMemory.Alloc((nuint)sizeof(ma_context));
        if (Miniaudio.ma_context_init(null, 0, null, pToneContext) != ma_result.MA_SUCCESS)
        {
            Console.WriteLine("Failed to initialize miniaudio context\n");
            StopSounds();
        }

        toneDeviceConfig = Miniaudio.ma_device_config_init(ma_device_type.ma_device_type_playback);
        toneDeviceConfig.playback.format = ma_format.ma_format_f32;
        toneDeviceConfig.playback.channels = 2;
        toneDeviceConfig.sampleRate = sampleRate;
        toneDeviceConfig.dataCallback = &ToneCallback;

        if (Miniaudio.ma_device_init(pToneContext, &toneDeviceConfig, pToneDevice) != ma_result.MA_SUCCESS)
        {
            Console.WriteLine("Failed to open playback device.\n");
            StopSounds();
            return;
        }

        if (Miniaudio.ma_device_start(pToneDevice) != ma_result.MA_SUCCESS)
        {
            Console.WriteLine("Failed to start playback device.\n");
            StopSounds();
            return;
        }
        #endregion
        #region Noise
        
        ma_device_config NoiseDeviceConfig;
        pNoiseDevice = (ma_device*)NativeMemory.Alloc((nuint)sizeof(ma_device));
        pNoiseContext = (ma_context*)NativeMemory.Alloc((nuint)sizeof(ma_context));
        lpf = (ma_lpf*)NativeMemory.Alloc((nuint)sizeof(ma_lpf));
        hpf = (ma_hpf*)NativeMemory.Alloc((nuint)sizeof(ma_hpf));
        bpf = (ma_bpf*)NativeMemory.Alloc((nuint)sizeof(ma_bpf));
        notch2 = (ma_notch2*)NativeMemory.Alloc((nuint)sizeof(ma_notch2));
        peak2 = (ma_peak2*)NativeMemory.Alloc((nuint)sizeof(ma_peak2));
        loshelf2 = (ma_loshelf2*)NativeMemory.Alloc((nuint)sizeof(ma_loshelf2));
        hishelf2 = (ma_hishelf2*)NativeMemory.Alloc((nuint)sizeof(ma_hishelf2));
        if (Miniaudio.ma_context_init(null, 0, null, pNoiseContext) != ma_result.MA_SUCCESS)
        {
            Console.WriteLine("Failed to initialize miniaudio context\n");
            StopSounds();
        }

        NoiseDeviceConfig = Miniaudio.ma_device_config_init(ma_device_type.ma_device_type_playback);
        NoiseDeviceConfig.playback.format = ma_format.ma_format_f32;
        NoiseDeviceConfig.playback.channels = 2;
        NoiseDeviceConfig.sampleRate = sampleRate;
        NoiseDeviceConfig.dataCallback = &NoiseCallback;

        ma_lpf_config lpfconfig = Miniaudio.ma_lpf_config_init(ma_format.ma_format_f32, 2, sampleRate, filterCutoff, 1);
        if (Miniaudio.ma_lpf_init(&lpfconfig, null, lpf) != ma_result.MA_SUCCESS)
        {
            throw new Exception("Filter failed to start. Wonder what happened?");
        }

        ma_hpf_config hpfconfig = Miniaudio.ma_hpf_config_init(ma_format.ma_format_f32, 2, sampleRate, filterCutoff, 1);
        if (Miniaudio.ma_hpf_init(&hpfconfig, null, hpf) != ma_result.MA_SUCCESS)
        {
            throw new Exception("Filter failed to start. Wonder what happened?");
        }

        if (Miniaudio.ma_device_init(pNoiseContext, &NoiseDeviceConfig, pNoiseDevice) != ma_result.MA_SUCCESS)
        {
            Console.WriteLine("Did your playback device come from Bigmotor? Kinda sus ngl\n");
            StopSounds();
            return;
        }

        if (Miniaudio.ma_device_start(pNoiseDevice) != ma_result.MA_SUCCESS)
        {
            Console.WriteLine("Failed to start playback device.\n");
            StopSounds();
            return;
        }
        #endregion
    }
    public static unsafe void StopSounds()
    {
        Miniaudio.ma_device_uninit(pToneDevice);
        Miniaudio.ma_context_uninit(pToneContext);
        NativeMemory.Free(pToneContext);
        NativeMemory.Free(pToneDevice);

        Miniaudio.ma_device_uninit(pNoiseDevice);
        Miniaudio.ma_context_uninit(pNoiseContext);
        NativeMemory.Free(pNoiseContext);
        NativeMemory.Free(pNoiseDevice);
        NativeMemory.Free(lpf);
        NativeMemory.Free(hpf);
        NativeMemory.Free(bpf);
        NativeMemory.Free(notch2);
        NativeMemory.Free(peak2);
        NativeMemory.Free(loshelf2);
        NativeMemory.Free(hishelf2);
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

    private static double randomStart = Random.Shared.NextDouble() * 32768;
    private static double brownpos;
    private static readonly PinkNoise source = new(alpha, poles);
    private const double alpha = 1.0;
    private const int poles = 5;
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
        _ = RandomNumberGenerator.Create();
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

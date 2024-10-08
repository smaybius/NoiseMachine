﻿using Avalonia;
using NoiseMachineDotNet.ViewModels;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NoiseMachineDotNet.Desktop
{
    internal class Program
    {
        private const string LibraryName = "miniaudio";
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static int Main(string[] args)
        {
            string subfolder = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm32",
                _ => throw new PlatformNotSupportedException("Dude, wuts ur processor arch????"),
            };
            string libraryPath = "If dis string no change, dis app dunno ur pc or cpu. Thankies 4 comin 2 muh ted tawk ^w^";
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
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // Are we rich laymen?
            {
                libraryPath = $"libs/{LibraryName}.dylib";
            }

            NativeLibrary.SetDllImportResolver(typeof(MainViewModel).Assembly, (name, assembly, path) =>
            {
                return name == LibraryName
                    ? NativeLibrary.Load(libraryPath)
                    : throw new Exception("Where's miniaudio? TELL ME NOW NOW NOOOOWWW!!!!!! It ain't in " + name);
            });
            AppBuilder builder = BuildAvaloniaApp();
            if (args.Contains("--drm"))
            {

                // If Card0, Card1 and Card2 all don't work. You can also try:                 
                // return builder.StartLinuxFbDev(args);
                // return builder.StartLinuxDrm(args, "/dev/dri/card1");
                return builder.StartLinuxDrm(args, "/dev/dri/card1", 1D);
            }

            return builder.StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                        .UsePlatformDetect()
                        .WithInterFont()
                        .LogToTrace();
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using D3D12SDKVersionPatcher.Utilities;

namespace D3D12SDKVersionPatcher
{
    internal class Program
    {
        static void Main()
        {
            if (Debugger.IsAttached)
            {
                SafeMain();
                return;
            }

            try
            {
                SafeMain();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static void SafeMain()
        {
            Console.WriteLine("D3D12SDKVersionPatcher - Copyright (C) 2022-" + DateTime.Now.Year + " Simon Mourier. All rights reserved.");
            Console.WriteLine();
            if (CommandLine.Current.HelpRequested)
            {
                Help();
                return;
            }

            var inputFilePath = CommandLine.Current.GetNullifiedArgument(0);
            var version = CommandLine.Current.GetArgument<int>(1);
            var path = CommandLine.Current.GetNullifiedArgument(2);
            var outputFilePath = CommandLine.Current.GetNullifiedArgument(3);
            if (inputFilePath == null || version == 0 || path == null)
            {
                Help();
                return;
            }

            if (outputFilePath == null)
            {
                var name = Path.GetFileNameWithoutExtension(inputFilePath);
                outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath)!, name + ".D3D12_" + version + Path.GetExtension(inputFilePath));
            }

            Console.WriteLine("Input           : " + inputFilePath);
            Console.WriteLine("Output          : " + outputFilePath);
            Console.WriteLine("D3D12SDKVersion : " + version);
            Console.WriteLine("D3D12SDKPath    : " + path);

            D3D12SdkVersion.PatchExe(inputFilePath, version, path, outputFilePath);
            Console.WriteLine();
            Console.WriteLine($"File {outputFilePath} was succesfully written.");
        }

        static void Help()
        {
            Console.WriteLine(Assembly.GetEntryAssembly()!.GetName().Name!.ToUpperInvariant() + " <input file path> <D3D12SDKVersion> <D3D12SDKPath> [output file path]");
            Console.WriteLine();
            Console.WriteLine("Description:");
            Console.WriteLine("    This tool patches a .exe file to add DirectX 12 Agility SDK version exports.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine();
            Console.WriteLine("    " + Assembly.GetEntryAssembly()!.GetName().Name!.ToUpperInvariant() + " c:\\mypath\\myproject.exe 611 \"\\D3D12\\\"");
            Console.WriteLine();
            Console.WriteLine("    Patches the c:\\mypath\\myproject.exe file to set version to '611' and directory path to '\\D3D12\\'.");
            Console.WriteLine();
        }
    }
}

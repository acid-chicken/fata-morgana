#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AcidChicken.FataMorgana
{
    partial class Program
    {
        static readonly string _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FataMorgana", "converter");
        static readonly string _inPath = Directory.CreateDirectory(Path.Combine(_basePath, "in")).FullName;
        static readonly string _outPath = Directory.CreateDirectory(Path.Combine(_basePath, "out")).FullName;
        static readonly IReadOnlyCollection<string> _ignore = new []
        {
            ".DS_Store",
            "thumbs.db",
            "windows.ini"
        };
        static readonly IReadOnlyCollection<string> _initial = new []
        {
            "mp4",
            "wav"
        };
        static readonly IReadOnlyCollection<string> _videoArgs = new []
        {
            "-c:v copy -c:a copy",
            "-c:v copy -b:a 1G -af astats",
            "-b:v 1G -c:a copy",
            "-b:v 1G -b:a 1G -af astats"
        };
        static readonly IReadOnlyCollection<string> _audioArgs = new []
        {
            // "-c:a copy",
            "-b:a 1G -af astats"
        };
        static readonly FileSystemWatcher _watcher = new FileSystemWatcher(_inPath)
        {
            IncludeSubdirectories = true
        };

        static void WatcherStart()
        {
            foreach (var item in _initial)
            {
                Directory.CreateDirectory(Path.Combine(_inPath, item));
            }

            _watcher.Created += (sender, e) =>
            {
                var section = e.Name.Split(Path.DirectorySeparatorChar);

                if (section.Length != 2 || _ignore.Contains(section[1]))
                {
                    return;
                }

                var src = $"\"{EscapePath(Path.Join(_inPath, e.Name))}\"";

                var dst = $"\"{EscapePath(Path.Join(_outPath, string.Join('.', section.Reverse())))}\"";

                var ffmpeg = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";

                var ffprobe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffprobe.exe" : "ffprobe";

                var probe = Process.Start(new ProcessStartInfo(ffprobe, $"-v fatal -select_streams v:0 -show_entries stream=width -of csv=p=0 {src}")
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });

                var video = probe.StandardOutput.ReadToEnd().Length != 0;

                probe.WaitForExit();

                foreach (var arg in video ? _videoArgs : _audioArgs)
                {
                    var process = Process.Start(new ProcessStartInfo(ffmpeg, $"-v fatal -stats -hide_banner -y -i {src} {arg} {dst}")
                    {
                        CreateNoWindow = true
                    });

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        break;
                    }
                }

                Console.WriteLine($"変換完了: {src}");
            };

            _watcher.EnableRaisingEvents = true;
        }

        static void WatcherStop()
        {
            _watcher.Dispose();
        }

        static string EscapePath(string source) => source
            .Replace("\0", "")
            .Replace(@"\\", "\0")
            .Replace("\"", @"\""")
            .Replace("\0", @"\\");
    }
}

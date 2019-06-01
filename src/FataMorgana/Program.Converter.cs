using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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

                var src = Path.Join(_inPath, e.Name);

                var dst = Path.Join(_outPath, string.Join('.', section.Reverse()));

                var copy = Process.Start(new ProcessStartInfo(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg", $"-loglevel fatal -y -i \"{EscapePath(src)}\" -vcodec copy -acodec copy \"{EscapePath(dst)}\"")
                {
                    CreateNoWindow = true
                });

                copy.EnableRaisingEvents = true;

                copy.Exited += (_, __) =>
                {
                    if (copy.ExitCode != 0)
                    {
                        var vcopy = Process.Start(new ProcessStartInfo(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg", $"-loglevel fatal -y -i \"{EscapePath(src)}\" -vcodec copy \"{EscapePath(dst)}\"")
                        {
                            CreateNoWindow = true
                        });

                        vcopy.EnableRaisingEvents = true;

                        vcopy.Exited += (___, ____) =>
                        {
                            if (vcopy.ExitCode != 0)
                            {
                                var acopy = Process.Start(new ProcessStartInfo(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg", $"-loglevel fatal -y -i \"{EscapePath(src)}\" -acodec copy \"{EscapePath(dst)}\"")
                                {
                                    CreateNoWindow = true
                                });

                                acopy.EnableRaisingEvents = true;

                                acopy.Exited += (_____, ______) =>
                                {
                                    if (acopy.ExitCode != 0)
                                    {
                                        var nocopy = Process.Start(new ProcessStartInfo(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg", $"-y -i \"{EscapePath(src)}\" -b 1G -ab 1G \"{EscapePath(dst)}\"")
                                        {
                                            CreateNoWindow = true
                                        });
                                    }
                                };
                            }
                        };
                    }
                };

                copy.WaitForExit();
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

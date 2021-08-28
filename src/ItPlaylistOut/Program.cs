using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CommandLine;
using CommandLine.Text;
using SimplePlistXmlRead;

namespace ItPlaylistOut
{
    internal static class Program
    {
        private const string LibraryFile = "iTunes Music Library.xml";

        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(Process);
        }

        private static void Process(Options opts)
        {
            var xml = XElement.Load(Path.Combine(opts.LibraryPath, LibraryFile));
            var rootDict = new PlistDict(xml.Elements("dict").First());

            if (!rootDict.TryGetDictionary("Tracks", out var tracks))
            {
                Console.WriteLine("Failed to get Tracks section in src file");
                return;
            }

            if (!rootDict.TryGetArray("Playlists", out var playlists))
            {
                Console.WriteLine("Failed to get Playlists section in src file");
                return;
            }

            if (playlists.OfType<PlistDict>().FirstOrDefault(v =>
                v.TryGetString("Name", out string? value) && value == opts.Playlist) is not { } playlist)
            {
                Console.WriteLine($"Failed to find playlist named {opts.Playlist} in src file");
                return;
            }

            if (!playlist.TryGetArray("Playlist Items", out var items))
            {
                Console.WriteLine("Failed to find playlist content data in src file");
                return;
            }

            var itemKeys = items.OfType<PlistDict>()
                .Select(d => d.TryGetInteger("Track ID", out var value) ? value : null);

            foreach (long? x in itemKeys)
            {
                if (x is not { } v) continue;

                if (tracks.TryGetValue(v.ToString(), out var t) && t is PlistDict track)
                {
                    if (!track.TryGetString("Name", out string? name))
                    {
                        Console.WriteLine($"Warning: Missing name on track {v}");
                        name = $"??? (ID {v}";
                    }

                    if (!track.TryGetString("Location", out string? location))
                    {
                        Console.WriteLine($"Warning: Missing location on track \"{name}\"");
                    }

                    Console.WriteLine($"{name} - {location}");
                    // TODO read meta based on filetype
                }
            }
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class Options
        {
            [Value(0,
                MetaName = "libraryPath",
                MetaValue = "path",
                HelpText = "Path to iTunes library.",
                Required = true)]
            public string LibraryPath { get; set; } = null!;

            [Value(1,
                MetaName = "playlist",
                MetaValue = "name",
                HelpText = "Playlist to extract.",
                Required = true)]
            public string Playlist { get; set; } = null!;

            [Value(2,
                MetaName = "outFile",
                MetaValue = "path",
                HelpText = "Output file.",
                Required = true)]
            public string OutFile { get; set; } = null!;

            // ReSharper disable once UnusedMember.Local
            [Usage] public static Example[] Examples => s_examples;

            private static readonly Example[] s_examples =
            {
                new("Export playlist", new Options
                {
                    LibraryPath = @"<libraryPath>", Playlist = "<playlist>", OutFile = "<outFile>"
                })
            };
        }
    }
}

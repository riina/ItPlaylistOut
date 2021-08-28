using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using CommandLine;
using CommandLine.Text;
using ItMusicInfo;
using ItMusicInfo.Itunes.Windows;
using File = System.IO.File;

namespace ItPlaylistOut
{
    internal static class Program
    {
        private static readonly JsonSerializerOptions s_jso = new() {IgnoreNullValues = true};
        private static readonly JsonWriterOptions s_jwo = new() {Indented = true};

        private static void Main(string[] args) => Parser.Default.ParseArguments<Options>(args).WithParsed(Process);

        private static void Process(Options opts)
        {
            var pl = WindowsItunes.GetPlaylist(opts.LibraryPath, opts.Playlist);
            if (pl == null)
            {
                Console.WriteLine("Failed to get playlist");
                return;
            }

            if (opts.JacketFolder != null)
                foreach (var song in pl.Songs)
                    if (song.Jacket != null)
                        Jackets.WriteImage(opts.JacketFolder, opts.LosslessJackets, song.Jacket);

            Directory.CreateDirectory(Path.GetDirectoryName(opts.OutFile)!);
            using FileStream fs = File.Create(opts.OutFile);
            JsonSerializer.Serialize(new Utf8JsonWriter(fs, s_jwo), pl, s_jso);
        }
    }


    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    internal class Options
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

        [Option('j', "jacketFolder",
            MetaValue = "path",
            HelpText = "Output directory for jackets.")]
        public string? JacketFolder { get; set; } = null!;

        [Option('l', "losslessJacket",
            HelpText = "Output lossless jackets.")]
        public bool LosslessJackets { get; set; }

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

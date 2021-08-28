using System.Diagnostics.CodeAnalysis;
using CommandLine;
using CommandLine.Text;

namespace ItPlaylistOut
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
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

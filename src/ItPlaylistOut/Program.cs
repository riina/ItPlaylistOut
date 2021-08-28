using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using CommandLine;
using ItSongMeta;
using SimplePlistXmlRead;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using File = System.IO.File;

namespace ItPlaylistOut
{
    internal static class Program
    {
        private const string LibraryFile = "iTunes Music Library.xml";
        private static readonly JsonSerializerOptions s_jso = new() {IgnoreNullValues = true};
        private static readonly JsonWriterOptions s_jwo = new() {Indented = true};

        private static void Main(string[] args) => Parser.Default.ParseArguments<Options>(args).WithParsed(Process);

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
                v.TryGetString("Name", out string? value) &&
                string.Equals(value, opts.Playlist, StringComparison.InvariantCultureIgnoreCase)) is not { } playlist)
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
                .Select(d => d.TryGetInteger("Track ID", out long? value) ? value : null).ToList();

            var pl = new PlaylistData {Name = (string)(playlist["Name"] as PlistString)!, Songs = new List<SongData>()};
            int i = 0, c = itemKeys.Count;
            foreach (long? x in itemKeys)
            {
                Console.WriteLine($"{++i}/{c}...");
                if (x is not { } v) continue;

                if (tracks.TryGetValue(v.ToString(), out var t) && t is PlistDict track)
                {
                    // If file's found, work purely off file
                    if (track.TryGetString("Location", out string? location))
                    {
                        var songData = SongData.Extract(DecodePath(TrimNetpathStart(location)));
                        if (songData.Jacket != null) WriteImage(opts, songData.Jacket);
                        pl.Songs.Add(songData);
                    }
                    else
                    {
                        var songData = new SongData();
                        if (!track.TryGetString("Name", out string? name))
                        {
                            Console.WriteLine($"Warning: Missing name on track {v}");
                            name = $"??? (ID {v})";
                        }

                        songData.Name = name;
                        Console.WriteLine($"Warning: Missing location on track \"{name}\"");
                        if (track.TryGetString("Album", out string? album))
                            songData.Album = album;

                        string? artist;
                        if (track.TryGetString("Artist", out artist))
                            goto artistDone;
                        if (track.TryGetString("Album Artist", out artist))
                            goto artistDone;
                        artist = null;
                        artistDone:
                        songData.Artist = artist;
                        pl.Songs.Add(songData);
                    }
                }
            }

            MakeDirsForFile(opts.OutFile);
            using FileStream fs = File.Create(opts.OutFile);
            JsonSerializer.Serialize(new Utf8JsonWriter(fs, s_jwo), pl, s_jso);
        }

        private static readonly Regex _pathRegex = new(@"(%[A-Za-z\d]{2})+");

        private static string DecodePath(string path) =>
            _pathRegex.Replace(path, match => HttpUtility.UrlDecode(match.Value));

        private static string TrimNetpathStart(string path)
        {
            const string lhs = "file://localhost/";
            return path.StartsWith(lhs) ? path[lhs.Length..] : path;
        }

        private static void WriteImage(Options opts, JacketInfo jacket)
        {
            if (opts.JacketFolder == null) return;
            string file = Path.Combine(opts.JacketFolder, jacket.Sha1);
            if (opts.LosslessJackets)
            {
                EncodeLossless(jacket, file);
            }
            else
            {
                switch (jacket.Extension)
                {
                    case ".png":
                        EncodeLossless(jacket, file);
                        break;
                    case "":
                        {
                            using var img = Image.Load(jacket.Value, out var format);
                            if (format is JpegFormat) Save(jacket with {Extension = ".jpg"}, file);
                            else EncodeLossless(img, file);
                            break;
                        }
                    default:
                        Save(jacket, file);
                        break;
                }
            }
        }

        private static readonly PngEncoder s_pngEncoder = new();

        private static void EncodeLossless(JacketInfo jacket, string file)
        {
            file += ".png";
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            using var img = Image.Load(jacket.Value);
            img.Save(file, s_pngEncoder);
        }

        private static void EncodeLossless(Image img, string file)
        {
            file += ".png";
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            img.Save(file, s_pngEncoder);
        }

        private static void Save(JacketInfo jacket, string file)
        {
            file += jacket.Extension;
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            File.WriteAllBytes(file, jacket.Value);
        }

        private static void MakeDirsForFile(string file) => Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    }
}

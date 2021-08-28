using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Web;
using System.Xml.Linq;
using CommandLine;
using SimplePlistXmlRead;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TagLib;
using TagLib.Mpeg4;
using File = System.IO.File;

namespace ItPlaylistOut
{
    internal static class Program
    {
        private const string LibraryFile = "iTunes Music Library.xml";
        private static readonly JsonSerializerOptions s_jso = new() {IgnoreNullValues = true};
        private static readonly JsonWriterOptions s_jwo = new() {Indented = true};

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
                    var songData = new SongData();
                    if (!track.TryGetString("Name", out string? name))
                    {
                        Console.WriteLine($"Warning: Missing name on track {v}");
                        name = $"??? (ID {v}";
                    }

                    songData.Name = name;

                    if (!track.TryGetString("Location", out string? location))
                        Console.WriteLine($"Warning: Missing location on track \"{name}\"");
                    var tagfile = location != null
                        ? TagLib.File.Create(HttpUtility.UrlDecode(TrimNetpathStart(location)))
                        : null;

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

                    SourceInfo? link;
                    if (tagfile != null)
                    {
                        if (tagfile.GetTag(TagTypes.Apple) is AppleTag {Comment: { }} mp4Apple)
                        {
                            if (!TryGetDlLink(mp4Apple.Comment, out link)) link = null;
                            goto linkDone;
                        }

                        if (tagfile.GetTag(TagTypes.Id3v2) is TagLib.Id3v2.Tag {Comment: { }} mp3)
                        {
                            if (!TryGetDlLink(mp3.Comment, out link)) link = null;
                            goto linkDone;
                        }
                    }

                    link = null;
                    linkDone:
                    songData.Link = link?.Link;
                    songData.Provider = link?.Provider;

                    string? jacketSha1;
                    if (tagfile != null)
                    {
                        if (tagfile.GetTag(TagTypes.Apple) is AppleTag {Comment: { }} mp4Apple)
                        {
                            if (!TryGetImageSha1(mp4Apple.Pictures, out jacketSha1)) jacketSha1 = null;
                            goto artDone;
                        }

                        if (tagfile.GetTag(TagTypes.Id3v2) is TagLib.Id3v2.Tag {Comment: { }} mp3)
                        {
                            if (!TryGetImageSha1(mp3.Pictures, out jacketSha1)) jacketSha1 = null;
                            goto artDone;
                        }
                    }

                    jacketSha1 = null;
                    artDone:
                    songData.JacketSha1 = jacketSha1;

                    pl.Songs.Add(songData);
                }
            }

            using FileStream fs = File.Create(opts.OutFile);
            JsonSerializer.Serialize(new Utf8JsonWriter(fs, s_jwo), pl, s_jso);
        }

        private static bool TryGetImageSha1(IPicture[] pictures, out string? sha1)
        {
            if (pictures.Length == 0) goto fail;
            try
            {
                using Image<Rgba32> img = Image.Load(pictures[0].Data.Data);
                int w = img.Width, h = img.Height;
                if (!img.TryGetSinglePixelSpan(out var span))
                {
                    span = new Rgba32[w * h];
                    for (int y = 0; y < h; y++)
                        img.GetPixelRowSpan(y).CopyTo(span.Slice(w * y, w));
                }

                sha1 = Convert.ToHexString(SHA1.HashData(MemoryMarshal.Cast<Rgba32, byte>(span)));
                return true;
            }
            catch
            {
                // fail
            }

            fail:
            sha1 = null;
            return false;
        }

        private static bool TryGetDlLink(string text, [NotNullWhen(true)] out SourceInfo? link)
        {
            var res = CommentRegex.Regexes
                .Select(r => (Match: r.regex.Match(text), Provider: r.provider, Transform: r.transform))
                .FirstOrDefault(v => v.Match.Success);
            if (res != default)
            {
                link = new SourceInfo(res.Transform(res.Match.Groups[0].Value), res.Provider);
                return true;
            }

            link = default;
            return false;
        }

        private record SourceInfo(string? Link, string Provider);

        private static string TrimNetpathStart(string path)
        {
            const string lhs = "file://localhost/";
            return path.StartsWith(lhs) ? path[lhs.Length..] : path;
        }
    }
}

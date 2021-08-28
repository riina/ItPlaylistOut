using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using CommandLine;
using SimplePlistXmlRead;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
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
                        name = $"??? (ID {v})";
                    }

                    songData.Name = name;

                    if (track.TryGetString("Location", out string? location))
                        location = DecodePath(TrimNetpathStart(location));
                    else
                        Console.WriteLine($"Warning: Missing location on track \"{name}\"");
                    var tagfile = location != null ? TagLib.File.Create(location) : null;

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
                        if (TryGetTag(tagfile, out AppleTag? mp4Apple))
                        {
                            if (mp4Apple.Copyright != null) songData.Copyright = mp4Apple.Copyright;
                        }
                    }

                    if (tagfile != null)
                    {
                        if (TryGetTag(tagfile, out AppleTag? mp4Apple))
                        {
                            if (mp4Apple.Comment != null)
                            {
                                if (!TryGetDlLink(mp4Apple.Comment, out link)) link = null;
                                goto linkDone;
                            }

                            // https://music.apple.com/us/album/arcahv/1523598787?i=1523598795
                            // Z<bh:d0>E<bh:cb>
                            // https://music.apple.com/us/album/genesong-feat-steve-vai/1451411999?i=1451412277
                            // 1451412277 V<bh:82><bh:cb>5 @0x10896 cnID
                            // 1451411999 V<bh:82><bh:ca><bh:1f> @0x10907 plID
                            // extract from song
                            var fp = new FileParser(tagfile);
                            fp.ParseBoxHeaders();
                            var tfd = new TagFileData(tagfile);
                            /*if (tfd.GetAtPath("moov/trak/mdia/minf/stbl/stsd/mp4a/pinf/schi/righ",
                                out var tag) && tag.Header.Header.TotalBoxSize == 0x58)
                            {
                                byte[] fileData = File.ReadAllBytes(location!);
                                int id = BinaryPrimitives.ReadInt32BigEndian(
                                    fileData.AsSpan((int)(tag.Header.Header.Position + 52)));
                                Console.WriteLine($"{name} -- {id}");
                            }*/

                            try
                            {
                                if (tfd.GetAtPath("moov/udta/meta/ilst/plID/data", out var plID) &&
                                    tfd.GetAtPath("moov/udta/meta/ilst/cnID/data", out var cnID) /* &&
                                    tfd.GetAtPath("moov/udta/meta/ilst/sonm/data", out var sonm)*/)
                                {
                                    Span<byte> fileData = File.ReadAllBytes(location!).AsSpan();
                                    int plIDV = BinaryPrimitives.ReadInt32BigEndian(
                                        fileData[(int)plID.Header.GetContentPosition(12)..]);
                                    int cnIDV = BinaryPrimitives.ReadInt32BigEndian(
                                        fileData[(int)cnID.Header.GetContentPosition(8)..]);
                                    /*string sonmV = Encoding.UTF8.GetString(
                                        fileData[(int)sonm.Header.GetContentPosition(8)
                                            ..(int)sonm.Header.GetContentPositionFromEnd()]);*/
                                    link = new SourceInfo(
                                        $"https://music.apple.com/us/album/{plIDV}?i={cnIDV}",
                                        "Apple Music");
                                    goto linkDone;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }

                            link = null;
                            goto linkDone;
                        }

                        if (TryGetTag(tagfile, out TagLib.Id3v2.Tag? mp3) && mp3.Comment != null)
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
                        if (TryGetTag(tagfile, out AppleTag? mp4Apple))
                        {
                            if (!TryGetImageSha1(mp4Apple.Pictures, opts, out jacketSha1)) jacketSha1 = null;
                            goto artDone;
                        }

                        if (TryGetTag(tagfile, out TagLib.Id3v2.Tag? mp3))
                        {
                            if (!TryGetImageSha1(mp3.Pictures, opts, out jacketSha1)) jacketSha1 = null;
                            goto artDone;
                        }
                    }

                    jacketSha1 = null;
                    artDone:
                    songData.JacketSha1 = jacketSha1;

                    pl.Songs.Add(songData);
                }
            }

            MakeDirsForFile(opts.OutFile);
            using FileStream fs = File.Create(opts.OutFile);
            JsonSerializer.Serialize(new Utf8JsonWriter(fs, s_jwo), pl, s_jso);
        }

        private static bool TryGetTag<T>(TagLib.File file, [NotNullWhen(true)] out T? tag) where T : Tag
        {
            switch (file.Tag)
            {
                case T tt:
                    tag = tt;
                    return true;
                case CombinedTag ct:
                    tag = ct.Tags.OfType<T>().FirstOrDefault();
                    return tag != null;
                default:
                    tag = default;
                    return false;
            }
        }

        private static readonly Regex _pathRegex = new(@"(%[A-Za-z\d]{2})+");

        private static string DecodePath(string path) =>
            _pathRegex.Replace(path, match => HttpUtility.UrlDecode(match.Value));

        private static readonly PngEncoder s_pngEncoder = new();
        private static readonly JpegEncoder s_jpegEncoder = new();

        private static bool TryGetImageSha1(IPicture[] pictures, Options opts, out string? sha1)
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
                if (opts.JacketFolder != null)
                {
                    string targetFile =
                        Path.Combine(opts.JacketFolder, sha1 + (opts.LosslessJackets ? ".png" : ".jpg"));
                    if (!File.Exists(targetFile))
                        try
                        {
                            MakeDirsForFile(targetFile);
                            img.Save(targetFile, opts.LosslessJackets ? s_pngEncoder : s_jpegEncoder);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Failed to write image for sha1-{sha1}\n{e}");
                        }
                }

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

        private static void MakeDirsForFile(string file) => Directory.CreateDirectory(Path.GetDirectoryName(file)!);

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

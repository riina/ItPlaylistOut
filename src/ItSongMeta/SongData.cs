using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TagLib;
using TagLib.Mpeg4;

namespace ItSongMeta
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class SongData
    {
        public string Name { get; set; } = null!;

        public string? Album { get; set; }

        public string? Artist { get; set; }

        public string? Provider { get; set; }

        public string? Link { get; set; }

        public string? JacketSha1 { get; set; }

        public string? Copyright { get; set; }

        [JsonIgnore] public JacketInfo? Jacket { get; set; }

        public static SongData Extract(string path)
        {
            var songData = new SongData();
            var tagfile = TagLib.File.Create(path);
            var tags = tagfile.Tag;
            songData.Name = tags.Title;
            songData.Album = tags.Album;
            songData.Copyright = tags.Copyright;
            songData.Artist = tags.Performers.FirstOrDefault();
            songData.Artist ??= tags.AlbumArtists.FirstOrDefault();

            SourceInfo? link;

            if (tags.Comment != null)
            {
                if (!TryGetDlLink(tags.Comment, out link)) link = null;
                goto linkDone;
            }

            if (TryGetTag(tagfile, out AppleTag? mp4Apple))
            {
                // https://music.apple.com/us/album/arcahv/1523598787?i=1523598795
                // Z<bh:d0>E<bh:cb>
                // https://music.apple.com/us/album/genesong-feat-steve-vai/1451411999?i=1451412277
                // 1451412277 V<bh:82><bh:cb>5 @0x10896 cnID
                // 1451411999 V<bh:82><bh:ca><bh:1f> @0x10907 plID
                // extract from song
                // just kidding use DataBoxes
                var plID = mp4Apple.DataBoxes("plID").FirstOrDefault();
                var cnID = mp4Apple.DataBoxes("cnID").FirstOrDefault();
                if (plID != null && cnID != null)
                {
                    long plIDV = BinaryPrimitives.ReadInt64BigEndian(plID.Data.Data);
                    int cnIDV = BinaryPrimitives.ReadInt32BigEndian(cnID.Data.Data);
                    link = new SourceInfo(
                        $"https://music.apple.com/us/album/{plIDV}?i={cnIDV}",
                        "Apple Music");
                    goto linkDone;
                }

                link = null;
                goto linkDone;
            }

            link = null;
            linkDone:
            songData.Link = link?.Link;
            songData.Provider = link?.Provider;

            if (TryGetImageSha1(tags.Pictures, out var jacketSha1))
            {
                songData.Jacket = jacketSha1;
                songData.JacketSha1 = jacketSha1?.Sha1;
            }

            return songData;
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

        private static bool TryGetImageSha1(IPicture[] pictures, out JacketInfo? jacket)
        {
            if (pictures.Length == 0) goto fail;
            try
            {
                var p0 = pictures[0];
                byte[] buf = p0.Data.Data;
                using Image<Rgba32> img = Image.Load(buf);
                int w = img.Width, h = img.Height;
                if (!img.TryGetSinglePixelSpan(out var span))
                {
                    span = new Rgba32[w * h];
                    for (int y = 0; y < h; y++)
                        img.GetPixelRowSpan(y).CopyTo(span.Slice(w * y, w));
                }

                string sha1 = Convert.ToHexString(SHA1.HashData(MemoryMarshal.Cast<Rgba32, byte>(span)));
                jacket = new JacketInfo(sha1, Path.GetExtension(p0.Filename).ToLowerInvariant(), buf);
                return true;
            }
            catch
            {
                // fail
            }

            fail:
            jacket = null;
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
    }
}

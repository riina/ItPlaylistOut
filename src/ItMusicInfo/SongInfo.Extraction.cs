using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TagLib;
using TagLib.Mpeg4;

namespace ItMusicInfo
{
    public partial class SongInfo
    {
        public static SongInfo Extract(string path, bool readJacketSha1 = false)
        {
            var songData = new SongInfo();
            songData.LoadCore(path, readJacketSha1);
            return songData;
        }

        public void LoadCore(string path, bool readJacketSha1 = false)
        {
            FilePath = path;
            var tagfile = TagLib.File.Create(path);
            var tags = tagfile.Tag;
            Name = tags.Title;
            Album = tags.Album;
            Copyright = tags.Copyright;
            Artist = tags.Performers.FirstOrDefault();
            Artist ??= tags.AlbumArtists.FirstOrDefault();

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
            Link = link?.Link;
            Provider = link?.Provider;

            if (readJacketSha1 && GetJacketInfo(tagfile.Tag.Pictures) is { } jacketInfo) JacketSha1 = jacketInfo.Sha1;
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

        public JacketInfo? GetJacketInfo(Dictionary<string, JacketInfo>? registry = null, bool setSha1 = true)
        {
            if (FilePath == null) return null;
            var tagfile = TagLib.File.Create(FilePath);
            if (tagfile.Tag.Pictures.Length == 0) return null;
            var p0 = tagfile.Tag.Pictures[0];
            byte[] buf = p0.Data.Data;
            string totalHash = Convert.ToHexString(SHA1.HashData(buf));
            JacketInfo? res;
            if (registry != null && registry.TryGetValue(totalHash, out var info))
                res = info;
            else
            {
                res = GetJacketInfo(buf, p0.Filename ?? "");
                if (registry != null) registry[totalHash] = res;
            }

            if (setSha1 && res != null) JacketSha1 = res.Sha1;
            return res;
        }

        private static JacketInfo? GetJacketInfo(IPicture[] pictures)
        {
            if (pictures.Length == 0) return null;
            try
            {
                var picture = pictures[0];
                return GetJacketInfo(picture.Data.Data, picture.Filename ?? "");
            }
            catch
            {
                return null;
            }
        }

        private static JacketInfo GetJacketInfo(byte[] buf, string filename)
        {
            using Image<Rgba32> img = Image.Load(buf);
            return new JacketInfo(Sha1(img), Path.GetExtension(filename).ToLowerInvariant(), buf);
        }

        private static string Sha1(Image<Rgba32> img)
        {
            int w = img.Width, h = img.Height;
            if (!img.TryGetSinglePixelSpan(out var span))
            {
                span = new Rgba32[w * h];
                for (int y = 0; y < h; y++)
                    img.GetPixelRowSpan(y).CopyTo(span.Slice(w * y, w));
            }

            return Convert.ToHexString(SHA1.HashData(MemoryMarshal.Cast<Rgba32, byte>(span)));
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

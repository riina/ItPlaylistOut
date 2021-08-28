using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace ItMusicInfo
{
    public static class Jackets
    {
        private static readonly PngEncoder s_pngEncoder = new();

        public static void WriteImage(string jacketFolder, bool losslessJackets, JacketInfo jacket)
        {
            string file = Path.Combine(jacketFolder, jacket.Sha1);
            if (losslessJackets)
                EncodeLossless(jacket, file);
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

        public static void EncodeLossless(JacketInfo jacket, string file)
        {
            file += ".png";
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            using var img = Image.Load(jacket.Value);
            img.Save(file, s_pngEncoder);
        }

        public static void EncodeLossless(Image img, string file)
        {
            file += ".png";
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            img.Save(file, s_pngEncoder);
        }

        public static void Save(JacketInfo jacket, string file)
        {
            file += jacket.Extension;
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            File.WriteAllBytes(file, jacket.Value);
        }

        private static void MakeDirsForFile(string file) => Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    }
}

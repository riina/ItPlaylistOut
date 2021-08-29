using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

        public static async Task WriteImageAsync(string jacketFolder, bool losslessJackets, JacketInfo jacket,
            CancellationToken cancellationToken)
        {
            string file = Path.Combine(jacketFolder, jacket.Sha1);
            if (losslessJackets)
                await EncodeLosslessAsync(jacket, file, cancellationToken);
            else
            {
                switch (jacket.Extension)
                {
                    case ".png":
                        await EncodeLosslessAsync(jacket, file, cancellationToken);
                        break;
                    case "":
                        {
                            using var img = Image.Load(jacket.Value, out var format);
                            if (format is JpegFormat)
                                await SaveAsync(jacket with {Extension = ".jpg"}, file, cancellationToken);
                            else await EncodeLosslessAsync(img, file, cancellationToken);
                            break;
                        }
                    default:
                        await SaveAsync(jacket, file, cancellationToken);
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

        public static async Task EncodeLosslessAsync(JacketInfo jacket, string file,
            CancellationToken cancellationToken)
        {
            file += ".png";
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            using var img = Image.Load(jacket.Value);
            await img.SaveAsync(file, s_pngEncoder, cancellationToken: cancellationToken);
        }

        public static void EncodeLossless(Image img, string file)
        {
            file += ".png";
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            img.Save(file, s_pngEncoder);
        }

        public static async Task EncodeLosslessAsync(Image img, string file, CancellationToken cancellationToken)
        {
            file += ".png";
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            await img.SaveAsync(file, s_pngEncoder, cancellationToken: cancellationToken);
        }

        public static void Save(JacketInfo jacket, string file)
        {
            file += jacket.Extension;
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            File.WriteAllBytes(file, jacket.Value);
        }

        public static async Task SaveAsync(JacketInfo jacket, string file, CancellationToken cancellationToken)
        {
            file += jacket.Extension;
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            await File.WriteAllBytesAsync(file, jacket.Value, cancellationToken);
        }

        private static void MakeDirsForFile(string file) => Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    }
}

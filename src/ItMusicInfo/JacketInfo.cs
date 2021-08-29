using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace ItMusicInfo
{
    public record JacketInfo(string Sha1, string Extension, byte[] Value)
    {
        private static readonly PngEncoder s_pngEncoder = new();

        public void WriteImage(string jacketFolder, bool losslessJackets)
        {
            string file = Path.Combine(jacketFolder, Sha1);
            if (losslessJackets)
                EncodePng(Path.ChangeExtension(file, ".png"));
            else
            {
                switch (Extension)
                {
                    case ".png":
                        EncodePng(Path.ChangeExtension(file, ".png"));
                        break;
                    case "":
                        {
                            using var img = Image.Load(Value, out var format);
                            if (format is JpegFormat) Save(file + ".jpg");
                            else EncodePng(img, Path.ChangeExtension(file, ".png"));
                            break;
                        }
                    default:
                        Save(file + Extension);
                        break;
                }
            }
        }

        public async Task WriteImageAsync(string jacketFolder, bool losslessJackets, CancellationToken cancellationToken)
        {
            string file = Path.Combine(jacketFolder, Sha1);
            if (losslessJackets)
                await EncodePngAsync(Path.ChangeExtension(file, ".png"), cancellationToken);
            else
            {
                switch (Extension)
                {
                    case ".png":
                        await EncodePngAsync(Path.ChangeExtension(file, ".png"), cancellationToken);
                        break;
                    case "":
                        {
                            using var img = Image.Load(Value, out var format);
                            if (format is JpegFormat)
                                await SaveAsync(file + ".jpg", cancellationToken);
                            else await EncodePngAsync(img, Path.ChangeExtension(file, ".png"), cancellationToken);
                            break;
                        }
                    default:
                        await SaveAsync(file + Extension, cancellationToken);
                        break;
                }
            }
        }

        public void EncodePng(string file)
        {
            file += ".png";
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            using var img = Image.Load(Value);
            img.Save(file, s_pngEncoder);
        }

        public async Task EncodePngAsync(string file, CancellationToken cancellationToken)
        {
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            using var img = Image.Load(Value);
            await img.SaveAsync(file, s_pngEncoder, cancellationToken);
        }

        public static void EncodePng(Image img, string file)
        {
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            img.Save(file, s_pngEncoder);
        }

        public static async Task EncodePngAsync(Image img, string file, CancellationToken cancellationToken)
        {
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            await img.SaveAsync(file, s_pngEncoder, cancellationToken);
        }

        public void Save(string file)
        {
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            File.WriteAllBytes(file, Value);
        }

        public async Task SaveAsync(string file, CancellationToken cancellationToken)
        {
            file += Extension;
            if (File.Exists(file)) return;
            MakeDirsForFile(file);
            await File.WriteAllBytesAsync(file, Value, cancellationToken);
        }

        private static void MakeDirsForFile(string file) => Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    }
}

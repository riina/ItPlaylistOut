using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ItMusicInfo
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public partial class SongInfo
    {
        public string Name { get; set; } = null!;

        public string? Album { get; set; }

        public string? Artist { get; set; }

        public string? Provider { get; set; }

        public string? Link { get; set; }

        public string? JacketSha1 { get; set; }

        public string? Copyright { get; set; }

        [JsonIgnore] public string? FilePath { get; set; }
    }
}

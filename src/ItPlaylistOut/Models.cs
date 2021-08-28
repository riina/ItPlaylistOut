using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ItPlaylistOut
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    internal class PlaylistData
    {
        public string Name { get; set; } = null!;
        public List<SongData> Songs { get; set; } = null!;
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    internal class SongData
    {
        public string Name { get; set; } = null!;

        public string? Album { get; set; }

        public string? Artist { get; set; }

        public string? Provider { get; set; }

        public string? Link { get; set; }

        public string? JacketSha1 { get; set; }
    }
}

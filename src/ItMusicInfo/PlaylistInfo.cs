using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ItMusicInfo
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class PlaylistInfo
    {
        public string Name { get; set; } = null!;
        public List<SongInfo> Songs { get; set; } = null!;
    }
}

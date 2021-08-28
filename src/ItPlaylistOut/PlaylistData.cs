﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ItSongMeta;

namespace ItPlaylistOut
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class PlaylistData
    {
        public string Name { get; set; } = null!;
        public List<SongData> Songs { get; set; } = null!;
    }
}

using System.Collections.Generic;

namespace ItMusicInfo
{
    public abstract class MusicLibrary
    {
        // query support would be good lol

        public abstract IEnumerable<SongInfo> GetSongs();
        public abstract IEnumerable<PlaylistInfo> GetPlaylists();
        public abstract PlaylistInfo? GetPlaylist(string playlistName);
    }
}

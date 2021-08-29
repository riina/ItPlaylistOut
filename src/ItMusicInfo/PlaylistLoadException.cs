using System.IO;

namespace ItMusicInfo
{
    public class PlaylistLoadException : IOException
    {
        public readonly string Playlist;

        public PlaylistLoadException(string message, string playlist) : base(message) => Playlist = playlist;
    }
}

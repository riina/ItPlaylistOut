using System.IO;

namespace ItMusicInfo
{
    public class PlaylistNotFoundException : IOException
    {
        public readonly string Playlist;

        public PlaylistNotFoundException(string message, string playlist) : base(message) => Playlist = playlist;
    }
}

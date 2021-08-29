using System.IO;

namespace ItMusicInfo
{
    public class LibraryLoadException : IOException
    {
        public LibraryLoadException(string message) : base(message)
        {
        }
    }
}

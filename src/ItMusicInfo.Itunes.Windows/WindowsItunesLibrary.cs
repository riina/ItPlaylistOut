using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using SimplePlistXmlRead;

namespace ItMusicInfo.Itunes.Windows
{
    public class WindowsItunesLibrary : MusicLibrary
    {
        private const string LibraryFile = "iTunes Music Library.xml";

        private readonly Dictionary<string, PlistValue> s_root;
        private readonly Dictionary<string, PlistValue> s_tracks;
        private readonly PlistValue[] s_playlists;

        public WindowsItunesLibrary(Dictionary<string, PlistValue> root, Dictionary<string, PlistValue> tracks,
            PlistValue[] playlists)
        {
            s_root = root;
            s_tracks = tracks;
            s_playlists = playlists;
        }

        public static async Task<WindowsItunesLibrary> LoadFromFileAsync(string libraryPath,
            CancellationToken cancellationToken)
        {
            XElement? xml;
            await using (var pfs = File.OpenRead(Path.Combine(libraryPath, LibraryFile)))
                xml = await XElement.LoadAsync(pfs, LoadOptions.None, cancellationToken);
            var rootDict = new Dictionary<string, PlistValue>(new PlistDict(xml.Elements("dict").First()));

            if (!rootDict.TryGetDictionary("Tracks", out var tracks))
                throw new LibraryLoadException("Failed to get Tracks section in src file");

            if (!rootDict.TryGetArray("Playlists", out var playlists))
                throw new LibraryLoadException("Failed to get Playlists section in src file");
            return new WindowsItunesLibrary(rootDict, tracks, playlists);
        }

        public override PlaylistInfo? GetPlaylist(string playlistName)
        {
            if (s_playlists.OfType<PlistDict>().FirstOrDefault(v =>
                v.TryGetString("Name", out string? value) &&
                string.Equals(value, playlistName, StringComparison.InvariantCultureIgnoreCase)) is not { } playlist)
                return null;

            if (!playlist.TryGetArray("Playlist Items", out var items))
                return null;

            var itemKeys = items.OfType<PlistDict>()
                .Select(d => d.TryGetInteger("Track ID", out long? value) ? value : null).ToList();

            var pl = new PlaylistInfo {Name = (string)(playlist["Name"] as PlistString)!, Songs = new List<SongInfo>()};
            foreach (long? x in itemKeys)
            {
                if (x is not { } v || !s_tracks.TryGetValue(v.ToString(), out var t) || t is not PlistDict track)
                    continue;
                if (track.TryGetString("Location", out string? location))
                    pl.Songs.Add(SongInfo.Extract(DecodePath(TrimNetpathStart(location))));
                else
                {
                    var songData = new SongInfo
                    {
                        Name = track.TryGetString("Name", out string? name) ? name : $"??? (ID {v})",
                        Album = track.TryGetString("Album", out string? album) ? album : null,
                        Artist = track.TryGetString("Artist", out string? artist) ? artist :
                            track.TryGetString("Album Artist", out string? albumArtist) ? albumArtist : null
                    };
                    pl.Songs.Add(songData);
                }
            }

            return pl;
        }

        private static readonly Regex s_pathRegex = new(@"(%[A-Za-z\d]{2})+");

        private static string DecodePath(string path) =>
            s_pathRegex.Replace(path, match => HttpUtility.UrlDecode(match.Value));

        private static string TrimNetpathStart(string path)
        {
            const string lhs = "file://localhost/";
            return path.StartsWith(lhs) ? path[lhs.Length..] : path;
        }
    }
}

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

        public override IEnumerable<SongInfo> GetSongs() => s_tracks.Values.OfType<PlistDict>().Select(GetSongInfo);

        public override IEnumerable<PlaylistInfo> GetPlaylists()
        {
            throw new NotImplementedException();
        }

        public override PlaylistInfo? GetPlaylist(string playlistName)
        {
            foreach (var pp in s_playlists.OfType<PlistDict>())
            {
                if (!pp.TryGetString("Name", out string? name)) continue;
                if (!string.Equals(name, playlistName, StringComparison.InvariantCultureIgnoreCase)) continue;
                return GetPlaylistInfo(name, pp, s_tracks);
            }

            return null;
        }

        private static PlaylistInfo GetPlaylistInfo(string name, PlistDict playlist,
            Dictionary<string, PlistValue> tracks)
        {
            if (!playlist.TryGetArray("Playlist Items", out var items))
                throw new PlaylistLoadException("Missing Playlist Items data", name);

            var itemKeys = items.OfType<PlistDict>()
                .Select(d => d.TryGetInteger("Track ID", out long? value) ? value : null).ToList();

            var pl = new PlaylistInfo {Name = name, Songs = new List<SongInfo>()};
            foreach (long? x in itemKeys)
                if (x is { } v && tracks.TryGetValue(v.ToString(), out var t) && t is PlistDict track)
                    pl.Songs.Add(GetSongInfo(track));
            return pl;
        }

        private static SongInfo GetSongInfo(PlistDict songValueDict)
        {
            if (songValueDict.TryGetString("Location", out string? location))
                return SongInfo.Extract(DecodePath(TrimNetpathStart(location)));
            return new SongInfo
            {
                Name = songValueDict.TryGetString("Name", out string? name) ? name : "???",
                Album = songValueDict.TryGetString("Album", out string? album) ? album : null,
                Artist = songValueDict.TryGetString("Artist", out string? artist) ? artist :
                    songValueDict.TryGetString("Album Artist", out string? albumArtist) ? albumArtist : null
            };
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

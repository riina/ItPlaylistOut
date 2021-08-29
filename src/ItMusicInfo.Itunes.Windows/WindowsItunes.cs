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
    public static class WindowsItunes
    {
        private const string LibraryFile = "iTunes Music Library.xml";

        public static async Task<PlaylistData?> GetPlaylistAsync(string libraryPath, string playlistName,
            bool readJacketSha1, IProgress<int>? progress, CancellationToken cancellationToken)
        {
            XElement? xml;
            await using (var pfs = File.OpenRead(Path.Combine(libraryPath, LibraryFile)))
                xml = await XElement.LoadAsync(pfs, LoadOptions.None, cancellationToken);
            var rootDict = new PlistDict(xml.Elements("dict").First());

            if (!rootDict.TryGetDictionary("Tracks", out var tracks))
                throw new PlaylistNotFoundException("Failed to get Tracks section in src file", playlistName);

            if (!rootDict.TryGetArray("Playlists", out var playlists))
                throw new PlaylistNotFoundException("Failed to get Playlists section in src file", playlistName);

            if (playlists.OfType<PlistDict>().FirstOrDefault(v =>
                v.TryGetString("Name", out string? value) &&
                string.Equals(value, playlistName, StringComparison.InvariantCultureIgnoreCase)) is not { } playlist)
                throw new PlaylistNotFoundException($"Failed to find playlist named {playlistName} in src file",
                    playlistName);

            if (!playlist.TryGetArray("Playlist Items", out var items))
                throw new PlaylistNotFoundException("Failed to find playlist content data in src file", playlistName);

            var itemKeys = items.OfType<PlistDict>()
                .Select(d => d.TryGetInteger("Track ID", out long? value) ? value : null).ToList();

            var pl = new PlaylistData {Name = (string)(playlist["Name"] as PlistString)!, Songs = new List<SongData>()};
            int i = 0;
            foreach (long? x in itemKeys)
            {
                if (x is not { } v || !tracks.TryGetValue(v.ToString(), out var t) || t is not PlistDict track)
                    continue;
                if (track.TryGetString("Location", out string? location))
                    pl.Songs.Add(SongData.Extract(DecodePath(TrimNetpathStart(location)), readJacketSha1));
                else
                {
                    var songData = new SongData
                    {
                        Name = track.TryGetString("Name", out string? name) ? name : $"??? (ID {v})",
                        Album = track.TryGetString("Album", out string? album) ? album : null,
                        Artist = track.TryGetString("Artist", out string? artist) ? artist :
                            track.TryGetString("Album Artist", out string? albumArtist) ? albumArtist : null
                    };
                    pl.Songs.Add(songData);
                }

                progress?.Report(++i * 100 / itemKeys.Count);
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

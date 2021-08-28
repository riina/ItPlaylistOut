using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using SimplePlistXmlRead;

namespace ItMusicInfo.Itunes.Windows
{
    public static class WindowsItunes
    {
        private const string LibraryFile = "iTunes Music Library.xml";

        public static PlaylistData? GetPlaylist(string libraryPath, string playlistName)
        {
            var xml = XElement.Load(Path.Combine(libraryPath, LibraryFile));
            var rootDict = new PlistDict(xml.Elements("dict").First());

            if (!rootDict.TryGetDictionary("Tracks", out var tracks))
            {
                //Console.WriteLine("Failed to get Tracks section in src file");
                return null;
            }

            if (!rootDict.TryGetArray("Playlists", out var playlists))
            {
                //Console.WriteLine("Failed to get Playlists section in src file");
                return null;
            }

            if (playlists.OfType<PlistDict>().FirstOrDefault(v =>
                v.TryGetString("Name", out string? value) &&
                string.Equals(value, playlistName, StringComparison.InvariantCultureIgnoreCase)) is not { } playlist)
            {
                //Console.WriteLine($"Failed to find playlist named {playlistName} in src file");
                return null;
            }

            if (!playlist.TryGetArray("Playlist Items", out var items))
            {
                //Console.WriteLine("Failed to find playlist content data in src file");
                return null;
            }

            var itemKeys = items.OfType<PlistDict>()
                .Select(d => d.TryGetInteger("Track ID", out long? value) ? value : null).ToList();

            var pl = new PlaylistData {Name = (string)(playlist["Name"] as PlistString)!, Songs = new List<SongData>()};
            //int i = 0;
            foreach (long? x in itemKeys)
            {
                //Console.WriteLine($"{++i}/{itemKeys.Count}...");
                if (x is not { } v) continue;

                if (tracks.TryGetValue(v.ToString(), out var t) && t is PlistDict track)
                {
                    // If file's found, work purely off file
                    if (track.TryGetString("Location", out string? location))
                        pl.Songs.Add(SongData.Extract(DecodePath(TrimNetpathStart(location))));
                    else
                    {
                        var songData = new SongData();
                        if (!track.TryGetString("Name", out string? name))
                        {
                            //Console.WriteLine($"Warning: Missing name on track {v}");
                            name = $"??? (ID {v})";
                        }

                        songData.Name = name;
                        //Console.WriteLine($"Warning: Missing location on track \"{name}\"");
                        if (track.TryGetString("Album", out string? album))
                            songData.Album = album;

                        string? artist;
                        if (track.TryGetString("Artist", out artist))
                            goto artistDone;
                        if (track.TryGetString("Album Artist", out artist))
                            goto artistDone;
                        artist = null;
                        artistDone:
                        songData.Artist = artist;
                        pl.Songs.Add(songData);
                    }
                }
            }

            return pl;
        }

        private static readonly Regex _pathRegex = new(@"(%[A-Za-z\d]{2})+");

        private static string DecodePath(string path) =>
            _pathRegex.Replace(path, match => HttpUtility.UrlDecode(match.Value));

        private static string TrimNetpathStart(string path)
        {
            const string lhs = "file://localhost/";
            return path.StartsWith(lhs) ? path[lhs.Length..] : path;
        }
    }
}

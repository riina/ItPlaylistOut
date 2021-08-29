using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ItMusicInfo
{
    public static class MusicUtility
    {
        public static async Task LoadJacketsAsync(this IEnumerable<SongInfo> songs, string? jacketFolder,
            bool losslessJackets, CancellationToken cancellationToken)
        {
            Dictionary<string, JacketInfo> knownMainHashes = new();
            HashSet<string> knownSubHashes = new();
            foreach (var song in songs)
                if (song.GetJacketInfo(knownMainHashes) is { } jacket)
                    if (jacketFolder != null && knownSubHashes.Add(jacket.Sha1))
                        await jacket.WriteImageAsync(jacketFolder, losslessJackets, cancellationToken);
        }
    }
}

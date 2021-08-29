using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ItMusicInfo
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class PlaylistInfo : IList<SongInfo>
    {
        public string Name { get; set; } = null!;
        public List<SongInfo> Songs { get; set; } = null!;
        public IEnumerator<SongInfo> GetEnumerator() => Songs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(SongInfo item) => Songs.Add(item);

        public void Clear() => Songs.Clear();

        public bool Contains(SongInfo item) => Songs.Contains(item);

        public void CopyTo(SongInfo[] array, int arrayIndex) => Songs.CopyTo(array, arrayIndex);

        public bool Remove(SongInfo item) => Songs.Remove(item);

        public int Count => Songs.Count;
        public bool IsReadOnly => false;
        public int IndexOf(SongInfo item) => Songs.IndexOf(item);

        public void Insert(int index, SongInfo item) => Songs.Insert(index, item);

        public void RemoveAt(int index) => Songs.RemoveAt(index);

        public SongInfo this[int index]
        {
            get => Songs[index];
            set => Songs[index] = value;
        }
    }
}

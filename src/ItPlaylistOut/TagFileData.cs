using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TagLib.Mpeg4;

namespace ItPlaylistOut
{
    public record TagFileData(TagLib.File File) : IEnumerable<TagData>
    {
        public IEnumerator<TagData> GetEnumerator()
        {
            BoxHeader2 current;
            long begin = 0, end = File.Length;
            for (long position = begin; position < end; position += current.Header.TotalBoxSize)
            {
                current = new BoxHeader2(new BoxHeader(File, position));
                if (current.Header.TotalBoxSize == 0) yield break;
                yield return new TagData(File, current);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool GetAtPath(string path, [NotNullWhen(true)] out TagData? tag) => GetAtPath(path.Split('/'), out tag);

        public bool GetAtPath(IReadOnlyList<string> splits, [NotNullWhen(true)] out TagData? tag)
        {
            if (splits.Count != 0)
            {
                TagData? next = this.FirstOrDefault(v => v.Header.Header.BoxType == splits[0]);
                if (next == null) goto fail;
                return next.GetAtPath(new List<string>(splits.Skip(1)), out tag);
            }

            fail:
            tag = null;
            return false;
        }
    }
}

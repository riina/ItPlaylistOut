using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TagLib.Mpeg4;

namespace ItPlaylistOut
{
    public record TagData(TagLib.File File, BoxHeader2 Header) : IEnumerable<TagData>
    {
        public IEnumerator<TagData> GetEnumerator()
        {
            BoxHeader2 current;
            (long begin, long end) = Header.GetOffsets();
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
            var next = this;
            foreach (var segment in splits)
            {
                next = next.FirstOrDefault(v => v.Header.Header.BoxType == segment);
                if (next == null)
                {
                    tag = null;
                    return false;
                }
            }

            tag = next;
            return true;
        }
    }
}

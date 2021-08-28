using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SimplePlistXmlRead
{
    public record PlistArray(XElement Value) : PlistValue, IReadOnlyList<PlistValue>
    {
        public IEnumerator<PlistValue> GetEnumerator() => Value.Elements().Select(GetValue).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => Value.Elements().Count();

        public PlistValue this[int index] => GetValue(Value.Elements().Skip(index).First());
        public static PlistArray FromElement(XElement element) => new(element);

        public override string ToDisplayString() => "<array>";
    }
}

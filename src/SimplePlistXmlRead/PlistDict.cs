using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SimplePlistXmlRead
{
    public record PlistDict(XElement Value) : PlistValue, IReadOnlyDictionary<string, PlistValue>
    {
        public bool ContainsKey(string key) => GetKeyElements().Any(k => (string)k == key);

        public bool TryGetValue(string key, [NotNullWhen(true)] out PlistValue? value)
        {
            if (GetLazyValueEnumerable().FirstOrDefault(k => k.Key == key) is not {Key: { }} element)
            {
                value = default;
                return false;
            }

            value = GetValue(element.Value);
            return true;
        }

        private static string GetKeyNameFromKeyElement(XElement element) =>
            (string)element;

        private IEnumerable<XElement> GetKeyElements() => Value.Elements().Where((_, i) => i % 2 == 0);
        private IEnumerable<XElement> GetValueElements() => Value.Elements().Where((_, i) => i % 2 == 1);

        public PlistValue this[string key] =>
            TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

        public IEnumerable<string> Keys => GetKeyElements().Select(GetKeyNameFromKeyElement);
        public IEnumerable<PlistValue> Values => GetValueElements().Select(GetValue);

        public IEnumerator<KeyValuePair<string, PlistValue>> GetEnumerator()
        {
            using var enumerator = Value.Elements().GetEnumerator();
            while (enumerator.MoveNext())
            {
                var k = enumerator.Current;
                if (!enumerator.MoveNext()) throw new InvalidDataException();
                var v = enumerator.Current;
                yield return new KeyValuePair<string, PlistValue>(GetKeyNameFromKeyElement(k), GetValue(v));
            }
        }

        private IEnumerable<KeyValuePair<string, XElement>> GetLazyValueEnumerable()
        {
            using var enumerator = Value.Elements().GetEnumerator();
            while (enumerator.MoveNext())
            {
                var k = enumerator.Current;
                if (!enumerator.MoveNext()) throw new InvalidDataException();
                var v = enumerator.Current;
                yield return new KeyValuePair<string, XElement>(GetKeyNameFromKeyElement(k), v);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => GetKeyElements().Count();

        public static PlistDict FromElement(XElement element) => new(element);

        public override string ToDisplayString() => "<dict>";
    }
}

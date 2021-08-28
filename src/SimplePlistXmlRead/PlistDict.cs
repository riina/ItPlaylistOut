using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;

namespace SimplePlistXmlRead
{
    public record PlistDict(XElement Value) : PlistValue, IReadOnlyDictionary<string, PlistValue>
    {
        public bool ContainsKey(string key) => GetKeyElements().Any(k => (string)k == key);

        public bool TryGetValue(string key, [NotNullWhen(true)] out PlistValue? value)
        {
            if (GetKeyElements().FirstOrDefault(k => (string)k == key) is not { } element)
            {
                value = default;
                return false;
            }

            value = GetValueFromKeyElement(element);
            return true;
        }

        private static string GetKeyNameFromKeyElement(XElement element) =>
            (string)element;

        private static PlistValue GetValueFromKeyElement(XElement element) =>
            GetValue(GetValueElementFromKeyElement(element));

        private static XElement GetValueElementFromKeyElement(XElement element) =>
            element.ElementsAfterSelf().First();

        private IEnumerable<XElement> GetKeyElements() =>
            Value.Elements("key").Where(k => k.ElementsAfterSelf().Any());

        public PlistValue this[string key] =>
            TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

        public IEnumerable<string> Keys =>
            GetKeyElements().Select(k => k.Name.LocalName);

        public IEnumerable<PlistValue> Values =>
            GetKeyElements().Select(GetValueFromKeyElement);

        public IEnumerator<KeyValuePair<string, PlistValue>> GetEnumerator()
        {
            foreach (var key in GetKeyElements())
                yield return new KeyValuePair<string, PlistValue>(
                    GetKeyNameFromKeyElement(key),
                    GetValueFromKeyElement(key));
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => GetKeyElements().Count();

        public static PlistDict FromElement(XElement element) => new(element);

        public override string ToDisplayString() => "<dict>";
    }
}

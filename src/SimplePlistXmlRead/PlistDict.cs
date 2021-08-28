using System;
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

        public bool TryGetTypedValue<T>(string key, [NotNullWhen(true)] out T? value) where T : PlistValue
        {
            if (TryGetValue(key, out var v) && v is T vv)
            {
                value = vv;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetArray(string key, [NotNullWhen(true)] out PlistValue[]? value)
        {
            if (TryGetTypedValue<PlistArray>(key, out var v))
            {
                value = v.ToArray();
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetDictionary(string key, [NotNullWhen(true)] out Dictionary<string, PlistValue>? value)
        {
            if (TryGetTypedValue<PlistDict>(key, out var v))
            {
                value = new Dictionary<string, PlistValue>(v);
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetInteger(string key, [NotNullWhen(true)] out long? value)
        {
            if (TryGetTypedValue<PlistInteger>(key, out var v))
            {
                value = v.Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetString(string key, [NotNullWhen(true)] out string? value)
        {
            if (TryGetTypedValue<PlistString>(key, out var v))
            {
                value = v.Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetDateTime(string key, [NotNullWhen(true)] out DateTime? value)
        {
            if (TryGetTypedValue<PlistDate>(key, out var v))
            {
                value = v.Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetBool(string key, [NotNullWhen(true)] out bool? value)
        {
            if (TryGetTypedValue<PlistBool>(key, out var v))
            {
                value = v.Value;
                return true;
            }

            value = null;
            return false;
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

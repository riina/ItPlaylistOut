using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SimplePlistXmlRead
{
    public static class Extensions
    {
        public static bool TryGetTypedValue<T>(this IReadOnlyDictionary<string, PlistValue> dict, string key,
            [NotNullWhen(true)] out T? value) where T : PlistValue
        {
            if (dict.TryGetValue(key, out var v) && v is T vv)
            {
                value = vv;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetArray(this IReadOnlyDictionary<string, PlistValue> dict, string key,
            [NotNullWhen(true)] out PlistValue[]? value)
        {
            if (dict.TryGetTypedValue<PlistArray>(key, out var v))
            {
                value = v.ToArray();
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetDictionary(this IReadOnlyDictionary<string, PlistValue> dict, string key,
            [NotNullWhen(true)] out Dictionary<string, PlistValue>? value)
        {
            if (dict.TryGetTypedValue<PlistDict>(key, out var v))
            {
                value = new Dictionary<string, PlistValue>(v);
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetInteger(this IReadOnlyDictionary<string, PlistValue> dict, string key,
            [NotNullWhen(true)] out long? value)
        {
            if (dict.TryGetTypedValue<PlistInteger>(key, out var v))
            {
                value = v.Value;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetString(this IReadOnlyDictionary<string, PlistValue> dict, string key,
            [NotNullWhen(true)] out string? value)
        {
            if (dict.TryGetTypedValue<PlistString>(key, out var v))
            {
                value = v.Value;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetDateTime(this IReadOnlyDictionary<string, PlistValue> dict, string key,
            [NotNullWhen(true)] out DateTime? value)
        {
            if (dict.TryGetTypedValue<PlistDate>(key, out var v))
            {
                value = v.Value;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetBool(this IReadOnlyDictionary<string, PlistValue> dict, string key,
            [NotNullWhen(true)] out bool? value)
        {
            if (dict.TryGetTypedValue<PlistBool>(key, out var v))
            {
                value = v.Value;
                return true;
            }

            value = null;
            return false;
        }
    }
}

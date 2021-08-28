using System.Xml.Linq;

namespace SimplePlistXmlRead
{
    public abstract record PlistValue
    {
        public static PlistValue GetValue(XElement element) => element.Name.LocalName switch
        {
            "array" => PlistArray.FromElement(element),
            "dict" => PlistDict.FromElement(element),
            "integer" => PlistInteger.FromElement(element),
            "string" => PlistString.FromElement(element),
            "date" => PlistDate.FromElement(element),
            "true" => new PlistBool(true),
            "false" => new PlistBool(false),
            _ => new PlistUnsupportedValue(element.Name.LocalName)
        };

        public abstract string ToDisplayString();
    }
}

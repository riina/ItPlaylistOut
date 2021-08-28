using System.Xml.Linq;

namespace SimplePlistXmlRead
{
    public record PlistString(string Value) : PlistValue
    {
        public static PlistString FromElement(XElement element) => new((string)element);

        public override string ToDisplayString() => Value;
    }
}

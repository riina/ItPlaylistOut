using System.Xml.Linq;

namespace SimplePlistXmlRead
{
    public record PlistInteger(long Value) : PlistValue
    {
        public static PlistInteger FromElement(XElement element) => new((long)element);

        public override string ToDisplayString() => Value.ToString();

        public static explicit operator long(PlistInteger value) => value.Value;
    }
}

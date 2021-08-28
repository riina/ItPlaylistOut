using System;
using System.Globalization;
using System.Xml.Linq;

namespace SimplePlistXmlRead
{
    public record PlistDate(DateTime Value) : PlistValue
    {
        public static PlistDate FromElement(XElement element) => new((DateTime)element);

        public override string ToDisplayString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}

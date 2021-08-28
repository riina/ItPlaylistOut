namespace SimplePlistXmlRead
{
    public record PlistBool(bool Value) : PlistValue
    {
        public override string ToDisplayString() => Value.ToString();

        public static explicit operator bool(PlistBool value) => value.Value;
    }
}

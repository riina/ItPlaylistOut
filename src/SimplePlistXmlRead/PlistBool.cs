namespace SimplePlistXmlRead
{
    public record PlistBool(bool Value) : PlistValue
    {
        public override string ToDisplayString() => Value.ToString();
    }
}

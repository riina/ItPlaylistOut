namespace SimplePlistXmlRead
{
    public record PlistUnsupportedValue(string Key) : PlistValue
    {
        public override string ToDisplayString() => $"<unsupported ({Key})>";
    }
}

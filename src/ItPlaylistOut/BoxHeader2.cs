using TagLib.Mpeg4;

namespace ItPlaylistOut
{
    public struct BoxHeader2
    {
        public BoxHeader Header;
        public BoxHeader2(BoxHeader header) => Header = header;

        public long GetContentPosition(long offset = 0) => Header.Position + Header.HeaderSize + offset;
        public long GetContentPositionFromEnd(long offset = 0) => Header.Position + Header.TotalBoxSize + offset;

        public (long InitialOffset, long End) GetOffsets() =>
            (Header.BoxType.ToString() switch
            {
                "stsd" => 8,
                "mp4a" => 28,
                "meta" => 4,
                _ => 0
            } + Header.Position + 8, Header.Position + Header.TotalBoxSize);
    }
}

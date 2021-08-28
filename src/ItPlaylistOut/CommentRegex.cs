using System;
using System.Text.RegularExpressions;

namespace ItPlaylistOut
{
    internal static class CommentRegex
    {
        public static readonly (Regex regex, string provider, Func<string, string?> transform)[] Regexes =
        {
            (new Regex(@"https?://ototoy\.jp[^\s]*"), "OTOTOY", t => t),
            (new Regex(@"https?://[^\s\.]+\.bandcamp\.com[^\s]*"), "Bandcamp", t => t),
            (new Regex(@"^\s*CDRIP\s*$"), "CD Rip", _ => null)
        };
    }
}

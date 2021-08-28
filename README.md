# ItPlaylistOut
 
Reads a playlist's data from iTunes XML and outputs basic JSON data

Also outputs album art (named by original raster buffer sha1)

## Provider detection support

Rudimentary sound provider detection based on comment or iTunes metadata when available

* iTunes
  - Use plID/cnID tags present on purchased tracks
  - Link to song
* Bandcamp
  - Comment applied by default to purchased tracks
  - Link to artist
* OTOTOY
  - Comment applied by default to purchased tracks
  - Link to album
* CDRIP
  - Tracks with comment "CDRIP" are marked as "CD Rip"
  - No linkable source

## Usage

```
Export playlist:
  ItPlaylistOut <libraryPath> <playlist> <outFile>

  -j path, --jacketFolder=path    Output directory for jackets.

  -l, --losslessJacket            Output lossless jackets.

  --help                          Display this help screen.

  --version                       Display version information.

  libraryPath (pos. 0) path       Required. Path to iTunes library.

  playlist (pos. 1) name          Required. Playlist to extract.

  outFile (pos. 2) path           Required. Output file.
```

## Layout

* Playlist
```
string Name
Song[] Songs
```

* Song
```
string Name
string? Album
string? Artist
string? Provider
string? Link
string? JacketSha1
string? Copyright
```

## Example Output

```json
{
  "Name": "Playlist Name",
  "Songs": [
    {
      "Name": "\u65CB\u98A8 -tsumujikaze-",
      "Album": "Iroha",
      "Artist": "Street",
      "Provider": "CD Rip",
      "JacketSha1": "F2C2C411529ABE90D0946DC5C372C44A57E9E706"
    },
    {
      "Name": "\u7EB5\u6B65 -z\u00F2ngb\u00F9-",
      "Album": "\u6D77\u91CC",
      "Artist": "Street",
      "Provider": "Bandcamp",
      "Link": "http://streetofficial.bandcamp.com",
      "JacketSha1": "A51072349B1E811D35BACA86B81A47CF13A3DBF3"
    },
    {
      "Name": "Sakura Fubuki",
      "Album": "Sakura Fubuki - Single",
      "Artist": "Street",
      "Provider": "Apple Music",
      "Link": "https://music.apple.com/us/album/1453204309?i=1453204310",
      "JacketSha1": "2220C26D02FE253AC5678C3DA2380A29DC14423C",
      "Copyright": "\u2117 2019 Street"
    },
    {
      "Name": "Utopia",
      "Album": "\u3042\u30FC\u3086\u30FC\u3086\u30FC\u3051\u30FC!?(R.U.U.K!?)",
      "Artist": "Street",
      "Provider": "OTOTOY",
      "Link": "http://ototoy.jp/_/default/p/52993",
      "JacketSha1": "1F6AFF4E5801B8BDFC7253B2078D6DAF2D54E164"
    }
  ]
}
```
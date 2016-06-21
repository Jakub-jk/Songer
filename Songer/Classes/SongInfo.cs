using System;
using System.IO;
using System.Windows.Media.Imaging;
using UtilLib;

namespace Songer.Classes
{
    public class SongInfo : IEquatable<SongInfo>
    {
        public string title;
        public string artist;
        public string album;
        public string path;
        public TimeSpan length;
        public BitmapImage albumArt;

        public SongInfo(string path)
        {
            TagLib.File f = TagLib.File.Create(path);
            title = f.Tag.Title;
            artist = (f.Tag.AlbumArtists.Length > 0) ? f.Tag.AlbumArtists.MergeWith("; ") : "Unknown";
            album = (f.Tag.Album.IsNullOrEmpty()) ? f.Tag.Album : "Unknown";
            if (f.Tag.Pictures.Length > 0)
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(f.Tag.Pictures[0].Data.Data);
                bi.EndInit();
                albumArt = bi;
            }
            length = PlayerEngine.Instance.BytesToTimeSpan(f.Length);
            this.path = path;
        }

        public bool Equals(SongInfo other)
        {
            return (title == other.title && artist == other.artist && album == other.album && length == other.length && albumArt == other.albumArt);
        }
    }
}
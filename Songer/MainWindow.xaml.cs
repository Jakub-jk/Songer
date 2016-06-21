using MahApps.Metro.Controls;
using System.Windows;
using Songer.Classes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Songer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        PlayerEngine player = PlayerEngine.Instance;
        public MainWindow()
        {
            InitializeComponent();
            player.Initialize();
            spectrum.RegisterSoundPlayer(PlayerEngine.Instance);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Ookii.Dialogs.Wpf.VistaOpenFileDialog o = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            o.ShowDialog();
            SongInfo s = new SongInfo(o.FileName);
            title.Content = s.title;
            artist.Content = s.artist;
            album.Content = s.album;
            //albumArt.Fill = new ImageBrush() { ImageSource = s.albumArt };
            MessageBox.Show(PlayerEngine.Instance.OpenFile(s.path).ToString());
            PlayerEngine.Instance.Play();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (player.IsPlaying)
            {
                player.Pause();
                //ResourceDictionary rd = new ResourceDictionary();
                //rd = this.Resources.MergedDictionaries[0];
                //rd.Source = new System.Uri("\\Resources\\Icons.xaml");
                VisualBrush vb = new VisualBrush();
                vb.Visual = (Canvas)Resources["appbar_controls_play"];
                vb.Stretch = Stretch.Uniform;
                (btnPlay.Content as Rectangle).Fill = vb;
            }
            else
            {
                player.Play();
                //ResourceDictionary rd = new ResourceDictionary();
                //rd.Source = new System.Uri("pack://application:,,,/MahApps.Metro.Resources;component/Icons.xaml");
                VisualBrush vb = new VisualBrush();
                vb.Visual = (Canvas)Resources["appbar_controls_pause"];
                vb.Stretch = Stretch.Uniform;
                (btnPlay.Content as Rectangle).Fill = vb;
            }
        }
    }
}
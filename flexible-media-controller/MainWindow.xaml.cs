using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.UI.Xaml.Media.Imaging;
using BitmapImage = System.Windows.Media.Imaging.BitmapImage;

namespace flexible_media_controller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GlobalSystemMediaTransportControlsSession gsmtcsm;
        private KeyboardCapture keyboardCapture;
        public MainWindow()
        {
            keyboardCapture = new KeyboardCapture();
            gsmtcsm = MediaController.Init();
            InitializeComponent();
            getMediaPropetries();
            gsmtcsm.MediaPropertiesChanged += updateMediaProperties;
            gsmtcsm.PlaybackInfoChanged += updatePlaybackInfo;
            keyboardCapture.AddCombination(new SortedSet<Windows.System.VirtualKey>{
                                           Windows.System.VirtualKey.NumberPad5},
                                           MediaController.TogglePlayPause);
        }
        private async Task getMediaPropetries()
        {
            var mediaProperties = await gsmtcsm.TryGetMediaPropertiesAsync();
            Dispatcher.Invoke(  async () =>
            {
                titleTB.Text = mediaProperties.Title;
                subtitleTB.Text = mediaProperties.Subtitle;
                artistTB.Text = mediaProperties.Artist;
                genresTB.Text = string.Join(", ", mediaProperties.Genres);
                albumTitleTB.Text = mediaProperties.AlbumTitle;
                if (mediaProperties.TrackNumber > 0)
                    trackTB.Text = mediaProperties.TrackNumber.ToString();
                albumArtistTB.Text = mediaProperties.AlbumArtist;
                if (mediaProperties.AlbumTrackCount > 0)
                    trackCountTB.Text = mediaProperties.AlbumTrackCount.ToString();
                if (mediaProperties.Thumbnail == null) return;
                using (var raStream = await mediaProperties.Thumbnail.OpenReadAsync())
                using (var stream = raStream.AsStream())
                {
                     BitmapImage bitmap = new BitmapImage();
                     bitmap.BeginInit();
                     bitmap.StreamSource = stream;
                     bitmap.EndInit();
                     thumbnailImg.Source = bitmap;
                }
            });
        }

        private void updateMediaProperties(object sender, MediaPropertiesChangedEventArgs e)
        {
            getMediaPropetries();
        }
        private void updatePlaybackInfo(object sender, PlaybackInfoChangedEventArgs e)
        {
            var info = gsmtcsm.GetPlaybackInfo();
            Dispatcher.Invoke( () =>
            {
                prevBtn.IsEnabled = info.Controls.IsPreviousEnabled;
                playToggleBtn.IsEnabled = info.Controls.IsPlayPauseToggleEnabled;
                nextBtn.IsEnabled = info.Controls.IsNextEnabled;
                switch (info.PlaybackStatus)
                {
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing:
                        playToggleBtn.Content = "Pause";
                        break;
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused:
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped:
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed:
                        playToggleBtn.Content = "Play";
                        break;
                }
            });
        }
        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            MediaController.Next();
        }
        private void playToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            MediaController.TogglePlayPause();
        }

        private void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            MediaController.Previous();
        }
    }
}

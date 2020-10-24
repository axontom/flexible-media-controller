using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Control;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Media.Capture.Frames;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Printing3D;

namespace flexible_media_controller
{
    public sealed partial class MainPage : Page
    {
        GlobalSystemMediaTransportControlsSession gsmtcsm;
        public MainPage()
        {
            this.InitializeComponent();
            gsmtcsm = GlobalSystemMediaTransportControlsSessionManager
                .RequestAsync().GetAwaiter().GetResult().GetCurrentSession();
            getMediaPropetries();
            gsmtcsm.MediaPropertiesChanged += updateMediaProperties;
            gsmtcsm.PlaybackInfoChanged += updatePlaybackInfo;
        }

        private async Task getMediaPropetries()
        {
            var mediaProperties = await gsmtcsm.TryGetMediaPropertiesAsync();
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow
               .Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
           async () =>
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
               using (var stream = await mediaProperties.Thumbnail.OpenReadAsync())
               {
                   try
                   {
                       var decoder = await BitmapDecoder.CreateAsync(stream);
                       WriteableBitmap bitmap = new WriteableBitmap(
                           Convert.ToInt32(decoder.PixelWidth),
                           Convert.ToInt32(decoder.PixelHeight));
                       BitmapTransform transform = new BitmapTransform()
                       {
                           ScaledWidth = Convert.ToUInt32(decoder.PixelWidth),
                           ScaledHeight = Convert.ToUInt32(decoder.PixelHeight)
                       };
                       PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                           BitmapPixelFormat.Bgra8,
                           BitmapAlphaMode.Straight,
                           transform,
                           ExifOrientationMode.IgnoreExifOrientation,
                           ColorManagementMode.DoNotColorManage
                       );
                       byte[] sourcePixels = pixelData.DetachPixelData();

                       using (Stream toimg = bitmap.PixelBuffer.AsStream())
                       {
                           await toimg.WriteAsync(sourcePixels, 0, sourcePixels.Length);
                       }
                       thumbnailImg.Source = bitmap;
                   }
                   catch (Exception ex)
                   {
                       var image = new BitmapImage();
                       image.UriSource = new Uri(thumbnailImg.BaseUri,
                           "Assets/Square150x150Logo.scale-200.png");
                       thumbnailImg.Source = image;
                   }
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
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow
                .Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            () =>
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
            gsmtcsm.TrySkipNextAsync();
        }
        private void playToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            gsmtcsm.TryTogglePlayPauseAsync();
        }

        private void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            gsmtcsm.TrySkipPreviousAsync();
        }
    }
}

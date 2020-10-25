using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
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
using Windows.System;
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
        private BindingList<HotkeyCombination> hotkeys;
        private List<HotkeyCombination> savedHotkeys;
        public MainWindow()
        {
            hotkeys = new BindingList<HotkeyCombination>(
                EmptyHotkeyCombinationList());
            savedHotkeys = EmptyHotkeyCombinationList();
            keyboardCapture = new KeyboardCapture();
            InitializeComponent();
            HotKeyItemsControl.ItemsSource = hotkeys;
            ReloadSMTCSession();
        }
        private List<HotkeyCombination> EmptyHotkeyCombinationList()
        {
            return new List<HotkeyCombination>
            {
                new HotkeyCombination(MediaController.Play, 0, "Play"),
                new HotkeyCombination(MediaController.Pause, 1, "Pause"),
                new HotkeyCombination(MediaController.TogglePlayPause, 2,
                                      "Toggle Play/Pause"),
                new HotkeyCombination(MediaController.Stop, 3, "Stop"),
                new HotkeyCombination(MediaController.Next, 4, "Next"),
                new HotkeyCombination(MediaController.Previous, 5, "Previous"),
                new HotkeyCombination(MediaController.Record, 6, "Record"),
                new HotkeyCombination(MediaController.FastForward, 7,
                                      "Fast Forward"),
                new HotkeyCombination(MediaController.Rewind, 8, "Rewind"),
                new HotkeyCombination(MediaController.ChannelUp, 9,
                                      "Channel Up"),
                new HotkeyCombination(MediaController.ChannelDown, 10,
                                      "Channel Down")
            };
        }
        private void ReloadSMTCSession()
        {
            gsmtcsm = MediaController.Init();
            GetMediaPropetries();
            UpdatePlaybackInfo();
            gsmtcsm.MediaPropertiesChanged += UpdateMediaProperties;
            gsmtcsm.PlaybackInfoChanged += UpdatePlaybackInfo;
        }
        private async Task GetMediaPropetries()
        {
            var mediaProperties = await gsmtcsm.TryGetMediaPropertiesAsync();
            Dispatcher.Invoke(async () =>
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

        private void UpdateMediaProperties(object sender,
            MediaPropertiesChangedEventArgs e)
        {
            GetMediaPropetries();
        }
        private void UpdatePlaybackInfo(object sender = null,
            PlaybackInfoChangedEventArgs e = null)
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
        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            MediaController.Next();
        }
        private void PlayToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            MediaController.TogglePlayPause();
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            MediaController.Previous();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            ReloadSMTCSession();
        }

        private void HotkeyResetBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button) == null) return;
            Button button = sender as Button;
            int idx = button.Tag as int? ?? -1;
            if (idx == -1) return;
            hotkeys[idx].Reset();
        }
        private void HotkeyTB_KeyDown(object sender, KeyEventArgs e)
        {
            if ((sender as TextBox) == null) return;
            TextBox textbox = sender as TextBox;
            int idx = textbox.Tag as int? ?? -1;
            if (idx == -1) return;

            if (!hotkeys[idx].Capturing)
            {
                hotkeys[idx].Reset();
                hotkeys[idx].Capturing = true;
            }
            hotkeys[idx].Keys.Add(e.Key);
            hotkeys[idx].UpdateText();
        }

        private void HotkeyTB_GotFocus(object sender, RoutedEventArgs e)
        {
            if ((sender as TextBox) == null) return;
            TextBox textbox = sender as TextBox;
            int idx = textbox.Tag as int? ?? -1;
            if (idx == -1) return;

            if (!hotkeys[idx].Capturing)
            {
                hotkeys[idx].Reset();
                hotkeys[idx].Capturing = true;
            }
        }

        private void HotkeyTB_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((sender as TextBox) == null) return;
            TextBox textbox = sender as TextBox;
            int idx = textbox.Tag as int? ?? -1;
            if (idx == -1) return;

            hotkeys[idx].Capturing = false;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            keyboardCapture.ClearCombinations();
            foreach (var comb in hotkeys)
            {
                if (comb.Keys.Count == 0) continue;
                SortedSet<VirtualKey> vkComb = new SortedSet<VirtualKey>();
                foreach (var key in comb.Keys)
                    vkComb.Add((VirtualKey)KeyInterop.VirtualKeyFromKey(key));
                if (!keyboardCapture.AddCombination(
                    new SortedSet<VirtualKey>(comb.Keys.Select<Key, VirtualKey>(k => (
                        (VirtualKey)KeyInterop.VirtualKeyFromKey(k)))), comb.KeyCapturedProc))
                    comb.Reset();
            }
            savedHotkeys = hotkeys.ToList();
        }

        private void UnbindAllBtn_Click(object sender, RoutedEventArgs e)
        {
            keyboardCapture.ClearCombinations();
            foreach (var comb in hotkeys)
                comb.Reset();
        }

        private void DiscardBtn_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < hotkeys.Count; i++)
                hotkeys[i].Keys = savedHotkeys[i].Keys;
        }
    }
}

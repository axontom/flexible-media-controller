using flexible_media_controller.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using Windows.Media.Control;
using Windows.System;
using BitmapImage = System.Windows.Media.Imaging.BitmapImage;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace flexible_media_controller
{
    static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }
    }
    public struct HotkeySave
    {
        public List<HotkeyCombination> Hotkeys { get; set; }
        public List<List<Key>> Combinations { get; set; }
        public HotkeySave(List<HotkeyCombination> list)
        {
            Hotkeys = list;
            Combinations = new List<List<Key>>(
                list.Select(e => (e.Keys.ToList())));
        }
        public List<HotkeyCombination> ToHotkeyList()
        {
            var list = Hotkeys;
            for (int i = 0; i < list.Count && i < Combinations.Count; i++)
                list[i].Keys = new SortedSet<Key>(Combinations[i]);
            return list;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GlobalSystemMediaTransportControlsSession gsmtcsm;
        private KeyboardCapture keyboardCapture;
        private BindingList<HotkeyCombination> hotkeys;
        private List<HotkeyCombination> savedHotkeys;
        private NotifyIcon notifyIcon;
        private bool runOnStartUp;

        public bool MinimizeToTray
        {
            get => Settings.Default.MinimizeToTray;
            set
            {
                Settings.Default.MinimizeToTray = value;
                Settings.Default.Save();
            }
        }
        public bool RunOnStartUp
        {
            get => runOnStartUp;
            set
            {
                if (App.HasAdminRights == false)
                {
                    MessageBoxResult result = MessageBox.Show(this,
                        "Setting this option requires administrative rights.\n"
                        + "Do you want to restart this application as Administrator?",
                        "Insufficient Privileges", MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                        App.RestartAsAdmin();
                }
                else
                {
                    App.RunOnStartUp = value;
                    runOnStartUp = App.RunOnStartUp;
                }
            }
        }
        public MainWindow()
        {
            runOnStartUp = App.RunOnStartUp;
            CreateNotifyIcon();
            if (!LoadHotkeysFromFile())
                savedHotkeys = EmptyHotkeyCombinationList();
            hotkeys = new BindingList<HotkeyCombination>(savedHotkeys.Clone());
            keyboardCapture = new KeyboardCapture();
            SaveBtn_Click();
            InitializeComponent();
            SaveBtn.IsEnabled = false;
            DiscardBtn.IsEnabled = false;
            HotKeyItemsControl.ItemsSource = hotkeys;
            ReloadSMTCSession();
        }

        public void DisplayWarning(string message)
        {
            warningTBl.Text = message;
            warningBorder.Height = 50;
            warningBorder.Visibility = Visibility.Visible;
        }

        public void HideWarning()
        {
            warningBorder.Height = 0;
            warningBorder.Visibility = Visibility.Hidden;
        }

        private void CreateNotifyIcon()
        {
            var contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.AddRange(
                new System.Windows.Forms.MenuItem[]
                {
                    new System.Windows.Forms.MenuItem() { Index = 0, Text = "Open" },
                    new System.Windows.Forms.MenuItem() { Index = 1, Text = "Minimize to Tray" },
                    new System.Windows.Forms.MenuItem() { Index = 1, Text = "Exit" }
                });
            contextMenu.MenuItems[0].Click += NotifyIconOpen;
            contextMenu.MenuItems[1].Click += NotifyIconMinimize;
            contextMenu.MenuItems[2].Click += NotifyIconExit;
            notifyIcon = new NotifyIcon()
            {
                Icon = new Icon(Application.GetResourceStream(
                    new Uri("fmc.ico", UriKind.Relative)).Stream),
                Visible = true,
                ContextMenu = contextMenu,
                Text = "Flexible Media Controller"
            };
            notifyIcon.DoubleClick += NotifyIconOpen;
        }
        private void NotifyIconExit(object sender, EventArgs args)
        {
            Close();
        }
        private void NotifyIconOpen(object sender, EventArgs args)
        {
            WindowState = WindowState.Normal;
            Visibility = Visibility.Visible;
        }
        private void NotifyIconMinimize(object sender, EventArgs args)
        {
            Hide();
            WindowState = WindowState.Minimized;
        }
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized && MinimizeToTray)
                Hide();
            base.OnStateChanged(e);
        }
        private void SaveHotkeysToFile()
        {
            var serializer = new XmlSerializer(typeof(HotkeySave));
            using (var writer = new XmlTextWriter(App.HotkeysSaveFile, Encoding.UTF8))
                serializer.Serialize(writer, new HotkeySave(savedHotkeys));
        }
        private bool LoadHotkeysFromFile()
        {
            if (!File.Exists(App.HotkeysSaveFile)) return false;

            HotkeySave save;
            var serializer = new XmlSerializer(typeof(HotkeySave));
            using (var reader = new XmlTextReader(App.HotkeysSaveFile))
                save = (HotkeySave)serializer.Deserialize(reader);
            save.Hotkeys = EmptyHotkeyCombinationList();
            savedHotkeys = save.ToHotkeyList();
            return true;
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
                                      "Channel Down"),
                new HotkeyCombination(RepeatModeToggle, 11,
                                      "Toggle Repeat Mode"),
                new HotkeyCombination(ToggleShuffle, 12,
                                      "Toggle Shuffle")
            };
        }
        private void DisablePlaybackButtons()
        {
            playToggleBtn.IsEnabled = false;
            nextBtn.IsEnabled = false;
            prevBtn.IsEnabled = false;
            shuffleBtn.IsEnabled = false;
            repeatModeBtn.IsEnabled = false;
        }
        private void ReloadSMTCSession()
        {
            gsmtcsm = MediaController.Init();
            if (gsmtcsm is null)
            {
                DisplayWarning("No compatible application found.\n"
                                + "Hit Refresh button to try again.");
                DisablePlaybackButtons();
                return;
            }
            HideWarning();
            _ = GetMediaPropetries();
            UpdatePlaybackInfo();
            gsmtcsm.MediaPropertiesChanged += UpdateMediaProperties;
            gsmtcsm.PlaybackInfoChanged += UpdatePlaybackInfo;
        }
        private async Task GetMediaPropetries()
        {
            var mediaProperties = await gsmtcsm.TryGetMediaPropertiesAsync();
            _ = Dispatcher.Invoke(async () =>
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
            _ = GetMediaPropetries();
        }
        private void UpdatePlaybackInfo(object sender = null,
            PlaybackInfoChangedEventArgs e = null)
        {
            var info = gsmtcsm.GetPlaybackInfo();
            Dispatcher.Invoke(() =>
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
                repeatModeBtn.IsEnabled = info.Controls.IsRepeatEnabled;
                if (info.AutoRepeatMode != null)
                    repeatModeBtn.Content = "Repeat: " + Enum.GetName(
                        typeof(Windows.Media.MediaPlaybackAutoRepeatMode),
                        info.AutoRepeatMode);
                shuffleBtn.IsEnabled = info.Controls.IsShuffleEnabled;
                shuffleBtn.Content = "Shuffle: " +
                    (info.IsShuffleActive == true ? "On" : "Off");
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

            keyboardCapture.Enabled = false;
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
            keyboardCapture.Enabled = true;
        }
        private void SaveBtn_Click(object sender = null,
            RoutedEventArgs e = null)
        {
            keyboardCapture.ClearCombinations();
            foreach (var comb in hotkeys)
            {
                if (comb.Keys.Count == 0) continue;
                SortedSet<VirtualKey> vkComb = new SortedSet<VirtualKey>();
                foreach (var key in comb.Keys)
                    vkComb.Add((VirtualKey)KeyInterop.VirtualKeyFromKey(key));
                if (!keyboardCapture.AddCombination(
                        new SortedSet<VirtualKey>(
                            comb.Keys.Select<Key, VirtualKey>(k => (
                                (VirtualKey)KeyInterop.VirtualKeyFromKey(k)))),
                        comb.KeyCapturedProc))
                {
                    comb.Reset();
                }
            }
            savedHotkeys = new List<HotkeyCombination>(hotkeys.ToList().Clone());
            SaveHotkeysToFile();
            if (SaveBtn != null)
                SaveBtn.IsEnabled = false;
            if (DiscardBtn != null)
                DiscardBtn.IsEnabled = false;
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
            SaveBtn.IsEnabled = false;
            DiscardBtn.IsEnabled = false;
        }

        private void RepeatModeBtn_Click(object sender = null,
                                         RoutedEventArgs e = null)
        {
            var info = gsmtcsm.GetPlaybackInfo();
            if (info.AutoRepeatMode is null)
            {
                UpdatePlaybackInfo();
                return;
            }
            MediaController.AutoRepeatMode(
                (Windows.Media.MediaPlaybackAutoRepeatMode)
                (((int)info.AutoRepeatMode + 1) % 3));
        }
        public void RepeatModeToggle()
        {
            RepeatModeBtn_Click();
        }

        private void ShuffleBtn_Click(object sender = null,
                                      RoutedEventArgs e = null)
        {
            var info = gsmtcsm.GetPlaybackInfo();
            if (info.IsShuffleActive is null)
            {
                UpdatePlaybackInfo();
                return;
            }
            MediaController.Shuffle(info.IsShuffleActive == false);
        }
        public void ToggleShuffle()
        {
            ShuffleBtn_Click();
        }

        private void HotkeyTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveBtn.IsEnabled = true;
            DiscardBtn.IsEnabled = true;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            notifyIcon.Dispose();
        }
    }
}

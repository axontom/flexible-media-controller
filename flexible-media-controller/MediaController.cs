using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace flexible_media_controller
{
    public static class MediaController
    {
        public static GlobalSystemMediaTransportControlsSession Session
        {
            get;
            private set;
        }

        public static bool Initialized { get; private set; } = false;
        public static GlobalSystemMediaTransportControlsSession Init()
        {
            Session = GlobalSystemMediaTransportControlsSessionManager
                .RequestAsync().GetAwaiter().GetResult().GetCurrentSession();
            Initialized = true;
            return Session;
        }

        public static void Play()
        {
            if (!Initialized) return;
            Session.TryPlayAsync();
        }
        public static void Pause()
        {
            if (!Initialized) return;
            Session.TryPauseAsync();
        }
        public static void TogglePlayPause()
        {
            if (!Initialized) return;
            Session.TryTogglePlayPauseAsync();
        }
        public static void Stop()
        {
            if (!Initialized) return;
            Session.TryStopAsync();
        }
        public static void Next()
        {
            if (!Initialized) return;
            Session.TrySkipNextAsync();
        }
        public static void Previous()
        {
            if (!Initialized) return;
            Session.TrySkipPreviousAsync();
        }
        public static void Record()
        {
            if (!Initialized) return;
            Session.TryRecordAsync();
        }
        public static void FastForward()
        {
            if (!Initialized) return;
            Session.TryFastForwardAsync();
        }
        public static void Rewind()
        {
            if (!Initialized) return;
            Session.TryRewindAsync();
        }
        public static void AutoRepeatMode(Windows.Media.MediaPlaybackAutoRepeatMode mode)
        {
            if (!Initialized) return;
            Session.TryChangeAutoRepeatModeAsync(mode);
        }
        public static void Shuffle(bool active)
        {
            if (!Initialized) return;
            Session.TryChangeShuffleActiveAsync(active);
        }
        public static void ChannelUp()
        {
            if (!Initialized) return;
            Session.TryChangeChannelUpAsync();
        }
        public static void ChannelDown()
        {
            if (!Initialized) return;
            Session.TryChangeChannelDownAsync();
        }
    }
}

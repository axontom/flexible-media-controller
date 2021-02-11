using System;
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
            _ = Session.TryPlayAsync();
        }
        public static void Pause()
        {
            if (!Initialized) return;
            _ = Session.TryPauseAsync();
        }
        public static void TogglePlayPause()
        {
            if (!Initialized) return;
            _ = Session.TryTogglePlayPauseAsync();
        }
        public static void Stop()
        {
            if (!Initialized) return;
            _ = Session.TryStopAsync();
        }
        public static void Next()
        {
            if (!Initialized) return;
            _ = Session.TrySkipNextAsync();
        }
        public static void Previous()
        {
            if (!Initialized) return;
            _ = Session.TrySkipPreviousAsync();
        }
        public static void Record()
        {
            if (!Initialized) return;
            _ = Session.TryRecordAsync();
        }
        public static void FastForward()
        {
            if (!Initialized) return;
            _ = Session.TryFastForwardAsync();
        }
        public static void Rewind()
        {
            if (!Initialized) return;
            _ = Session.TryRewindAsync();
        }
        public static void AutoRepeatMode(Windows.Media.MediaPlaybackAutoRepeatMode mode)
        {
            if (!Initialized) return;
            _ = Session.TryChangeAutoRepeatModeAsync(mode);
        }
        public static void Shuffle(bool active)
        {
            if (!Initialized) return;
            _ = Session.TryChangeShuffleActiveAsync(active);
        }
        public static void ChannelUp()
        {
            if (!Initialized) return;
            _ = Session.TryChangeChannelUpAsync();
        }
        public static void ChannelDown()
        {
            if (!Initialized) return;
            _ = Session.TryChangeChannelDownAsync();
        }
    }
}

using System;
using System.Diagnostics;
using MMBot;

namespace MMBotiTunes
{
    public class iTunesModule : BotModule
    {
        public iTunesModule() { }

        public override void Shutdown() { }
        /*private iTunesLib.iTunesApp App;

        public bool GetApp(IRC IrcObject, string channel)
        {
            if (Process.GetProcessesByName("itunes").Length == 0)
            {
                IrcObject.WriteMessage("iTunes is not running.", channel);
                return false;
            }
            else
            {
                App = new iTunesLib.iTunesApp();
                return true;
            }
        }

        public void ReleaseApp()
        {
            App = null;
        }

        void ItunesCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!GetApp(IrcObject, channel)) return;
            if (App.PlayerState == iTunesLib.ITPlayerState.ITPlayerStateStopped)
                IrcObject.WriteMessage("iTunes is stopped.", channel);
            else
            {
                iTunesLib.IITTrack track = App.CurrentTrack;
                string msg = "Listening to: " + Module1.UnderChar + track.Name + Module1.UnderChar;
                if (!string.IsNullOrEmpty(track.Artist))
                    msg += " by " + Module1.UnderChar + track.Artist + Module1.UnderChar;
                msg += " from " + Module1.UnderChar + track.Album + Module1.UnderChar;
                msg += " (";
                TimeSpan time = TimeSpan.FromSeconds(App.PlayerPosition);
                msg += time.Minutes + ":" + time.Seconds.ToString("00");
                msg += "/" + track.Time + ")";
                msg += " " + Module1.smartsize((ulong)track.Size) + " " + track.BitRate + "kbps " + Module1.ColorChar + "08" + new string('\u2605', track.Rating / 20) + Module1.ColorChar;
                IrcObject.WriteMessage(msg, channel);
            }
            ReleaseApp();
        }

        void ItunesPlaylistCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!GetApp(IrcObject, channel)) return;
            iTunesLib.IITPlaylist playlist = App.CurrentPlaylist;
            string msg = "Current playlist: " + Module1.UnderChar + playlist.Name + Module1.UnderChar;
            msg += ": " + Module1.UnderChar + playlist.Tracks.Count + " songs" + Module1.UnderChar;
            msg += ", " + Module1.UnderChar + playlist.Time + " total time" + Module1.UnderChar;
            msg += ", " + Module1.UnderChar + Module1.smartsize((ulong)playlist.Size) + Module1.UnderChar;
            IrcObject.WriteMessage(msg, channel);
            ReleaseApp();
        }

        void ItunesPlayCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!GetApp(IrcObject, channel)) return;
            App.Play();
            ReleaseApp();
        }

        void ItunesStopCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!GetApp(IrcObject, channel)) return;
            App.Stop();
            ReleaseApp();
        }

        void ItunesNextCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!GetApp(IrcObject, channel)) return;
            App.NextTrack();
            ReleaseApp();
        }

        void ItunesPrevCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!GetApp(IrcObject, channel)) return;
            App.PreviousTrack();
            ReleaseApp();
        }*/
	}
}
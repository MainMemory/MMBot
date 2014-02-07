using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

namespace MMBot
{
    public class IRCChannel
    {
        public string Name { get; set; }
        public string Keyword { get; set; }
        public bool Hidden { get; set; }
        public string Topic { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public List<IRCUser> People = new List<IRCUser>();
        public List<IRCChanUserStats> stats = new List<IRCChanUserStats>();
        [Newtonsoft.Json.JsonIgnore]
        public System.Timers.Timer CommandTimer = new System.Timers.Timer(5000)
        {
            AutoReset = false,
            Enabled = false
        };
        [Newtonsoft.Json.JsonIgnore]
        public System.Timers.Timer WaitTimer = new System.Timers.Timer(10000)
        {
            AutoReset = false,
            Enabled = false
        };
        public bool greeting;
        public int Consecutivecmds;
        public bool Active = true;
        public LinkCheckMode linkcheck = LinkCheckMode.Off;
        public List<string> VOP = new List<string>();
        public List<string> HOP = new List<string>();
        public List<string> AOP = new List<string>();
        public List<FeedInfo> feeds = new List<FeedInfo>();
        public ulong peakusers;
        public bool displayaccesserror = true;
        public Dictionary<string, UserModes> AccessLevels = new Dictionary<string, UserModes>();
        public List<Note> Notes = new List<Note>();

        [Newtonsoft.Json.JsonIgnore]
        public IRC IrcObject;

        public IRCChannel()
        {
            CommandTimer.Elapsed += CMD_Tick;
            WaitTimer.Elapsed += Wait_Tick;
        }

        public IRCChannel(IRC network, string ChannelName)
            : this()
        {
            IrcObject = network;
            Name = ChannelName;
            Keyword = string.Empty;
        }

        public IRCChannel(IRC network, string ChannelName, string ChannelKeyword)
            : this(network, ChannelName)
        {
            Keyword = ChannelKeyword;
        }

        internal void CMD_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            CommandTimer.Stop();
            Consecutivecmds = 0;
        }

        internal void Wait_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            WaitTimer.Stop();
            IrcObject.WriteMessage("Commands reenabled.", Name);
        }

        public IRCUser GetUser(string name)
        {
            foreach (IRCUser user in People)
            {
                if (user.name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    return user;
            }
            return null;
        }

        public IRCUser[] GetUsersByMask(string mask)
        {
            List<IRCUser> result = new List<IRCUser>();
            foreach (IRCUser item in People)
                if (item.Mask.Like(mask))
                    result.Add(item);
            return result.ToArray();
        }

        public IRCChanUserStats GetUserStats(string name, bool combineAliases)
        {
            if (combineAliases)
            {
                IRCChanUserStats result = new IRCChanUserStats();
                result.name = name;
                List<string> a = IrcObject.GetAliases(name);
                if (a == null)
                    a = new List<string>() { name };
                bool found = false;
                foreach (IRCChanUserStats s in stats)
                    foreach (string al in a)
                        if (al.Equals(s.name, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Combine(s);
                            found = true;
                        }
                if (found)
                    return result;
            }
            else
                foreach (IRCChanUserStats s in stats)
                    if (name.Equals(s.name, StringComparison.OrdinalIgnoreCase))
                        return s;
            return null;
        }

        public IRCChanUserStats GetUserStats(IRCUser person, bool combineAliases)
        {
            return GetUserStats(person.name, combineAliases);
        }

        public void AddUser(string name)
        {
            if (GetUser(name.TrimStart(IrcObject.prefixes)) != null) return;
            People.Add(new IRCUser(name, IrcObject));
            peakusers = Math.Max(peakusers, (ulong)People.Count);
            if (GetUserStats(name.TrimStart(IrcObject.prefixes), false) == null)
                stats.Add(new IRCChanUserStats(name.TrimStart(IrcObject.prefixes)));
            else
            {
                IRCChanUserStats stat = GetUserStats(name.TrimStart(IrcObject.prefixes), false);
                stat.name = name.TrimStart(IrcObject.prefixes);
                stat.refcount++;
                if (!stat.onlinetimer.IsRunning)
                    stat.onlinetimer.Start();
            }
            if (IrcObject.GetAliases(name.TrimStart(IrcObject.prefixes)) == null)
            {
                List<string> x = new List<string>();
                x.Add(name.TrimStart(IrcObject.prefixes));
                IrcObject.Aliases.Add(x);
            }
        }

        public void RemoveUser(string name)
        {
            foreach (IRCUser user in People)
            {
                if (user.name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    People.Remove(user);
                    return;
                }
            }
        }

        public class FeedInfo
        {
            public string url;
            public string title;
            public System.DateTime lastupdate;

            public FeedInfo() { }
            public FeedInfo(string url, DateTime lastupdate)
            {
                this.url = url;
                this.lastupdate = lastupdate;
            }

            public FeedInfo(string url)
            {
                this.url = url;
                for (int eqq = 1; eqq <= 5; eqq++)
                {
                    try
                    {
                        XmlReader r = null;
                        for (int trycnt = 0; trycnt < 3; trycnt++)
                        {
                            try
                            {
                                r = XmlReader.Create(url);
                                break;
                            }
                            catch { }
                        }
                        XmlSerializer xs = new XmlSerializer(typeof(RssFeed));
                        if (!xs.CanDeserialize(r))
                        {
                            xs = new XmlSerializer(typeof(AtomFeed));
                            AtomFeed feed = (AtomFeed)xs.Deserialize(r);
                            r.Close();
                            title = feed.Title.Text;
                            Array.Sort(feed.Entries);
                            AtomFeedEntry entry = feed.Entries[feed.Entries.Length - 1];
                            if (entry.GetPostTimestamp().HasValue)
                                lastupdate = entry.GetPostTimestamp().Value;
                        }
                        else
                        {
                            RssFeed feed = (RssFeed)xs.Deserialize(r);
                            r.Close();
                            title = feed.Channel.Title;
                            Array.Sort(feed.Channel.Entries);
                            RssFeedEntry entry = feed.Channel.Entries[feed.Channel.Entries.Length - 1];
                            if (entry.GetPostTimestamp().HasValue)
                                lastupdate = entry.GetPostTimestamp().Value;
                        }
                        break;
                    }
                    catch
                    {
                        //IrcObject.WriteMessage("Feed error: " & Feed.url & " " & ex.GetType().Name & ": " & ex.Message, OpName)
                        //IrcObject.WriteMessage(ex.StackTrace, OpName)
                    }
                    System.Threading.Thread.Sleep(100);
                }
                if (lastupdate == System.DateTime.MinValue)
                    lastupdate = DateTime.Now;
            }
        }
    }

    public enum LinkCheckMode
    {
        Off,
        TitleOnly,
        On
    }

    public class Note
    {
        [DefaultValue(-1)]
        public int ID { get; set; }
        public string Sender { get; set; }
        [DefaultValue(true)]
        public bool UseAliases { get; set; }
        [DefaultValue(NoteMode.JoinOrText)]
        public NoteMode Mode { get; set; }
        public string Target { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }

        public Note() { }

        public Note(int id, string sender, bool useAliases, NoteMode mode, string target, DateTime date, string message)
        {
            ID = id;
            Sender = sender;
            UseAliases = useAliases;
            Mode = mode;
            Target = target;
            Date = date;
            Message = message;
        }
    }

    public enum NoteMode
    {
        JoinOrText,
        JoinOnly,
        TextOnly
    }
}
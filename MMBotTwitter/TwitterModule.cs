using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using LinqToTwitter;
using MMBot;
using Newtonsoft.Json;

namespace MMBotTwitter
{
    public class TwitterModule : BotModule
    {
        ApplicationOnlyAuthorizer auth;
        TwitterContext context;
        Dictionary<string, Dictionary<string, TwitterChannelInfo>> feeds = new Dictionary<string, Dictionary<string, TwitterChannelInfo>>();
        Timer timer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds) { AutoReset = true };
        internal static readonly string defaultformat = Module1.UnderChar + "Twitter / {User.Name}" + Module1.UnderChar + ": " + Module1.UnderChar + "{Text}" + Module1.UnderChar + " ( http://twitter.com/{User.Identifier.ScreenName}/status/{StatusID} )";
		const string ConsumerKey = null; // The Consumer Key you get from registering an app on https://dev.twitter.com/
		const string ConsumerSecret = null; // The Consumer Secret you get from registering the app

        public TwitterModule()
        {
            if (Type.GetType("Mono.Runtime") != null || ConsumerKey == null || ConsumerSecret == null)
            {
                LoadFailed = true;
                return;
            }
            auth = new ApplicationOnlyAuthorizer();
            auth.Credentials = new InMemoryCredentials() { ConsumerKey = ConsumerKey, ConsumerSecret = ConsumerSecret };
            auth.Authorize();
            context = new TwitterContext(auth);
            ChangeDirectory();
            if (File.Exists("Twitter.json"))
            {
                JsonSerializer js = new JsonSerializer();
                StreamReader sr = new StreamReader("Twitter.json");
                JsonTextReader jr = new JsonTextReader(sr);
                feeds = js.Deserialize<Dictionary<string, Dictionary<string, TwitterChannelInfo>>>(jr);
                jr.Close();
                sr.Close();
            }
            RestoreDirectory();
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Start();
            foreach (IRC network in Module1.IrcApp.IrcObjects)
                network.eventMessage += new Message(network_eventMessage);
            Module1.AddLinkHandler(LinkHandler);
        }

        public override void Shutdown()
        {
            timer.Stop();
            auth.Invalidate();
            Save();
        }

        public override void Save()
        {
            ChangeDirectory();
            JsonSerializer js = new JsonSerializer();
            StreamWriter sw = new StreamWriter("Twitter.json");
            JsonTextWriter jw = new JsonTextWriter(sw);
            js.Serialize(jw, feeds);
            jw.Close();
            sw.Close();
            RestoreDirectory();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Save();
            foreach (KeyValuePair<string, Dictionary<string, TwitterChannelInfo>> net in feeds)
            {
                IRC network = Module1.GetNetworkByName(net.Key);
                if (network == null || !network.Connected) continue;
                foreach (KeyValuePair<string, TwitterChannelInfo> chan in net.Value)
                    if (network.GetChannel(chan.Key) != null && network.GetChannel(chan.Key).Active)
                        foreach (TwitterUserInfo item in chan.Value.feeds)
                        {
                            var statusTweets =
                from tweet in context.Status
                where tweet.Type == StatusType.User
                      && tweet.UserID == item.id.ToString(NumberFormatInfo.InvariantInfo)
                      && tweet.SinceID == item.lasttweet
                select tweet;
                            foreach (Status tweet in statusTweets.Reverse())
                            {
								DisplayTweet(network, chan.Key, tweet);
                                item.lasttweet = Math.Max(item.lasttweet, ulong.Parse(tweet.StatusID, NumberStyles.None, NumberFormatInfo.InvariantInfo));
                            }
                        }
            }
        }

        void TwitterCommand(IRC IrcObject, string channel, string user, string command)
        {
            Status status;
            ulong id;
            if (ulong.TryParse(command, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out id))
            {
                var statuses =
        from tweet in context.Status
        where tweet.Type == StatusType.Show &&
              tweet.ID == command
        select tweet;
                try { status = statuses.Single(); }
                catch
                {
                    IrcObject.WriteMessage("No tweet with that ID was found!", channel);
                    return;
                }
            }
            else
            {
                var users =
                from tweet in context.Status
                where tweet.Type == StatusType.User
                      && tweet.ScreenName == command
                      && tweet.Count == 1
                select tweet;
                try { status = users.Single(); }
                catch
                {
                    IrcObject.WriteMessage("No user by that name was found, or that user has not made any tweets.", channel);
                    return;
                }
            }
            DisplayTweet(IrcObject, channel, status);
        }

        private void DisplayTweet(IRC IrcObject, string channel, Status status)
        {
            status.Text = System.Web.HttpUtility.HtmlDecode(status.Text.Replace('\n', ' ').Replace('\r', ' ').TrimExcessSpaces());
            foreach (UrlEntity url in status.Entities.UrlEntities)
                status.Text = status.Text.Replace(url.Url, url.ExpandedUrl);
            Dictionary<string, string> tweetdict = status.ToStringDictionary();
            string format = defaultformat;
            if (feeds.ContainsKey(IrcObject.name) && feeds[IrcObject.name].ContainsKey(channel))
                format = feeds[IrcObject.name][channel].format;
            foreach (KeyValuePair<string, string> prop in tweetdict)
                format = format.Replace("{" + prop.Key + "}", prop.Value);
            IrcObject.WriteMessage(format, channel);
        }

        void network_eventMessage(IRC sender, string User, string channel, string Message)
        {
            if (channel.StartsWith("#") && !Message.Contains(" "))
            {
                if (Message.StartsWith("@#"))
                {
                    var statuses =
            from tweet in context.Status
            where tweet.Type == StatusType.Show &&
      tweet.ID == Message.Substring(2)
            select tweet;
                    Status status;
                    try { status = statuses.Single(); }
                    catch
                    {
                        sender.WriteMessage("No tweet with that ID was found!", channel);
                        return;
                    }
                    DisplayTweet(sender, channel, status);
                }
                else if (Message.StartsWith("@"))
                {
                    var users =
from tweet in context.Status
where tweet.Type == StatusType.User
      && tweet.ScreenName == Message.Substring(1)
      && tweet.Count == 1
select tweet;
                    Status status;
                    try { status = users.Single(); }
                    catch
                    {
                        sender.WriteMessage("No user by that name was found, or that user has not made any tweets.", channel);
                        return;
                    }
                    DisplayTweet(sender, channel, status);
                }
            }
        }

        static readonly Regex StatusLink = new Regex(@"^https?://twitter\.com/[^/]+/status(?:es)?/([0-9]+)$");
        static readonly Regex UserLink = new Regex(@"^https?://twitter\.com/([^/]+)$");
        bool LinkHandler(LinkCheckParams param)
        {
            string channel = param.Channel;
            IRC sender = param.IrcObject;
            string url = param.Url;
            if (StatusLink.IsMatch(url))
            {
                var statuses =
        from tweet in context.Status
        where tweet.Type == StatusType.Show &&
  tweet.ID == StatusLink.Match(url).Groups[1].Value
        select tweet;
                Status status;
                try { status = statuses.Single(); }
                catch
                {
                    sender.WriteMessage("No tweet with that ID was found!", channel);
                    return true;
                }
                DisplayTweet(sender, channel, status);
                return true;
            }
            else if (UserLink.IsMatch(url))
            {
                var users =
from tweet in context.Status
where tweet.Type == StatusType.User
      && tweet.ScreenName == UserLink.Match(url).Groups[1].Value
      && tweet.Count == 1
select tweet;
                Status status;
                try { status = users.Single(); }
                catch
                {
                    sender.WriteMessage("No user by that name was found, or that user has not made any tweets.", channel);
                    return true;
                }
                DisplayTweet(sender, channel, status);
                return true;
            }
            return false;
        }

        void TwitterAddCommand(IRC IrcObject, string channel, string user, string command)
        {
            IQueryable<User> users;
            ulong id;
            if (ulong.TryParse(command, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out id))
                users =
        from tweet in context.User
        where tweet.Type == UserType.Show &&
              tweet.UserID == command
        select tweet;
            else
                users =
        from tweet in context.User
        where tweet.Type == UserType.Show &&
              tweet.ScreenName == command
        select tweet;
            var t = users.SingleOrDefault();
            if (!feeds.ContainsKey(IrcObject.name))
                feeds.Add(IrcObject.name, new Dictionary<string, TwitterChannelInfo>());
            Dictionary<string, TwitterChannelInfo> channels = feeds[IrcObject.name];
            if (!channels.ContainsKey(channel))
                channels.Add(channel, new TwitterChannelInfo());
            channels[channel].feeds.Add(new TwitterUserInfo(ulong.Parse(t.Identifier.UserID, NumberStyles.None, NumberFormatInfo.InvariantInfo), t.Status == null ? 0 : ulong.Parse(t.Status.StatusID, NumberStyles.None, NumberFormatInfo.InvariantInfo)));
            IrcObject.WriteMessage("Added feed for user \"" + t.Identifier.ScreenName + "\" (id " + t.Identifier.UserID + ").", channel);
        }

        void TwitterFormatCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!feeds.ContainsKey(IrcObject.name))
                feeds.Add(IrcObject.name, new Dictionary<string, TwitterChannelInfo>());
            Dictionary<string, TwitterChannelInfo> channels = feeds[IrcObject.name];
            if (!channels.ContainsKey(channel))
                channels.Add(channel, new TwitterChannelInfo());
            if (!string.IsNullOrEmpty(command))
                channels[channel].format = command;
            IrcObject.WriteMessage("Tweet format is: " + channels[channel].format, channel);
        }

        void TwitterDelCommand(IRC IrcObject, string channel, string user, string command)
        {
            ulong id;
            if (!ulong.TryParse(command, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out id))
            {
                IQueryable<User> users =
    from tweet in context.User
    where tweet.Type == UserType.Show &&
          tweet.ScreenName == command
    select tweet;
                var t = users.SingleOrDefault();
                id = ulong.Parse(t.Identifier.UserID, NumberStyles.None, NumberFormatInfo.InvariantInfo);
            }
            if (!feeds.ContainsKey(IrcObject.name))
                return;
            Dictionary<string, TwitterChannelInfo> channels = feeds[IrcObject.name];
            if (!channels.ContainsKey(channel))
                return;
            foreach (TwitterUserInfo info in channels[channel].feeds)
                if (info.id == id)
                {
                    channels[channel].feeds.Remove(info);
                    IrcObject.WriteMessage("Feed deleted.", channel);
                    return;
                }
        }
    }

    public class TwitterChannelInfo
    {
        public List<TwitterUserInfo> feeds = new List<TwitterUserInfo>();
        public string format = TwitterModule.defaultformat;

        public TwitterChannelInfo()
        { }
    }

    public class TwitterUserInfo
    {
        public ulong id;
        public ulong lasttweet;

        public TwitterUserInfo(ulong id, ulong lasttweet)
        {
            this.id = id;
            this.lasttweet = lasttweet;
        }
    }
}
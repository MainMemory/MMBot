using System;
using Newtonsoft.Json;

namespace MMBot
{
    public class IRCUser
    {
        public string name, user, host;
        public UserModes mode;

        [JsonIgnore]
        public IRC IrcObject;
        public IRCUser() { }
        public IRCUser(string Name, IRC server)
        {
            IrcObject = server;
            if (Array.IndexOf(IrcObject.prefixes, Name[0]) > -1)
            {
                if (Array.IndexOf(IrcObject.prefixes, Name[0]) < IrcObject.voiceind)
                {
                    mode = (UserModes)(Array.IndexOf(IrcObject.prefixes, Name[0]) - IrcObject.voiceind);
                }
                else
                {
                    mode = (UserModes)(Array.IndexOf(IrcObject.prefixes, Name[0]) + 1 - IrcObject.voiceind);
                }
                Name = Name.Substring(1);
            }
            this.name = Name;
        }

        public string GetModeChar()
        {
            if (mode > UserModes.Normal)
                return IrcObject.prefixes[(int)mode - 1 + IrcObject.voiceind].ToString();
            else if (mode < UserModes.Normal)
                return IrcObject.prefixes[(int)mode + IrcObject.voiceind].ToString();
            else
                return string.Empty;
        }

        public string Mask { get { return name + "!" + user + "@" + host; } }
    }

    public class IRCChanUserStats
    {
        public string name { get; set; }
        public ulong kicks { get; set; }
        public ulong kicked { get; set; }
        public ulong messages { get; set; }
        public ulong actions { get; set; }
        public ulong words { get; set; }
        public ulong characters { get; set; }
        public double wordsperline { get { return messages == 0 ? 0 : words / (double)messages; } }
        public double charsperline { get { return messages == 0 ? 0 : characters / (double)messages; } }
        public double charsperword { get { return words == 0 ? 0 : characters / (double)words; } }
        public ulong commands { get; set; }
        public ulong quits { get; set; }
        public ulong userquits { get; set; }
        public ulong pingquits { get; set; }
        public ulong joins { get; set; }
        public ulong parts { get; set; }
        public ulong fparts { get; set; }
        public ulong modes { get; set; }
        public DateTime lastaction { get; set; }
        public string lastmessage { get; set; }
        public string greeting = string.Empty;
        public TimeSpan onlinesaved { get; set; }
        [JsonIgnore]
        public TimeSpan onlinetime { get { return onlinesaved + onlinetimer.Elapsed; } }
        [JsonIgnore]
        public int refcount = 0;
        [JsonIgnore]
        public System.Diagnostics.Stopwatch onlinetimer = new System.Diagnostics.Stopwatch();

        public IRCChanUserStats() { }
        public IRCChanUserStats(string Name)
        {
            this.name = Name;
            refcount = 1;
            onlinetimer.Start();
        }

        public void Combine(IRCChanUserStats other)
        {
            if (!object.ReferenceEquals(this, other))
            {
                messages += other.messages;
                actions += other.actions;
                words += other.words;
                characters += other.characters;
                commands += other.commands;
                kicked += other.kicked;
                kicks += other.kicks;
                fparts += other.fparts;
                joins += other.joins;
                parts += other.parts;
                pingquits += other.pingquits;
                quits += other.quits;
                userquits += other.userquits;
                modes += other.modes;
                onlinesaved += other.onlinetime;
                if (lastaction < other.lastaction)
                {
                    lastaction = other.lastaction;
                    lastmessage = other.lastmessage;
                }
            }
        }
    }

    public enum UserModes
    {
        Normal,
        Voice,
        Halfop,
        Operator,
        Admin,
        Owner,
        BotOp = int.MaxValue
    }
}
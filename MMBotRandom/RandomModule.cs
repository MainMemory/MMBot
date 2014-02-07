using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using MMBot;
using Newtonsoft.Json;
using IniFile;

namespace MMBotRandom
{
    public class RandomModule : BotModule
    {
        static internal RandomModule Instance;
        int lastquote = -1;
        RandomData data = new RandomData();
        Dictionary<string, string> quotelists = new Dictionary<string, string>();
        List<string> Quotes = new List<string>();
        MarkovWordTextModel markovdict = new MarkovWordTextModel(3);

        public RandomModule()
        {
            Instance = this;
            ChangeDirectory();
            if (File.Exists("quotes.ini"))
                quotelists = IniSerializer.Deserialize<Dictionary<string, string>>("quotes.ini");
            if (File.Exists("Random.json"))
            {
                Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
                StreamReader sr = new StreamReader("Random.json");
                Newtonsoft.Json.JsonTextReader jr = new Newtonsoft.Json.JsonTextReader(sr);
                data = js.Deserialize<RandomData>(jr);
                jr.Close();
                sr.Close();
            }
            ChangeQuoteLists();
            foreach (KeyValuePair<string, Dictionary<string, ChannelData>> item in data.ChannelData)
            {
                IRC network = Module1.GetNetworkByName(item.Key);
                foreach (KeyValuePair<string, ChannelData> chan in item.Value)
                {
                    chan.Value.IrcObject = network;
                    chan.Value.channel = chan.Key;
                    chan.Value.RandTimer.Interval = Module1.Random.Next(chan.Value.randtime) + 1;
                }
            }
            foreach (IRC network in Module1.IrcApp.IrcObjects)
                network.eventMessage += new Message(network_eventMessage);
            RestoreDirectory();
        }

        void network_eventMessage(IRC sender, string User, string channel, string Message)
        {
            if (!Message.StartsWith("!") & channel.StartsWith("#"))
            {
                ChannelData rc = GetChannelData(sender, channel);
                if (!rc.random)
                    return;
                if (rc.probability > Module1.Random.Next(1, 100))
                    QuoteMarkov(sender, channel, User, Message);
            }
        }

        public override void Shutdown()
        {
            foreach (KeyValuePair<string, Dictionary<string, ChannelData>> net in data.ChannelData)
                foreach (KeyValuePair<string, ChannelData> chan in net.Value)
                    if (chan.Value.asplode)
                    {
                        chan.Value.probability = chan.Value.lastprob;
                        chan.Value.randtime = chan.Value.lasttime;
                        chan.Value.randtimer = chan.Value.lasttimer;
                        chan.Value.asplode = false;
                    }
            Save();
        }

        public override void  Save()
        {
            ChangeDirectory();
            Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
            StreamWriter stw = new StreamWriter("Random.json");
            Newtonsoft.Json.JsonTextWriter jw = new Newtonsoft.Json.JsonTextWriter(stw);
            js.Serialize(jw, data);
            jw.Close();
            stw.Close();
            RestoreDirectory();
        }

        ChannelData GetChannelData(IRC network, string channel)
        {
            if (!data.ChannelData.ContainsKey(network.name))
                data.ChannelData.Add(network.name, new Dictionary<string, ChannelData>());
            Dictionary<string, ChannelData> netdata = data.ChannelData[network.name];
            if (!netdata.ContainsKey(channel))
                netdata.Add(channel, new ChannelData() { IrcObject = network, channel = channel });
            return netdata[channel];
        }

        void ChangeQuoteLists()
        {
            Quotes.Clear();
            markovdict.Clear();
            foreach (string item in data.QuoteList.Split(','))
                if (quotelists.ContainsKey(item.ToLower()))
                    Quotes.AddRange(File.ReadAllLines(quotelists[item.ToLower()]));
            Quotes.TrimExcess();
            markovdict.AddStrings(Quotes.ToArray());
        }

        public void QuoteMarkov(IRC IrcObject, string channel)
        {
            switch (data.QuoteMode)
            {
                case 1:
                case 3:
                case 4:
                case 5:
                case 6:
                    Quote(IrcObject, channel, string.Empty);
                    break;
                case 2:
                    Markov(IrcObject, channel, string.Empty);
                    break;
            }
        }

        public void QuoteMarkov(IRC IrcObject, string channel, string user, string message)
        {
            switch (data.QuoteMode)
            {
                case 1:
                    Quote(IrcObject, channel, user);
                    break;
                case 2:
                    Markov(IrcObject, channel, user);
                    break;
                case 3:
                    Quoteplus(IrcObject, channel, user, message);
                    break;
                case 4:
                    Quoteplusplus(IrcObject, channel, user, message);
                    break;
                case 5:
                    Quoteminus(IrcObject, channel, user, message);
                    break;
                case 6:
                    Quoteminusminus(IrcObject, channel, user, message);
                    break;
            }
        }

        public void Quote(IRC IrcObject, string channel, string User) { Quote(IrcObject, channel, User, -1); }
        public void Quote(IRC IrcObject, string channel, string User, int index)
        {
            List<IRCUser> people = IrcObject.GetChannel(channel, true, User).People;
            if (Quotes.Count == 0)
                IrcObject.WriteMessage("I have nothing to say.", channel);
            else
            {
                Random a = new Random();
                int b = lastquote;
                if (index == -1)
                    if (Quotes.Count > 1)
                        while (b == lastquote)
                            b = a.Next(Quotes.Count);
                    else
                        b = 0;
                else
                    b = index % Quotes.Count;
                lastquote = b;
                string[] sep = { "[name]" };
                string[] msg = Quotes[b].Split(sep, StringSplitOptions.None);
                string message = "";
                for (int i = 0; i < msg.Length - 1; i++)
                    message += msg[i] + people[a.Next(people.Count)].name;
                message += msg[msg.Length - 1];
                sep = new string[] { "[NAME]" };
                msg = message.Split(sep, StringSplitOptions.None);
                message = "";
                for (int i = 0; i < msg.Length - 1; i++)
                    message += msg[i] + people[a.Next(people.Count)].name.ToUpper();
                IrcObject.WriteMessage(message + msg[msg.Length - 1], channel);
            }
        }

        public void Markov(IRC IrcObject, string Channel, string User)
        {
            List<IRCUser> people = IrcObject.GetChannel(Channel, true, User).People;
            Random a = new Random();
            string str = markovdict.Generate(data.MarkovLevel);
            if (str.StartsWith(Module1.CTCPChar + "ACTION"))
                if (!str.EndsWith(Module1.CTCPChar.ToString()))
                    str += Module1.CTCPChar;
                else
                    str.Replace(Module1.CTCPChar.ToString(), "");
            string[] sep = { "[name]" };
            string[] msg = str.Split(sep, StringSplitOptions.None);
            string message = "";
            for (int i = 0; i <= msg.Length - 2; i++)
                message += msg[i] + people[a.Next(people.Count)].name;
            message += msg[msg.Length - 1];
            string[] sep2 = { "[NAME]" };
            string[] msg2 = message.Split(sep2, StringSplitOptions.None);
            string message2 = "";
            for (int i = 0; i <= msg2.Length - 2; i++)
                message2 += msg2[i] + people[a.Next(people.Count)].name.ToUpper();
            IrcObject.WriteMessage(message2 + msg2[msg2.Length - 1], Channel);
        }

        public void Quoteplus(IRC IrcObject, string channel, string User, string messa)
        {
            if (string.IsNullOrEmpty(messa))
            {
                Quote(IrcObject, channel, User);
                return;
            }
            if (Quotes.Count == 0)
                IrcObject.WriteMessage("I have nothing to say.", channel);
            else
            {
                Random a = new Random();
                List<int> quoteinds = new List<int>();
                string matchword = messa.Split(' ')[a.Next(messa.Split(' ').Length)].TrimEnd(':', ',', '.', ';', '?', '!', Module1.CTCPChar).Trim(Module1.BoldChar, Module1.UnderChar, Module1.ColorChar);
                for (int i = 0; i <= Quotes.Count - 1; i++)
                    if (Quotes[i].ToLower().Contains(matchword.ToLower()))
                        quoteinds.Add(i);
                if (quoteinds.Count > 0)
                    Quote(IrcObject, channel, User, quoteinds[a.Next(quoteinds.Count)]);
                else
                    Quote(IrcObject, channel, User);
            }
        }

        public void Quoteplusplus(IRC IrcObject, string channel, string User, string messa)
        {
            if (string.IsNullOrEmpty(messa))
            {
                Quote(IrcObject, channel, User);
                return;
            }
            if (Quotes.Count == 0)
                IrcObject.WriteMessage("I have nothing to say.", channel);
            else
            {
                int[] matches = new int[Quotes.Count];
                string[] line = messa.Split(' ');
                for (int i = 0; i < line.Length; i++)
                    line[i] = line[i].TrimEnd(':', ',', '.', ';', '?', '!', Module1.CTCPChar).Trim(Module1.BoldChar, Module1.UnderChar, Module1.ColorChar);
                List<string> lineb = new List<string>();
                foreach (string item in line)
                    if (!lineb.Contains(item))
                        lineb.Add(item);
                for (int i = 0; i < Quotes.Count; i++)
                {
                    if (Quotes[i].Equals(messa, StringComparison.CurrentCultureIgnoreCase))
                        continue;
                    for (int j = 0; j < lineb.Count; j++)
                        if (Quotes[i].ToLower().Contains(lineb[j].ToLower()))
                            matches[i]++;
                }
                int highestnum = 0;
                List<int> bestmatches = new List<int>();
                for (int i = 0; i < matches.Length; i++)
                {
                    if (matches[i] > highestnum)
                    {
                        bestmatches.Clear();
                        highestnum = matches[i];
                    }
                    if (matches[i] == highestnum)
                        bestmatches.Add(i);
                }
                Random a = new Random();
                Quote(IrcObject, channel, User, bestmatches[a.Next(bestmatches.Count)]);
            }
        }

        public void Quoteminus(IRC IrcObject, string channel, string User, string messa)
        {
            if (string.IsNullOrEmpty(messa))
            {
                Quote(IrcObject, channel, User);
                return;
            }
            if (Quotes.Count == 0)
                IrcObject.WriteMessage("I have nothing to say.", channel);
            else
            {
                Random a = new Random();
                List<int> quoteinds = new List<int>();
                string matchword = messa.Split(' ')[a.Next(messa.Split(' ').Length)].TrimEnd(':', ',', '.', ';', '?', '!', Module1.CTCPChar).Trim(Module1.BoldChar, Module1.UnderChar, Module1.ColorChar);
                for (int i = 0; i <= Quotes.Count - 1; i++)
                    if (!Quotes[i].ToLower().Contains(matchword.ToLower()))
                        quoteinds.Add(i);
                if (quoteinds.Count > 0)
                    Quote(IrcObject, channel, User, quoteinds[a.Next(quoteinds.Count)]);
                else
                    Quote(IrcObject, channel, User);
            }
        }

        public void Quoteminusminus(IRC IrcObject, string channel, string User, string messa)
        {
            if (string.IsNullOrEmpty(messa))
            {
                Quote(IrcObject, channel, User);
                return;
            }
            if (Quotes.Count == 0)
                IrcObject.WriteMessage("I have nothing to say.", channel);
            else
            {
                int[] matches = new int[Quotes.Count];
                string[] line = messa.Split(' ');
                for (int i = 0; i < line.Length; i++)
                    line[i] = line[i].TrimEnd(':', ',', '.', ';', '?', '!', Module1.CTCPChar).Trim(Module1.BoldChar, Module1.UnderChar, Module1.ColorChar);
                List<string> lineb = new List<string>(System.Linq.Enumerable.Distinct(line));
                for (int i = 0; i < Quotes.Count; i++)
                    for (int j = 0; j < lineb.Count; j++)
                        if (Quotes[i].ToLower().Contains(lineb[j].ToLower()))
                            matches[i]++;
                int highestnum = int.MaxValue;
                List<int> bestmatches = new List<int>();
                for (int i = 0; i < matches.Length; i++)
                {
                    if (matches[i] < highestnum)
                    {
                        bestmatches.Clear();
                        highestnum = matches[i];
                    }
                    if (matches[i] == highestnum)
                        bestmatches.Add(i);
                }
                Random a = new Random();
                Quote(IrcObject, channel, User, bestmatches[a.Next(bestmatches.Count)]);
            }
        }

        void RandomCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData ch = GetChannelData(IrcObject, channel);
            IrcObject.WriteMessage("Random responses are currently " + (ch.random ? "en" : "dis") + "abled.", channel);
        }

        void RandomOnCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData ch = GetChannelData(IrcObject, channel);
            ch.random = true;
            IrcObject.WriteMessage("Random responses enabled.", channel);
        }

        void RandomOffCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData ch = GetChannelData(IrcObject, channel);
            ch.random = false;
            ch.RandTimer.Stop();
            IrcObject.WriteMessage("Random responses disabled.", channel);
        }

        void RandomModeCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                if (!Module1.CheckAccessLevel(UserModes.BotOp, IrcObject.GetChannel(channel, true, user).GetUser(user)))
                    return;
                switch (command.ToLower().Strip())
                {
                    case "quote":
                        data.QuoteMode = 1;
                        break;
                    case "markov":
                        data.QuoteMode = 2;
                        break;
                    case "quote+":
                        data.QuoteMode = 3;
                        break;
                    case "quote++":
                        data.QuoteMode = 4;
                        break;
                    case "quote-":
                        data.QuoteMode = 5;
                        break;
                    case "quote--":
                        data.QuoteMode = 6;
                        break;
                }
            }
            IrcObject.WriteMessage("Random response mode is " + Module1.Choose(data.QuoteMode, "Quote", "Markov", "Quote+", "Quote++", "Quote-", "Quote--") + ".", channel);
        }

        void RandomListCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                if (!Module1.CheckAccessLevel(UserModes.BotOp, IrcObject.GetChannel(channel, true, user).GetUser(user)))
                    return;
                data.QuoteList = command.ToLower().Strip();
                ChangeDirectory();
                ChangeQuoteLists();
                RestoreDirectory();
            }
            IrcObject.WriteMessage("Random response list is " + data.QuoteList + ".", channel);
        }

        void RandomAsplodeCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData chan = GetChannelData(IrcObject, channel);
            if (chan.asplode) return;
            chan.lastprob = chan.probability;
            chan.lasttime = chan.randtime;
            chan.lasttimer = chan.randtimer;
            chan.probability = 100;
            chan.randtime = 5000;
            chan.RandTimer.Stop();
            chan.RandTimer.Interval = Module1.Random.Next(5000) + 1;
            chan.RandTimer.Start();
            chan.asplode = true;
        }

        void RandomUnasplodeCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData chan = GetChannelData(IrcObject, channel);
            if (!chan.asplode) return;
            chan.probability = chan.lastprob;
            chan.randtime = chan.lasttime;
            chan.randtimer = chan.lasttimer;
            chan.asplode = false;
        }

        void RandomProbCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData chan = GetChannelData(IrcObject, channel);
            if (!string.IsNullOrEmpty(command))
            {
                if (!Module1.CheckAccessLevel(UserModes.Admin, IrcObject.GetChannel(channel, true, user).GetUser(user)))
                    return;
                int num = int.Parse(command.Strip().TrimEnd('%'));
                if (num >= 0 & num <= 100)
                    chan.probability = num;
            }
            IrcObject.WriteMessage("Random probability is " + chan.probability + "%.", channel);
        }

        void RandomTimerCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData ch = GetChannelData(IrcObject, channel);
            IrcObject.WriteMessage("Random quote timer is " + (ch.randtimer ? "en" : "dis") + "abled.", channel);
        }

        void RandomTimerOnCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData ch = GetChannelData(IrcObject, channel);
            ch.RandTimer.Start();
            IrcObject.WriteMessage("Random quote timer started.", channel);
        }

        void RandomTimerOffCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData ch = GetChannelData(IrcObject, channel);
            ch.RandTimer.Stop();
            IrcObject.WriteMessage("Random quote timer stopped.", channel);
        }

        void RandomTimeCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!channel.StartsWith("#")) return;
            ChannelData chan = GetChannelData(IrcObject, channel);
            if (!string.IsNullOrEmpty(command))
            {
                if (!Module1.CheckAccessLevel(UserModes.Admin, IrcObject.GetChannel(channel, true, user).GetUser(user)))
                    return;
                chan.randtime = (int)Module1.GetTimeSpan(command.Strip()).Value.TotalMilliseconds;
            }
            IrcObject.WriteMessage("Maximum interval is " + TimeSpan.FromMilliseconds(chan.randtime).ToStringCustM() + ".", channel);
        }

        void RandomFindCommand(IRC IrcObject, string channel, string user, string command)
        {
            for (int i = 0; i < Quotes.Count; i++)
                if (Quotes[i].Like(command))
                    IrcObject.WriteMessage("Quote " + (i + 1) + ": " + Quotes[i], user);
        }

        void RandomQuoteCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.Strip();
            if (!string.IsNullOrEmpty(command))
            {
                if (command.Equals("last", StringComparison.OrdinalIgnoreCase))
                    Quote(IrcObject, channel, user, lastquote);
                else
                    Quote(IrcObject, channel, user, int.Parse(command) - 1);
            }
            else
                Quote(IrcObject, channel, user);
        }

        void RandomMarkovCommand(IRC IrcObject, string channel, string user, string command)
        {
            Markov(IrcObject, channel, user);
        }

        void RandomMarkovLevelCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                Module1.CheckAccessLevel(UserModes.BotOp, IrcObject.GetChannel(channel, true, user).GetUser(user));
                int lvl = data.MarkovLevel;
                if (int.TryParse(command.Strip(), out lvl))
                    if (lvl >= 1 & lvl <= 4)
                        data.MarkovLevel = lvl;
            }
            IrcObject.WriteMessage("Markov level is " + data.MarkovLevel + ".", channel);
        }

        void RandomQuotesCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage("I currently have " + Quotes.Count + " quotes loaded in " + data.QuoteList.Split(',').Length + " lists.", channel);
        }

        void RandomKickCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            if (ChanObj.GetUser(IrcObject.IrcNick).mode > UserModes.Voice)
            {
                UserModes m = (UserModes)Math.Min((int)ChanObj.GetUser(user).mode, (int)ChanObj.GetUser(IrcObject.IrcNick).mode);
                List<IRCUser> users = new List<IRCUser>();
                foreach (IRCUser u in ChanObj.People)
                    if (u.mode <= m)
                        users.Add(u);
                if (users.Count > 0)
                    IrcObject.QueueWrite("KICK " + channel + " " + users[Module1.Random.Next(users.Count)].name + " :CONGLATURATION YOU'RE WINNER!");
                else
                    IrcObject.WriteMessage("I can't kick anyone on this channel.", channel);
            }
            else
                IrcObject.WriteMessage("I can't kick anyone on this channel.", channel);
        }
    }

    public class RandomData
    {
        [DefaultValue(1)]
        public int QuoteMode { get; set; }
        [DefaultValue("")]
        public string QuoteList { get; set; }
        [DefaultValue(1)]
        public int MarkovLevel { get; set; }
        public Dictionary<string, Dictionary<string, ChannelData>> ChannelData { get; set; }

        public RandomData()
        {
            QuoteMode = 1;
            QuoteList = "";
            MarkovLevel = 1;
            ChannelData = new Dictionary<string, Dictionary<string, ChannelData>>();
        }
    }

    public class ChannelData
    {
        [JsonIgnore]
        public bool asplode;
        [JsonIgnore]
        public int lastprob;
        [JsonIgnore]
        public int lasttime;
        [JsonIgnore]
        public bool lasttimer;
        [JsonIgnore]
        public System.Timers.Timer RandTimer = new System.Timers.Timer
        {
            AutoReset = true,
            Enabled = false
        };
        [JsonIgnore]
        public IRC IrcObject;
        [JsonIgnore]
        public string channel;
        public int randtime = 600000;
        public bool randtimer { get { return RandTimer.Enabled; } set { RandTimer.Enabled = value; } }
        public int probability = 25;
        public bool random;

        public ChannelData()
        {
            RandTimer.Elapsed += RandTimer_Elapsed;
        }

        private void RandTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (random && IrcObject != null && IrcObject.Connected && IrcObject.GetChannel(channel).Active)
                RandomModule.Instance.QuoteMarkov(IrcObject, channel);
            RandTimer.Interval = Module1.Random.Next(randtime) + 1;
        }
    }
}
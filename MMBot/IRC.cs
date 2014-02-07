using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace MMBot
{
    #region "Delegates"
    public delegate void CommandReceived(IRC sender, string IrcCommand);
    public delegate void TopicSet(IRC sender, string IrcChannel, string IrcTopic);
    public delegate void TopicOwner(IRC sender, string IrcChannel, string IrcUser, string TopicDate);
    public delegate void NamesList(IRC sender, string IrcChannel, string UserNames);
    public delegate void ServerMessage(IRC sender, string ServerMessage);
    public delegate void Join(IRC sender, string IrcChannel, string IrcNick, string IrcUser, string IrcHost);
    public delegate void Part(IRC sender, string IrcChannel, string IrcUser, string PartMessage);
    public delegate void Mode(IRC sender, string IrcChannel, string IrcUser, string UserMode);
    public delegate void NickChange(IRC sender, string UserOldNick, string UserNewNick);
    public delegate void Kick(IRC sender, string IrcChannel, string UserKicker, string UserKicked, string KickMessage);
    public delegate void evQuit(IRC sender, string UserQuit, string QuitMessage);
    public delegate void Message(IRC sender, string User, string channel, string Message);
    #endregion
    public class IRC
    {
        #region "Events"
        public event CommandReceived eventReceiving;
        public event TopicSet eventTopicSet;
        public event TopicOwner eventTopicOwner;
        public event NamesList eventNamesList;
        public event ServerMessage eventServerMessage;
        public event Join eventJoin;
        public event Part eventPart;
        public event Mode eventMode;
        public event NickChange eventNickChange;
        public event Kick eventKick;
        public event evQuit eventQuit;
        public event Message eventMessage;
        public event Message eventNotice;
        public event CommandReceived eventUnknown;
        #endregion

        #region "Private Variables"
        private string m_ircNick;
        private string m_ircUser;
        private string m_ircRealName;
        private List<IRCChannel> m_channels = new List<IRCChannel>();
        private bool isInvisible;
        private TcpClient m_ircConnection;
        private Stream m_ircStream;
        private StreamWriter m_ircWriter;
        private StreamReader m_ircReader;
        private string m_hostname;
        private string m_nsPass;
        private System.Timers.Timer ConCheck = new System.Timers.Timer(60000) { AutoReset = true };
        #endregion

        #region "Properties"
        [Newtonsoft.Json.JsonIgnore]
        public string[] IrcServers { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public int[] IrcPorts { get; set; }
        private int servInd;

        public string IrcServer { get { return IrcServers[servInd]; } }

        public int IrcPort { get { return IrcPorts[servInd]; } }

        public string IrcNick
        {
            get { return this.m_ircNick; }
            set { this.m_ircNick = value; }
        }

        public string NSPass
        {
            get { return m_nsPass; }
            set { m_nsPass = value; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public string IrcUser
        {
            get { return this.m_ircUser; }
            set { this.m_ircUser = value; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public string IrcRealName
        {
            get { return this.m_ircRealName; }
            set { this.m_ircRealName = value; }
        }

        public List<IRCChannel> IrcChannels
        {
            get { return m_channels; }
            set { this.m_channels = value; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public bool IsInvisble
        {
            get { return this.isInvisible; }
            set { this.isInvisible = value; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public TcpClient IrcConnection
        {
            get { return this.m_ircConnection; }
            set { this.m_ircConnection = value; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public Stream IrcStream
        {
            get { return this.m_ircStream; }
            set { this.m_ircStream = value; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public StreamWriter IrcWriter
        {
            get { return this.m_ircWriter; }
            set { this.m_ircWriter = value; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public StreamReader IrcReader
        {
            get { return this.m_ircReader; }
            set { this.m_ircReader = value; }
        }

        [Newtonsoft.Json.JsonIgnore]
        public string IrcHostName
        {
            get { return m_hostname; }
            set { m_hostname = value; }
        }

        public bool Connected { get; private set; }
        #endregion
        [Newtonsoft.Json.JsonIgnore]
        public char[] prefixes = { '+', '%', '@', '&', '~' };
        [Newtonsoft.Json.JsonIgnore]
        public char[] modechars = { 'v', 'h', 'o', 'a', 'q' };
        [Newtonsoft.Json.JsonIgnore]
        public int voiceind = 0;
        [Newtonsoft.Json.JsonIgnore]
        public char[][] chanmodes = new char[4][];

        public string name;
        private bool usessl;
        #region "Constructor"
        public static IRC Load(ServerInfo servinf)
        {
            IRC result;
            if (System.IO.File.Exists(System.IO.Path.Combine(Environment.CurrentDirectory, servinf.name + ".json")))
            {
                Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
                StreamReader sr = new StreamReader(System.IO.Path.Combine(Environment.CurrentDirectory, servinf.name + ".json"));
                Newtonsoft.Json.JsonTextReader jr = new Newtonsoft.Json.JsonTextReader(sr);
                result = js.Deserialize<IRC>(jr);
                foreach (IRCChannel chan in result.IrcChannels)
                {
                    chan.IrcObject = result;
                    chan.Active = false;
                    foreach (IRCUser user in chan.People)
                        user.IrcObject = result;
                }
                jr.Close();
                sr.Close();
            }
            else
                result = new IRC();
            for (int i = 0; i < 4; i++)
                result.chanmodes[i] = new char[0];
            result.name = servinf.name;
            result.usessl = servinf.usessl;
            List<string> servers = new List<string>();
            List<int> ports = new List<int>();
            foreach (string s in servinf.servers.Split(' '))
            {
                string[] servport = s.Split(':');
                servers.Add(servport[0]);
                if (servport.Length > 1)
                    ports.Add(int.Parse(servport[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture));
                else
                    ports.Add(6667);
            }
            result.IrcServers = servers.ToArray();
            result.IrcPorts = ports.ToArray();
            result.IrcUser = "MMBot";
            result.IrcRealName = Module1.ColorChar + "4" + Module1.StopChar + "MMBot IRC Bot by MainMemory";
            foreach (string item in servinf.channels.Split(' '))
            {
                if (result.GetChannel(item.Split(',')[0]) != null)
                {
                    if (item.Contains(","))
                        result.GetChannel(item.Split(',')[0]).Keyword = item.Split(',')[1];
                    else
                        result.GetChannel(item).Keyword = string.Empty;
                    result.GetChannel(item.Split(',')[0]).Active = true;
                }
                else
                    if (item.Contains(","))
                        result.IrcChannels.Add(new IRCChannel(result, item.Split(',')[0], item.Split(',')[1]));
                    else
                        result.IrcChannels.Add(new IRCChannel(result, item));
            }
            result.IsInvisble = false;
            result.ConCheck.Elapsed += new System.Timers.ElapsedEventHandler(result.ConCheck_Elapsed);
            return result;
        }

        void ConCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            TestConnection();
        }
        #endregion

        #region "Public Methods"
        Thread connthrd;
        public void Connect()
        {
            // Connect with the IRC server.
            if (IrcWriter != null)
                IrcWriter.Close();
            if (IrcReader != null)
                IrcReader.Close();
            if (IrcConnection != null)
                IrcConnection.Close();
            if (connthrd != null && connthrd.IsAlive) connthrd.Abort();
            writethread = new System.Threading.Thread(ProcessWriteQueue);
        con: try
            {
                IrcConnection = new TcpClient(IrcServer, IrcPort) { SendTimeout = 5000 };
                IrcStream = IrcConnection.GetStream();
                if (usessl)
                {
                    System.Net.Security.SslStream sslstr = new System.Net.Security.SslStream(IrcStream, false, (a, b, c, d) => true);
                    sslstr.AuthenticateAsClient(IrcServer);
                    IrcStream = sslstr;
                }
                IrcStream.WriteTimeout = 5000;
            }
            catch { Module1.WriteOutput(name, "Connection failed, retrying...", false); servInd = (servInd + 1) % IrcServers.Length; Thread.Sleep(1000); goto con; }
            IrcReader = new StreamReader(IrcStream);
            IrcWriter = new StreamWriter(IrcStream);
            connthrd = new Thread(ReadFromServer);
            connthrd.Start();
            ConCheck.Start();
        }

        private void ReadFromServer()
        {
            Connected = true;
            if (!writethread.IsAlive) writethread.Start();
            // Authenticate our user
            string isInvisible = IsInvisble ? "8" : "0";
            QueueWrite(String.Format("USER {0} {1} * :{2}", this.IrcUser, isInvisible, this.IrcRealName));
            QueueWrite("NICK " + this.IrcNick);
            // Listen for commands
            while (Connected)
            {
                string ircCommand = string.Empty;
                try
                {
                    ircCommand = this.IrcReader.ReadLine();
                    while (!string.IsNullOrEmpty(ircCommand))
                    {
                        if (eventReceiving != null)
                        {
                            eventReceiving(this, ircCommand);
                        }

                        string[] commandParts = new string[ircCommand.Split(' ').Length];
                        commandParts = ircCommand.Split(' ');
                        commandParts[0] = commandParts[0].TrimStart(':');
                        if (commandParts[0] == "PING")
                        {
                            // Server PING, send PONG back
                            this.IrcPing(commandParts);
                        }
                        else
                        {
                            // Normal message
                            string commandAction = commandParts[1];
                            switch (commandAction)
                            {
                                case "001":
                                    QueueWrite("PRIVMSG NickServ :identify " + NSPass, "MODE " + IrcNick + " +B");
                                    //IrcWriter.Flush()
                                    if (IrcChannels.Any((a) => string.IsNullOrEmpty(a.Keyword)))
                                        QueueWrite("JOIN " + string.Join(",", 
                                            IrcChannels.Where((a) => a.Active && string.IsNullOrEmpty(a.Keyword)).Select((a) => a.Name)));
                                    if (IrcChannels.Any((a) => !string.IsNullOrEmpty(a.Keyword)))
                                        QueueWrite("JOIN " + string.Join(",",
                                            IrcChannels.Where((a) => a.Active && !string.IsNullOrEmpty(a.Keyword)).Select((a) => a.Name)) + " "
                                            + string.Join(",",
                                            IrcChannels.Where((a) => a.Active && !string.IsNullOrEmpty(a.Keyword)).Select((a) => a.Keyword)));
                                    IrcServerMessage(commandParts);
                                    break;
                                case "005":
                                    for (int i = 2; i <= commandParts.Length - 5; i++)
                                    {
                                        switch (commandParts[i].Split('=')[0])
                                        {
                                            case "PREFIX":
                                                modechars = commandParts[i].Substring(commandParts[i].IndexOf('(') + 1, commandParts[i].IndexOf(')') - (commandParts[i].IndexOf('(') + 1)).ToCharArray();
                                                Array.Reverse(modechars);
                                                prefixes = commandParts[i].Substring(commandParts[i].IndexOf(')') + 1).ToCharArray();
                                                Array.Reverse(prefixes);
                                                voiceind = Array.IndexOf(modechars, 'v');
                                                break;
                                            case "CHANMODES":
                                                string[] chm = commandParts[i].Split('=')[1].Split(',');
                                                for (int j = 0; j < 4; j++)
                                                    chanmodes[j] = chm[j].ToCharArray();
                                                break;
                                        }
                                    }
                                    IrcServerMessage(commandParts);
                                    break;
                                case "319":
                                    for (int i = 4; i <= commandParts.Length - 1; i++)
                                    {
                                        if (GetChannel(commandParts[i].TrimStart(':').TrimStart(prefixes)) != null)
                                        {
                                            IRCChannel chanobj = GetChannel(commandParts[i].TrimStart(':').TrimStart(prefixes));
                                            if (chanobj.GetUser(commandParts[3]) != null)
                                            {
                                                char prefix = commandParts[i].TrimStart(':')[0];
                                                UserModes mode = UserModes.Normal;
                                                if (Array.IndexOf(prefixes, prefix) > -1)
                                                {
                                                    if (Array.IndexOf(prefixes, prefix) < voiceind)
                                                    {
                                                        mode = (UserModes)(Array.IndexOf(prefixes, prefix) - voiceind);
                                                    }
                                                    else
                                                    {
                                                        mode = (UserModes)(Array.IndexOf(prefixes, prefix) + 1 - voiceind);
                                                    }
                                                }
                                                chanobj.GetUser(commandParts[3]).mode = mode;
                                            }
                                        }
                                    }
                                    IrcServerMessage(commandParts);
                                    break;
                                case "324":
                                    GetChannel(commandParts[3]).Hidden = commandParts[4].Contains("s");
                                    IrcServerMessage(commandParts);
                                    break;
                                case "332":
                                    IrcTopic(commandParts);
                                    break;
                                case "333":
                                    IrcTopicOwner(commandParts);
                                    break;
                                case "352":
                                    //:irc.badnik.net 352 GLaDOS #SF94 MainMemory x-hax.cultnet.net irc.badnik.net MainMemory H& :0 ???
                                    GetChannel(commandParts[3]).GetUser(commandParts[7]).user = commandParts[4];
                                    GetChannel(commandParts[3]).GetUser(commandParts[7]).host = commandParts[5];
                                    IrcServerMessage(commandParts);
                                    break;
                                case "353":
                                    IrcNamesList(commandParts);
                                    break;
                                case "366":
                                    //IrcEndNamesList(commandParts)
                                    IrcServerMessage(commandParts);
                                    break;
                                case "396":
                                    //:irc.badnik.net 396 MMBot|Debug bdnk-53f6dcf1.mpls.qwest.net :is now your displayed host
                                    IrcHostName = commandParts[3];
                                    IrcServerMessage(commandParts);
                                    break;
                                case "372":
                                    //IrcMOTD(commandParts)
                                    IrcServerMessage(commandParts);
                                    break;
                                case "376":
                                    //IrcEndMOTD(commandParts)
                                    IrcServerMessage(commandParts);
                                    break;
                                case "433":
                                    IrcNick += "_";
                                    QueueWrite("NICK " + IrcNick);
                                    IrcServerMessage(commandParts);
                                    break;
                                case "474":
                                    string IRCChannel = commandParts[3];
                                    if (GetChannel(IRCChannel).Active)
                                    {
                                        QueueWrite("PRIVMSG ChanServ :unban " + IRCChannel);
                                        if (!string.IsNullOrEmpty(GetChannel(IRCChannel).Keyword))
                                        {
                                            QueueWrite("JOIN " + IRCChannel + " " + GetChannel(IRCChannel).Keyword);
                                        }
                                        else
                                        {
                                            QueueWrite("JOIN " + IRCChannel);
                                        }
                                    }
                                    IrcServerMessage(commandParts);
                                    break;
                                case "495":
                                    string Channel = commandParts[3];
                                    if (GetChannel(Channel).Active)
                                    {
                                        if (!string.IsNullOrEmpty(GetChannel(Channel).Keyword))
                                        {
                                            QueueWrite("JOIN " + Channel + " " + GetChannel(Channel).Keyword);
                                        }
                                        else
                                        {
                                            QueueWrite("JOIN " + Channel);
                                        }
                                    }
                                    IrcServerMessage(commandParts);
                                    break;
                                case "JOIN":
                                    this.IrcJoin(commandParts);
                                    break;
                                case "PART":
                                    this.IrcPart(commandParts);
                                    break;
                                case "MODE":
                                    this.IrcMode(commandParts);
                                    break;
                                case "NICK":
                                    this.IrcNickChange(commandParts);
                                    break;
                                case "KICK":
                                    this.IrcKick(commandParts);
                                    break;
                                case "QUIT":
                                    this.IrcQuit(commandParts);
                                    break;
                                case "PRIVMSG":
                                    IrcMsg(commandParts);
                                    break;
                                case "NOTICE":
                                    IrcNotice(commandParts);
                                    break;
                                case "TOPIC":
                                    try { GetChannel(commandParts[2]).Topic = Module1.Recombine(commandParts, 3).Substring(1); }
                                    catch { }
                                    break;
                                case "INVITE":
                                    WriteMessage(commandParts[0].Split('!')[0] + " has invited me to " + commandParts[3].Substring(1) + ".", Module1.OpName);
                                    break;
                                case "PONG":
                                    if (commandParts[3].TrimStart(':') != "test")
                                        WriteMessage((DateTime.Now - System.DateTime.FromBinary(long.Parse(commandParts[3].TrimStart(':')))).ToStringCustM(), Module1.OpName);
                                    break;
                                default:
                                    IrcUnknown(ircCommand);
                                    break;
                            }
                        }
                        ircCommand = this.IrcReader.ReadLine();
                    }
                }
                catch (ThreadAbortException) { return; }
#if !DEBUG
                catch (Exception ex)
                {
                    //MsgBox("Error")
                    Module1.WriteOutput(name, ex.GetType().Name, false);
                    Module1.WriteOutput(name, ex.Message, false);
                    Module1.WriteOutput(name, ex.StackTrace, false);
                    if (ex is IOException)
                    {
                        if (Connected)
                        {
                            servInd = (servInd + 1) % IrcServers.Length;
                            Connect();
                        }
                        return;
                    }
                }
#endif
            }
        }


        public void WriteMessage(string message, string channel)
        {
            string mes2 = "";
            for (int a = 0; a <= message.Length - 1; a++)
            {
                if ((a > 0 && (message[a] == '\n' & message[a - 1] != '\r')) | (a == 0 & message[a] == '\n'))
                {
                    mes2 += '\r';
                }
                mes2 += message[a];
                if ((a < (message.Length - 1) && (message[a] == '\r' & message[a + 1] != '\n')) | ((a == message.Length - 1) & message[a] == '\r'))
                {
                    mes2 += '\n';
                }
            }
            IRCChannel x = GetChannel(channel);
            foreach (string line in mes2.SplitByString("\r\n"))
            {
                string outline = line;
                if (!line.StartsWith(Module1.CTCPChar.ToString()))
                    outline = line.Translate();
                else if (line.StartsWith(Module1.CTCPChar + "ACTION", StringComparison.CurrentCultureIgnoreCase))
                    outline = Module1.CTCPChar + "ACTION " + line.Remove(0, 8).TrimEnd(Module1.CTCPChar).Translate() + Module1.CTCPChar;
                if (channel.Equals("#sf94", StringComparison.OrdinalIgnoreCase))
                {
                    int ind = 0;
                    while ((ind = outline.IndexOf("the game", ind, StringComparison.OrdinalIgnoreCase)) > -1)
                    {
                        outline = outline.Remove(ind + 4, 4);
                        outline = outline.Insert(ind + 4, "derp");
                    }
                    ind = 0;
                    while ((ind = outline.IndexOf("the gaem", ind, StringComparison.OrdinalIgnoreCase)) > -1)
                    {
                        outline = outline.Remove(ind + 4, 4);
                        outline = outline.Insert(ind + 4, "depr");
                    }
                    ind = 0;
                    while ((ind = outline.IndexOf("teh game", ind, StringComparison.OrdinalIgnoreCase)) > -1)
                    {
                        outline = outline.Remove(ind + 4, 4);
                        outline = outline.Insert(ind + 4, "derp");
                    }
                    ind = 0;
                    while ((ind = outline.IndexOf("teh gaem", ind, StringComparison.OrdinalIgnoreCase)) > -1)
                    {
                        outline = outline.Remove(ind + 4, 4);
                        outline = outline.Insert(ind + 4, "depr");
                    }
                }
                if (outline.StartsWith(Module1.CTCPChar.ToString()))
                {
                    QueueWrite("PRIVMSG " + channel + " :" + outline);
                    if (outline.StartsWith(Module1.CTCPChar + "ACTION "))
                    {
                        Module1.WriteOutput(name + "\\" + channel, Module1.ColorChar + "18* " + IrcNick + Module1.ColorChar + " " + outline.Remove(0, 8).TrimEnd(Module1.CTCPChar), true);
                        if (x != null)
                            x.GetUserStats(IrcNick, false).actions++;
                    }
                }
                else
                {
					int maxlen = 510 - ("PRIVMSG " + channel + " :").Length;
					while (outline.Length > maxlen)
					{
						bool white = false;
						string tmp = outline.Substring(0, maxlen);
						for (int i = tmp.Length - 1; i >= 0; i--)
							if (char.IsWhiteSpace(tmp, i))
							{
								tmp = tmp.Substring(0, i);
								white = true;
							}
						QueueWrite("PRIVMSG " + channel + " :" + tmp);
						Module1.WriteOutput(name + "\\" + channel, Module1.ColorChar + "18<" + (x != null ? x.GetUser(IrcNick).GetModeChar() : "") + IrcNick + ">" + Module1.ColorChar + " " + tmp, true);
						if (x != null)
						{
							x.GetUserStats(IrcNick, false).messages++;
							x.GetUserStats(IrcNick, false).words += (ulong)tmp.Split(' ').Length;
							x.GetUserStats(IrcNick, false).characters += (ulong)tmp.Length;
						}
						outline = outline.Remove(0, tmp.Length + (white ? 1 : 0));
					}
					QueueWrite("PRIVMSG " + channel + " :" + outline);
                    Module1.WriteOutput(name + "\\" + channel, Module1.ColorChar + "18<" + (x != null ? x.GetUser(IrcNick).GetModeChar() : "") + IrcNick + ">" + Module1.ColorChar + " " + outline, true);
                    if (x != null)
                    {
                        x.GetUserStats(IrcNick, false).messages += 1;
                        x.GetUserStats(IrcNick, false).words += (ulong)outline.Split(' ').Length;
                        x.GetUserStats(IrcNick, false).characters += (ulong)outline.Length;
                    }
                }
            }
            if (x != null)
            {
                x.GetUserStats(IrcNick, false).lastaction = DateTime.Now;
                if (message.StartsWith(Module1.CTCPChar + "action", StringComparison.CurrentCultureIgnoreCase))
                    x.GetUserStats(IrcNick, false).lastmessage = "saying \"* " + IrcNick + " " + message.Remove(0, 8).TrimEnd(Module1.CTCPChar) + Module1.StopChar + "\".";
                else
                    x.GetUserStats(IrcNick, false).lastmessage = "saying \"" + message + Module1.StopChar + "\".";
            }
        }

        public void WriteMessage(string prefix, string message, string channel)
        {
            string mes2 = "";
            for (int a = 0; a <= message.Length - 1; a++)
            {
                if ((a > 0 && (message[a] == '\n' & message[a - 1] != '\r')) | (a == 0 & message[a] == '\n'))
                {
                    mes2 += '\r';
                }
                mes2 += message[a];
                if ((a < (message.Length - 1) && (message[a] == '\r' & message[a + 1] != '\n')) | ((a == message.Length - 1) & message[a] == '\r'))
                {
                    mes2 += '\n';
                }
            }
            IRCChannel x = GetChannel(channel);
            foreach (string line in mes2.SplitByString("\r\n"))
            {
                string outline = line;
                if (!line.StartsWith(Module1.CTCPChar.ToString()))
                    outline = prefix + line.Translate();
                else if (line.StartsWith(Module1.CTCPChar + "ACTION", StringComparison.CurrentCultureIgnoreCase))
                    outline = Module1.CTCPChar + "ACTION " + line.Remove(0, 8).TrimEnd(Module1.CTCPChar).Translate() + Module1.CTCPChar;
                if (channel.Equals("#sf94", StringComparison.OrdinalIgnoreCase))
                {
                    int ind = 0;
                    while ((ind = outline.IndexOf("the game", ind, StringComparison.OrdinalIgnoreCase)) > -1)
                    {
                        outline = outline.Remove(ind + 4, 4);
                        outline = outline.Insert(ind + 4, "derp");
                    }
                    ind = 0;
                    while ((ind = outline.IndexOf("the gaem", ind, StringComparison.OrdinalIgnoreCase)) > -1)
                    {
                        outline = outline.Remove(ind + 4, 4);
                        outline = outline.Insert(ind + 4, "depr");
                    }
                    ind = 0;
                    while ((ind = outline.IndexOf("teh game", ind, StringComparison.OrdinalIgnoreCase)) > -1)
                    {
                        outline = outline.Remove(ind + 4, 4);
                        outline = outline.Insert(ind + 4, "derp");
                    }
                    ind = 0;
                    while ((ind = outline.IndexOf("teh gaem", ind, StringComparison.OrdinalIgnoreCase)) > -1)
                    {
                        outline = outline.Remove(ind + 4, 4);
                        outline = outline.Insert(ind + 4, "depr");
                    }
                }
                if (outline.StartsWith(Module1.CTCPChar.ToString()))
                {
                    QueueWrite("PRIVMSG " + channel + " :" + outline);
                    if (outline.StartsWith(Module1.CTCPChar + "ACTION "))
                    {
                        Module1.WriteOutput(name + "\\" + channel, Module1.ColorChar + "18* " + IrcNick + Module1.ColorChar + " " + outline.Remove(0, 8).TrimEnd(Module1.CTCPChar), true);
                        if (x != null)
                            x.GetUserStats(IrcNick, false).actions++;
                    }
                }
                else
                {
                    int maxlen = 510 - ("PRIVMSG " + channel + " :").Length;
                    while (outline.Length > maxlen)
                    {
						bool white = false;
						string tmp = outline.Substring(0, maxlen);
						for (int i = tmp.Length - 1; i >= 0; i--)
							if (char.IsWhiteSpace(tmp, i))
							{
								tmp = tmp.Substring(0, i);
								white = true;
							}
                        QueueWrite("PRIVMSG " + channel + " :" + tmp);
                        Module1.WriteOutput(name + "\\" + channel, Module1.ColorChar + "18<" + (x != null ? x.GetUser(IrcNick).GetModeChar() : "") + IrcNick + ">" + Module1.ColorChar + " " + tmp, true);
                        if (x != null)
                        {
                            x.GetUserStats(IrcNick, false).messages++;
                            x.GetUserStats(IrcNick, false).words += (ulong)tmp.Split(' ').Length;
                            x.GetUserStats(IrcNick, false).characters += (ulong)tmp.Length;
                        }
                        outline = outline.Remove(0, tmp.Length + (white ? 1 : 0));
                    }
                    QueueWrite("PRIVMSG " + channel + " :" + outline);
                    Module1.WriteOutput(name + "\\" + channel, Module1.ColorChar + "18<" + (x != null ? x.GetUser(IrcNick).GetModeChar() : "") + IrcNick + ">" + Module1.ColorChar + " " + outline, true);
                    if (x != null)
                    {
                        x.GetUserStats(IrcNick, false).messages++;
                        x.GetUserStats(IrcNick, false).words += (ulong)outline.Split(' ').Length;
                        x.GetUserStats(IrcNick, false).characters += (ulong)outline.Length;
                    }
                }
            }
            if (x != null)
            {
                x.GetUserStats(IrcNick, false).lastaction = DateTime.Now;
                if (message.StartsWith(Module1.CTCPChar + "action", StringComparison.CurrentCultureIgnoreCase))
                    x.GetUserStats(IrcNick, false).lastmessage = "saying \"* " + IrcNick + " " + message.Remove(0, 8).TrimEnd(Module1.CTCPChar) + Module1.StopChar + "\".";
                else
                    x.GetUserStats(IrcNick, false).lastmessage = "saying \"" + message + Module1.StopChar + "\".";
            }
        }

        public void SendNotice(string message, string channel)
        {
            string mes2 = "";
            for (int a = 0; a <= message.Length - 1; a++)
            {
                if ((a > 0 && (message[a] == '\n' & message[a - 1] != '\r')) | (a == 0 & message[a] == '\n'))
                {
                    mes2 += '\r';
                }
                mes2 += message[a];
                if ((a < (message.Length - 1) && (message[a] == '\r' & message[a + 1] != '\n')) | ((a == message.Length - 1) & message[a] == '\r'))
                {
                    mes2 += '\n';
                }
            }
            foreach (string line in mes2.SplitByString("\r\n"))
            {
                if (line.StartsWith(Module1.CTCPChar.ToString()))
                    QueueWrite("NOTICE " + channel + " :" + line);
                else
                {
                    string outline = line;
                    int maxlen = 470 - ("NOTICE " + channel + " :").Length;
                    while (outline.Length > maxlen)
                    {
                        QueueWrite("NOTICE " + channel + " :" + outline.Substring(0, maxlen));
                        Module1.WriteOutput(name + "\\" + channel, Module1.ColorChar + "28-" + Module1.ColorChar + "29" + IrcNick + Module1.ColorChar + "28-" + Module1.ColorChar + " " + outline.Substring(0, maxlen), true);
                        outline = outline.Remove(0, maxlen);
                    }
                    QueueWrite("NOTICE " + channel + " :" + outline);
                    Module1.WriteOutput(name + "\\" + channel, Module1.ColorChar + "28-" + Module1.ColorChar + "29" + IrcNick + Module1.ColorChar + "28-" + Module1.ColorChar + " " + outline, true);
                }
            }
        }

        private Queue<string> WriteQueue = new Queue<string>();
        private EventWaitHandle queueLock = new EventWaitHandle(false, EventResetMode.AutoReset);
        private Thread writethread;
        public void ProcessWriteQueue()
        {
            while (Connected)
            {
                try
                {
                    queueLock.WaitOne();
                    lock (queueLock)
                    {
                        while (WriteQueue.Count > 0)
                        {
                            IrcWriter.WriteLine(WriteQueue.Dequeue());
                            System.Threading.Thread.Sleep(1);
                        }
                        IrcWriter.Flush();
                    }
                }
                catch
                {
                    if (Connected)
                    {
                        WriteQueue.Clear();
                        servInd = (servInd + 1) % IrcServers.Length;
                        Connect();
                    }
                }
            }
        }

        public void QueueWrite(params string[] str)
        {
            lock (queueLock)
            {
                foreach (string item in str)
                {
                    WriteQueue.Enqueue(item);
                }
            }
            queueLock.Set();
        }

        public void TestConnection()
        {
            lock (queueLock)
            {
                try
                {
                    IrcWriter.WriteLine("PING :test");
                    IrcWriter.Flush();
                }
                catch
                {
                    if (Connected)
                    {
                        WriteQueue.Clear();
                        servInd = (servInd + 1) % IrcServers.Length;
                        Connect();
                    }
                }
            }
        }

        public void Disconnect(string message)
        {
            if (!Connected) return;
            lock (queueLock)
            {
                WriteQueue.Clear();
                try
                {
                    IrcWriter.WriteLine("QUIT :" + message);
                    IrcWriter.Flush();
                }
                finally
                {
                    Connected = false;
                    if (connthrd != null && connthrd.IsAlive) connthrd.Abort();
                    ConCheck.Stop();
                }
            }
        }
        
        public IRCChannel GetChannel(string name, bool getuserchan, string user)
        {
            foreach (IRCChannel chan in m_channels)
            {
                if (chan.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    return chan;
            }
            if (getuserchan)
            {
                IRCChannel x = new IRCChannel(this, name);
                x.People.Add(new IRCUser(name, this));
                if (name == IrcNick)
                {
                    x.People.Add(new IRCUser(user, this));
                }
                else
                {
                    x.People.Add(new IRCUser(IrcNick, this));
                }
                return x;
            }
            else
            {
                return null;
            }
        }

        public IRCChannel GetChannel(string name) { return GetChannel(name, false, ""); }

        public List<List<string>> Aliases = new List<List<string>>();
        public void AddAlias(string name, string Alias)
        {
            foreach (List<string> li in Aliases)
            {
                bool matchname = false;
                bool matchalias = false;
                foreach (string item in li)
                {
                    if (name.Equals(item, StringComparison.CurrentCultureIgnoreCase))
                        matchname = true;
                    if (Alias.Equals(item, StringComparison.CurrentCultureIgnoreCase))
                        matchalias = true;
                }
                if (matchname & !matchalias)
                    li.Add(Alias);
            }
        }

        public List<string> GetAliases(string name)
        {
            foreach (List<string> li in Aliases)
            {
                foreach (string item in li)
                {
                    if (name.Equals(item, StringComparison.CurrentCultureIgnoreCase))
                        return li;
                }
            }
            return null;
        }

        public void DelAlias(string name, string Alias)
        {
            foreach (List<string> li in Aliases)
            {
                if (li.Contains(name))
                    li.Remove(Alias);
            }
        }

        public void DelAliases(string name)
        {
            List<string> aliases = GetAliases(name);
            if (aliases == null) return;
            Aliases.Remove(aliases);
        }
        #endregion

        #region "Private Methods"
        #region "Server Messages"
        private void IrcTopic(string[] IrcCommand)
        {
            string IrcChannel = IrcCommand[3];
            string IrcTopic = "";
            for (int intI = 4; intI <= IrcCommand.Length - 1; intI++)
            {
                IrcTopic += IrcCommand[intI] + " ";
            }
            if (eventTopicSet != null)
            {
                eventTopicSet(this, IrcChannel, IrcTopic.Remove(0, 1).Trim());
            }
        }
        // IrcTopic 

        private void IrcTopicOwner(string[] IrcCommand)
        {
            string IrcChannel = IrcCommand[3];
            string IrcUser = IrcCommand[4].Split('!')[0];
            string TopicDate = IrcCommand[5];
            if (eventTopicOwner != null)
            {
                eventTopicOwner(this, IrcChannel, IrcUser, TopicDate);
            }
        }
        // IrcTopicOwner 

        private void IrcNamesList(string[] IrcCommand)
        {
            string UserNames = "";
            for (int intI = 5; intI <= IrcCommand.Length - 1; intI++)
            {
                UserNames += IrcCommand[intI] + " ";
            }
            if (eventNamesList != null)
            {
                eventNamesList(this, IrcCommand[4], UserNames.Remove(0, 1).Trim());
            }
        }
        // IrcNamesList 

        private void IrcServerMessage(string[] IrcCommand)
        {
            string ServerMessage = "";
            for (int intI = 1; intI <= IrcCommand.Length - 1; intI++)
            {
                ServerMessage += IrcCommand[intI] + " ";
            }
            if (eventServerMessage != null)
            {
                eventServerMessage(this, ServerMessage.Trim());
            }
        }
        // IrcServerMessage 
        #endregion

        #region "Ping"
        private void IrcPing(string[] IrcCommand)
        {
            string PingHash = "";
            for (int intI = 1; intI <= IrcCommand.Length - 1; intI++)
            {
                PingHash += IrcCommand[intI] + " ";
            }
            QueueWrite("PONG " + PingHash);
            //Me.IrcWriter.Flush()
        }
        // IrcPing 
        #endregion

        #region "User Messages"
        private void IrcJoin(string[] IrcCommand)
        {
            string IrcChannel = IrcCommand[2];
            if (IrcChannel.StartsWith(":"))
                IrcChannel = IrcChannel.Remove(0, 1);
            string IrcNick = IrcCommand[0].Remove(IrcCommand[0].IndexOf('!'));
            string IrcUser = IrcCommand[0].Remove(IrcCommand[0].IndexOf('@')).Substring(IrcCommand[0].IndexOf('!') + 1);
            string IrcHost = IrcCommand[0].Substring(IrcCommand[0].IndexOf('@') + 1);
            if (eventJoin != null)
            {
                eventJoin(this, IrcChannel, IrcNick, IrcUser, IrcHost);
            }
        }
        // IrcJoin 

        private void IrcPart(string[] IrcCommand)
        {
            string IrcChannel = IrcCommand[2];
            string IrcUser = IrcCommand[0].Split('!')[0];
            string message = "";
            if (IrcCommand.Length > 3)
                message = Module1.Recombine(IrcCommand, 3);
            if (message.StartsWith(":"))
                message = message.Remove(0, 1);
            if (eventPart != null)
            {
                eventPart(this, IrcChannel, IrcUser, message);
            }
        }
        // IrcPart 

        private void IrcMode(string[] IrcCommand)
        {
            string IrcChannel = IrcCommand[2];
            string IrcUser = IrcCommand[0].Split('!')[0];
            string UserMode = Module1.Recombine(IrcCommand, 3);
            if (UserMode[0] == ':')
                UserMode = UserMode.Remove(0, 1);
            if (eventMode != null)
            {
                eventMode(this, IrcChannel, IrcUser, UserMode.Trim());
            }
        }
        // IrcMode 

        private void IrcNickChange(string[] IrcCommand)
        {
            string UserOldNick = IrcCommand[0].Split('!')[0];
            string UserNewNick = IrcCommand[2].TrimStart(':');
            if (eventNickChange != null)
            {
                eventNickChange(this, UserOldNick, UserNewNick);
            }
        }
        // IrcNickChange 

        private void IrcKick(string[] IrcCommand)
        {
            string UserKicker = IrcCommand[0].Split('!')[0];
            string UserKicked = IrcCommand[3];
            string IrcChannel = IrcCommand[2];
            string KickMessage = Module1.Recombine(IrcCommand, 4);
            if (eventKick != null)
            {
                eventKick(this, IrcChannel, UserKicker, UserKicked, KickMessage.Remove(0, 1));
            }
        }
        // IrcKick 

        private void IrcQuit(string[] IrcCommand)
        {
            string UserQuit = IrcCommand[0].Split('!')[0];
            string QuitMessage = Module1.Recombine(IrcCommand, 2);
            if (eventQuit != null)
            {
                eventQuit(this, UserQuit, QuitMessage.Remove(0, 1));
            }
        }

        private void IrcMsg(string[] IrcCommand)
        {
            string message = Module1.Recombine(IrcCommand, 3);
            if (message.StartsWith(":"))
                message = message.Remove(0, 1);
            if (eventMessage != null)
            {
                eventMessage(this, IrcCommand[0].Split('!')[0], IrcCommand[2].TrimStart(prefixes), message);
            }
        }

        private void IrcNotice(string[] IrcCommand)
        {
            string message = Module1.Recombine(IrcCommand, 3);
            if (message.StartsWith(":"))
                message = message.Remove(0, 1);
            if (eventNotice != null)
            {
                eventNotice(this, IrcCommand[0].Split('!')[0], IrcCommand[2].TrimStart(prefixes), message);
            }
        }

        private void IrcUnknown(string Command)
        {
            if (eventUnknown != null)
            {
                eventUnknown(this, Command);
            }
        }

        // IrcQuit 
        #endregion
        #endregion
    }
}
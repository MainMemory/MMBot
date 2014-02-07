using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Security.Policy;
using System.Security;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;

namespace MMBot
{
    public delegate void BotCommandFunc(IRC IrcObject, string channel, string user, string command);
    public class cIRC
    {
        public List<IRC> IrcObjects = new List<IRC>();
        internal CookieContainer cookies = new CookieContainer();

        internal void LoadCommands()
        {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(XML.CommandList));
            XML.CommandList lst;
            using (StringReader str = new StringReader(Properties.Resources.CommandList))
                lst = (XML.CommandList)xs.Deserialize(str);
            BotModule modobj = new CommonModule();
            Modules = new Dictionary<string,BotModule>() { { "Common", modobj } };
            List<BotCommand> cmds = new List<BotCommand>();
            foreach (XML.BotCommand item in lst.Commands)
                cmds.Add(CommandFromXML(item, "Common", modobj));
            cmds.Sort((a, b) => a.Name.CompareTo(b.Name));
            CommandDictionary = new Dictionary<string, BotCommand>();
            foreach (BotCommand item in cmds)
                if (!CommandDictionary.ContainsKey(item.Name))
                    CommandDictionary.Add(item.Name, item);
            xs = new System.Xml.Serialization.XmlSerializer(typeof(XML.BotModule));
            foreach (string file in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Modules"), "*.xml", SearchOption.AllDirectories))
            {
                XML.BotModule mod;
                using (System.Xml.XmlReader xr = new System.Xml.XmlTextReader(file))
                {
                    if (!xs.CanDeserialize(xr)) continue;
                    mod = (XML.BotModule)xs.Deserialize(xr);
                }
                if (Modules.ContainsKey(mod.name)) continue;
                try
                {
                    modobj = (BotModule)Assembly.LoadFile(Path.ChangeExtension(file, "dll")).CreateInstance(mod.className, true);
                }
                catch { continue; }
                if (modobj.LoadFailed)
                    continue;
                Modules.Add(mod.name, modobj);
                cmds = new List<BotCommand>();
                foreach (XML.BotCommand item in mod.CommandList)
                    cmds.Add(CommandFromXML(item, mod.name, modobj));
                cmds.Sort((a, b) => a.Name.CompareTo(b.Name));
                foreach (BotCommand item in cmds)
                    if (!CommandDictionary.ContainsKey(item.Name))
                        CommandDictionary.Add(item.Name, item);
            }
        }

        internal BotCommand CommandFromXML(XML.BotCommand command, string module, object codeObj)
        {
            if (!command.HelpTextSpecified)
                command.HelpText = "No help text specified for this command.";
            List<BotCommand> subcommands = new List<BotCommand>();
            if (command.SubCommands != null)
                foreach (XML.BotCommand item in command.SubCommands)
                    subcommands.Add(CommandFromXML(item, module, codeObj));
            return new BotCommand(command.name, module, command.accessLevel, command.functionName == "null" ? null : (BotCommandFunc)Delegate.CreateDelegate(typeof(BotCommandFunc), codeObj, command.functionName, true, true), command.cmdMinLength, command.separateThread, command.HelpText, subcommands.ToArray());
        }

        internal IRC AddConnection(ServerInfo serverInfo)
        {
            IRC x = IRC.Load(serverInfo);
            x.eventJoin += IrcJoin;
            x.eventKick += IrcKick;
            x.eventMessage += ReceiveMessage;
            x.eventMode += IrcMode;
            x.eventNamesList += IrcNamesList;
            x.eventNickChange += IrcNickChange;
            x.eventNotice += ReceiveNotice;
            x.eventPart += IrcPart;
            x.eventQuit += IrcQuit;
            x.eventReceiving += IrcCommandReceived;
            x.eventServerMessage += IrcServerMessage;
            x.eventTopicOwner += IrcTopicOwner;
            x.eventTopicSet += IrcTopicSet;
            x.eventUnknown += UnknownCommand;
            IrcObjects.Add(x);
            return x;
        }

        private void IrcCommandReceived(IRC IrcObject, string IrcCommand)
        {
            //Module1.WriteOutput(IrcCommand)
        }

        private void IrcTopicSet(IRC IrcObject, string IrcChan, string IrcTopic)
        {
            Module1.WriteOutput(IrcObject.name + "\\" + IrcChan, string.Format(Module1.ColorChar + "29* Topic is: {0}", IrcTopic), true);
            IrcObject.GetChannel(IrcChan).Topic = IrcTopic;
        }

        private void IrcTopicOwner(IRC IrcObject, string IrcChan, string IrcUser, string TopicDate)
        {
            Module1.WriteOutput(IrcObject.name + "\\" + IrcChan, string.Format(Module1.ColorChar + "29* Topic set by {0} on {1}", IrcUser, new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(int.Parse(TopicDate)).ToString()), true);
        }

        private void IrcNamesList(IRC IrcObject, string IrcChannel, string UserNames)
        {
            Module1.WriteOutput(IrcObject.name + "\\" + IrcChannel, string.Format(Module1.ColorChar + "29* People: {0}", UserNames), true);
            IRCChannel ChanObj = IrcObject.GetChannel(IrcChannel);
            foreach (string name in UserNames.Split(' '))
            {
                ChanObj.AddUser(name);
                string trim = name.TrimStart(IrcObject.prefixes);
                List<string> aliases = IrcObject.GetAliases(trim);
                if (aliases != null)
                {
                    for (int i = 0; i < aliases.Count; i++)
                        if (aliases[i].Equals(trim, StringComparison.CurrentCultureIgnoreCase))
                        {
                            aliases.RemoveAt(i);
                            break;
                        }
                    aliases.Insert(0, trim);
                }
            }
        }

        private void IrcServerMessage(IRC IrcObject, string ServerMessage)
        {
            Module1.WriteOutput(IrcObject.name, ServerMessage, true);
        }

        private void IrcJoin(IRC IrcObject, string IrcChan, string IrcNick, string IrcUser, string IrcHost)
        {
            Module1.WriteOutput(IrcObject.name + "\\" + IrcChan, string.Format(Module1.ColorChar + "19* {0} joins {1}", IrcNick, IrcChan), true);
            if (IrcNick == IrcObject.IrcNick)
            {
                if (IrcObject.GetChannel(IrcChan) == null)
                    IrcObject.IrcChannels.Add(new IRCChannel(IrcObject, IrcChan));
                IrcObject.QueueWrite("WHO " + IrcChan);
                IrcObject.QueueWrite("MODE " + IrcChan);
                IrcObject.GetChannel(IrcChan).People.Clear();
            }
            IRCChannel ChanObj = IrcObject.GetChannel(IrcChan);
            if (IrcNick != IrcObject.IrcNick & IrcObject.GetChannel(IrcChan).greeting)
                if (ChanObj.GetUserStats(IrcNick, true) != null)
                    if (!ChanObj.GetUserStats(IrcNick, true).lastmessage.Like("quitting with the message \"Changing *\"."))
                        if (!string.IsNullOrWhiteSpace(ChanObj.GetUserStats(IrcNick, true).greeting))
                            IrcObject.WriteMessage(ChanObj.GetUserStats(IrcNick, true).greeting.Replace("[name]", IrcNick), IrcChan);
            ChanObj.AddUser(IrcNick);
            ReadNotes(IrcObject, IrcChan, IrcNick, true);
            IRCUser UserObj = ChanObj.GetUser(IrcNick);
            UserObj.user = IrcUser;
            UserObj.host = IrcHost;
            IRCChanUserStats StatsObj = ChanObj.GetUserStats(UserObj, false);
            StatsObj.joins++;
            StatsObj.lastaction = DateTime.Now;
            StatsObj.lastmessage = "joining the channel.";
            List<string> aliases = IrcObject.GetAliases(IrcNick);
            if (aliases != null)
            {
                for (int i = 0; i < aliases.Count; i++)
                    if (aliases[i].Equals(IrcNick, StringComparison.CurrentCultureIgnoreCase))
                    {
                        aliases.RemoveAt(i);
                        break;
                    }
                aliases.Insert(0, IrcNick);
            }
            string mask = UserObj.Mask;
            foreach (string item in IrcObject.GetChannel(IrcChan).VOP)
                if (mask.Like(item))
                    IrcObject.QueueWrite("MODE " + IrcChan + " +v " + IrcNick);
            foreach (string item in IrcObject.GetChannel(IrcChan).HOP)
                if (mask.Like(item))
                    IrcObject.QueueWrite("MODE " + IrcChan + " +h " + IrcNick + "  " + IrcNick);
            foreach (string item in IrcObject.GetChannel(IrcChan).AOP)
                if (mask.Like(item))
                    IrcObject.QueueWrite("MODE " + IrcChan + " +o " + IrcNick + "  " + IrcNick + "  " + IrcNick);
        }

        private static void ReadNotes(IRC IrcObject, string Channel, string Nick, bool join)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(Channel);
            List<Note> readnotes = new List<Note>();
            List<string> aliases = IrcObject.GetAliases(Nick) ?? new List<string>() { Nick };
            foreach (Note note in ChanObj.Notes)
            {
                if (join & note.Mode == NoteMode.TextOnly) continue;
                if (!join & note.Mode == NoteMode.JoinOnly) continue;
                foreach (string alias in note.UseAliases ? aliases : new List<string>() { Nick })
                    if (note.Target.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    {
                        IrcObject.WriteMessage(Nick + ": " + note.Sender + " left a note " + (DateTime.Now - note.Date).ToStringCust() + " ago: " + note.Message, Channel);
                        readnotes.Add(note);
                    }
            }
            foreach (Note note in readnotes)
                ChanObj.Notes.Remove(note);
        }

        private void IrcPart(IRC IrcObject, string IrcChan, string IrcUser, string PartMessage)
        {
            if (!string.IsNullOrEmpty(PartMessage))
                Module1.WriteOutput(IrcObject.name + "\\" + IrcChan, string.Format(Module1.ColorChar + "23* {0} has left {1} ({2})", IrcUser, IrcChan, PartMessage), false);
            else
                Module1.WriteOutput(IrcObject.name + "\\" + IrcChan, string.Format(Module1.ColorChar + "23* {0} has left {1}", IrcUser, IrcChan), false);
            if (IrcUser == IrcObject.IrcNick)
                if (IrcObject.GetChannel(IrcChan).Active)
                {
                    IrcObject.QueueWrite("PRIVMSG ChanServ :unban " + IrcChan);
                    if (!string.IsNullOrEmpty(IrcObject.GetChannel(IrcChan).Keyword))
                        IrcObject.QueueWrite("JOIN " + IrcChan + " " + IrcObject.GetChannel(IrcChan).Keyword);
                    else
                        IrcObject.QueueWrite("JOIN " + IrcChan);
                }
            IrcObject.GetChannel(IrcChan).RemoveUser(IrcUser);
            IRCChanUserStats stats = IrcObject.GetChannel(IrcChan).GetUserStats(IrcUser, false);
            stats.parts++;
            if (PartMessage.StartsWith("Removed by"))
                stats.fparts++;
            stats.lastaction = DateTime.Now;
            stats.lastmessage = "leaving" + (string.IsNullOrEmpty(PartMessage) ? "" : " with the message \"" + PartMessage + Module1.StopChar + "\"") + ".";
            stats.refcount--;
            if (stats.refcount == 0)
            {
                stats.onlinetimer.Stop();
                stats.onlinesaved = stats.onlinetime;
                stats.onlinetimer.Reset();
            }
            if (IrcUser == IrcObject.IrcNick)
                foreach (IRCChanUserStats stat in IrcObject.GetChannel(IrcChan).stats)
                {
                    stat.onlinetimer.Stop();
                    stat.onlinesaved = stat.onlinetime;
                    stat.onlinetimer.Reset();
                    stat.refcount = 0;
                }
        }

        private void IrcMode(IRC IrcObject, string IrcChan, string IrcUser, string UserMode)
        {
            if (IrcUser != IrcChan)
            {
                Module1.WriteOutput(IrcObject.name + "\\" + IrcChan, string.Format("* {0} sets mode {1}", IrcUser, UserMode), false);
                if (IrcObject.GetChannel(IrcChan) != null)
                {
                    int paramindex = 1;
                    sbyte mode = 1;
                    string[] @params = UserMode.Split(' ');
                    for (int i = 0; i <= @params[0].Length - 1; i++)
                    {
                        if (@params[0][i] == '+')
                        {
                            mode = 1;
                        }
                        else if (@params[0][i] == '-')
                        {
                            mode = -1;
                        }
                        else
                        {
                            switch (mode)
                            {
                                case 1:
                                    switch (@params[0][i])
                                    {
                                        case 'b':
                                            string mask = @params[paramindex];
                                            if (mask.Contains("#")) mask = mask.Remove(mask.IndexOf('#'));
                                            if ((IrcObject.IrcNick + "!" + IrcObject.IrcUser + "@" + IrcObject.IrcHostName).Like(mask))
                                            {
                                                IrcObject.QueueWrite("PRIVMSG ChanServ :unban " + IrcChan);
                                                IrcObject.QueueWrite("mode " + IrcChan + " -b " + @params[paramindex]);
                                                IrcObject.WriteMessage(IrcUser + " has banned me from " + IrcChan + ". (" + mask + ")", Module1.OpName);
                                            }
                                            if (IrcObject.GetChannel(IrcChan).GetUser(Module1.OpName) != null)
                                                if (IrcObject.GetChannel(IrcChan).GetUser(Module1.OpName).Mask.Like(mask))
                                                    IrcObject.QueueWrite("mode " + IrcChan + " -b " + @params[paramindex]);
                                            break;
                                        case 'k':
                                            IrcObject.GetChannel(IrcChan).Keyword = @params[paramindex];
                                            break;
                                        case 's':
                                            IrcObject.GetChannel(IrcChan).Hidden = true;
                                            break;
                                    }
                                    if (Array.IndexOf(IrcObject.modechars, @params[0][i]) > -1 && IrcObject.GetChannel(IrcChan).GetUser(@params[paramindex]) != null)
                                    {
                                        UserModes umode = UserModes.Normal;
                                        if (Array.IndexOf(IrcObject.modechars, @params[0][i]) < IrcObject.voiceind)
                                            umode = (UserModes)(Array.IndexOf(IrcObject.modechars, @params[0][i]) - IrcObject.voiceind);
                                        else
                                            umode = (UserModes)(Array.IndexOf(IrcObject.modechars, @params[0][i]) + 1 - IrcObject.voiceind);
                                        if (IrcObject.GetChannel(IrcChan).GetUser(@params[paramindex]).mode < umode)
                                            IrcObject.GetChannel(IrcChan).GetUser(@params[paramindex]).mode = umode;
                                    }
                                    for (int j = 0; j < 3; j++)
                                        if (IrcObject.chanmodes[j].Contains(@params[0][i]))
                                            paramindex++;
                                    break;
                                case -1:
                                    switch (@params[0][i])
                                    {
                                        case 'k':
                                            IrcObject.GetChannel(IrcChan).Keyword = string.Empty;
                                            break;
                                        case 's':
                                            IrcObject.GetChannel(IrcChan).Hidden = false;
                                            break;
                                    }
                                    if (Array.IndexOf(IrcObject.modechars, @params[0][i]) > -1 && IrcObject.GetChannel(IrcChan).GetUser(@params[paramindex]) != null)
                                    {
                                        UserModes umode = UserModes.Normal;
                                        if (Array.IndexOf(IrcObject.modechars, @params[0][i]) < IrcObject.voiceind)
                                            umode = (UserModes)(Array.IndexOf(IrcObject.modechars, @params[0][i]) - IrcObject.voiceind);
                                        else
                                            umode = (UserModes)(Array.IndexOf(IrcObject.modechars, @params[0][i]) + 1 - IrcObject.voiceind);
                                        if (IrcObject.GetChannel(IrcChan).GetUser(@params[paramindex]).mode <= umode)
                                        {
                                            IrcObject.GetChannel(IrcChan).GetUser(@params[paramindex]).mode = UserModes.Normal;
                                            IrcObject.QueueWrite("WHOIS " + @params[paramindex]);
                                        }
                                    }
                                    for (int j = 0; j < 2; j++)
                                        if (IrcObject.chanmodes[j].Contains(@params[0][i]))
                                            paramindex++;
                                    break;
                            }
                        }
                    }
                    if (IrcObject.GetChannel(IrcChan).GetUserStats(IrcUser, false) != null)
                    {
                        IrcObject.GetChannel(IrcChan).GetUserStats(IrcUser, false).lastaction = DateTime.Now;
                        IrcObject.GetChannel(IrcChan).GetUserStats(IrcUser, false).lastmessage = "setting mode " + UserMode + ".";
                        IrcObject.GetChannel(IrcChan).GetUserStats(IrcUser, false).modes++;
                    }
                }
            }
        }

        private void IrcNickChange(IRC IrcObject, string UserOldNick, string UserNewNick)
        {
            if (UserOldNick.Equals(Module1.OpName, StringComparison.CurrentCultureIgnoreCase))
                Module1.OpName = UserNewNick;
            if (UserOldNick == IrcObject.IrcNick)
                IrcObject.IrcNick = UserNewNick;
            Module1.myForm.Invoke(Module1.myForm.ChangeUserDelegate, IrcObject, UserOldNick, UserNewNick);
            if (IrcObject.GetAliases(UserNewNick) == null)
                IrcObject.AddAlias(UserOldNick, UserNewNick);
            foreach (IRCChannel item in IrcObject.IrcChannels)
            {
                if (item.GetUser(UserOldNick) != null)
                {
                    Module1.WriteOutput(IrcObject.name + "\\" + item.Name, string.Format("* {0} is now known as {1}", UserOldNick, UserNewNick), false);
                    item.GetUser(UserOldNick).name = UserNewNick;
                    IRCChanUserStats oldstats = item.GetUserStats(UserOldNick, false);
                    IRCChanUserStats newstats = item.GetUserStats(UserNewNick, false);
                    if (newstats == null)
                    {
                        newstats = new IRCChanUserStats(UserNewNick);
                        item.stats.Add(newstats);
                    }
                    if (oldstats != newstats)
                    {
                        oldstats.refcount--;
                        if (oldstats.refcount == 0)
                        {
                            oldstats.onlinetimer.Stop();
                            oldstats.onlinesaved = oldstats.onlinetime;
                            oldstats.onlinetimer.Reset();
                        }
                        newstats.refcount++;
                        if (!newstats.onlinetimer.IsRunning)
                            newstats.onlinetimer.Start();
                    }
                    newstats.lastaction = DateTime.Now;
                    newstats.lastmessage = "changing nicks from " + UserOldNick + " to " + UserNewNick + ".";
                    newstats.name = UserNewNick;
                }
            }
            foreach (Module1.Reminder item in Module1.reminders)
            {
                if (UserOldNick.Equals(item.Name, StringComparison.CurrentCultureIgnoreCase))
                    item.Name = UserNewNick;
            }
            List<string> aliases = IrcObject.GetAliases(UserNewNick);
            if (aliases != null)
            {
                for (int i = 0; i < aliases.Count; i++)
                    if (aliases[i].Equals(UserNewNick, StringComparison.CurrentCultureIgnoreCase))
                    {
                        aliases.RemoveAt(i);
                        break;
                    }
                aliases.Insert(0, UserNewNick);
            }
        }

        private void IrcKick(IRC IrcObject, string IrcChannel, string UserKicker, string UserKicked, string KickMessage)
        {
            Module1.WriteOutput(IrcObject.name + "\\" + IrcChannel, string.Format(Module1.ColorChar + "21* {0} kicks {1} out of {2} ({3})", UserKicker, UserKicked, IrcChannel, KickMessage), true);
            IRCChannel ChanObj = IrcObject.GetChannel(IrcChannel);
            IRCChanUserStats kickerstats = ChanObj.GetUserStats(UserKicker, false);
            if (kickerstats != null)
            {
                kickerstats.kicks++;
                kickerstats.lastaction = DateTime.Now;
                kickerstats.lastmessage = "kicking " + UserKicked + " with the message \"" + KickMessage + Module1.StopChar + "\".";
            }
            IRCChanUserStats kickedstats = ChanObj.GetUserStats(UserKicked, false);
            if (kickedstats != null)
            {
                kickedstats.kicked += 1;
                kickedstats.lastaction = DateTime.Now;
                kickedstats.lastmessage = "being kicked by " + UserKicker + " with the message \"" + KickMessage + Module1.StopChar + "\".";
                kickedstats.refcount--;
                if (kickedstats.refcount == 0)
                {
                    kickedstats.onlinetimer.Stop();
                    kickedstats.onlinesaved = kickedstats.onlinetime;
                    kickedstats.onlinetimer.Reset();
                }
            }
            if (UserKicked == IrcObject.IrcNick)
            {
                IrcObject.QueueWrite("PRIVMSG ChanServ :unban " + IrcChannel);
                if (!string.IsNullOrEmpty(ChanObj.Keyword))
                    IrcObject.QueueWrite("JOIN " + IrcChannel + " " + ChanObj.Keyword);
                else
                    IrcObject.QueueWrite("JOIN " + IrcChannel);
                foreach (IRCChanUserStats stat in ChanObj.stats)
                {
                    stat.onlinetimer.Stop();
                    stat.onlinesaved = stat.onlinetime;
                    stat.onlinetimer.Reset();
                    stat.refcount = 0;
                }
            }
            else
                ChanObj.RemoveUser(UserKicked);
        }

        private void IrcQuit(IRC IrcObject, string UserQuit, string QuitMessage)
        {
            foreach (IRCChannel item in IrcObject.IrcChannels)
                if (item.GetUser(UserQuit) != null)
                {
                    Module1.WriteOutput(IrcObject.name + "\\" + item.Name, string.Format(Module1.ColorChar + "23* {0} has quit IRC ({1})", UserQuit, QuitMessage), false);
                    item.RemoveUser(UserQuit);
                    IRCChanUserStats stats = item.GetUserStats(UserQuit, false);
                    if (stats != null)
                    {
                        stats.lastaction = DateTime.Now;
                        stats.lastmessage = "quitting with the message \"" + QuitMessage + Module1.StopChar + "\".";
                        stats.quits++;
                        if (QuitMessage.StartsWith("Ping timeout:") | QuitMessage.StartsWith("Quit: No Ping reply in") | QuitMessage == "Quit: CGI:IRC (Ping timeout)")
                            stats.pingquits++;
                        else if (QuitMessage.StartsWith("Quit:"))
                            stats.userquits++;
                        stats.refcount--;
                        if (stats.refcount == 0)
                        {
                            stats.onlinetimer.Stop();
                            stats.onlinesaved = stats.onlinetime;
                            stats.onlinetimer.Reset();
                        }
                    }
                }
            if (UserQuit == IrcObject.IrcNick)
                foreach (IRCChannel item in IrcObject.IrcChannels)
                    foreach (IRCChanUserStats stat in item.stats)
                    {
                        stat.onlinetimer.Stop();
                        stat.onlinesaved = stat.onlinetime;
                        stat.onlinetimer.Reset();
                        stat.refcount = 0;
                    }
        }

        internal void ReceiveMessage(IRC IrcObject, string User, string Channel, string Message)
        {
            if (Channel.Equals(IrcObject.IrcNick, StringComparison.OrdinalIgnoreCase))
                Channel = User;
            IRCChannel chobj = IrcObject.GetChannel(Channel);
            if (chobj == null || chobj.GetUser(User) == null)
            {
                foreach (string item in Module1.IgnoreList)
                    if (User.Like(item))
                        return;
            }
            else
            {
                foreach (string item in Module1.IgnoreList)
                    if (chobj.GetUser(User).Mask.Like(item))
                        return;
                ReadNotes(IrcObject, Channel, User, false);
            }
            if (Message.StartsWith(Module1.CTCPChar.ToString()))
            {
                if (Message.Equals(Module1.CTCPChar + "version" + Module1.CTCPChar, StringComparison.OrdinalIgnoreCase))
                {
                    string Version = "MMBot - C# .NET 4.0 ";
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT | Environment.OSVersion.Platform == PlatformID.Win32Windows)
                        Version += CSharp411.OSInfo.Name + " " + CSharp411.OSInfo.Edition + " " + CSharp411.OSInfo.ServicePack;
                    else
                        Version += Environment.OSVersion.VersionString;
                    IrcObject.QueueWrite("NOTICE " + User + " :" + Module1.CTCPChar + "VERSION " + Version + Module1.CTCPChar);
                }
                if (Message.Equals(Module1.CTCPChar + "time" + Module1.CTCPChar, StringComparison.OrdinalIgnoreCase))
                    IrcObject.QueueWrite("NOTICE " + User + " :" + Module1.CTCPChar + "TIME " + DateTime.Now.ToString() + Module1.CTCPChar);
                if (Message.StartsWith(Module1.CTCPChar + "ping", StringComparison.OrdinalIgnoreCase))
                    IrcObject.QueueWrite("NOTICE " + User + " :" + Message);
                if (Message.Equals(Module1.CTCPChar + "source" + Module1.CTCPChar, StringComparison.OrdinalIgnoreCase))
					IrcObject.QueueWrite("NOTICE " + User + " :" + Module1.CTCPChar + "SOURCE https://github.com/MainMemory/MMBot/" + Module1.CTCPChar);
                if (Message.Equals(Module1.CTCPChar + "avatar" + Module1.CTCPChar, StringComparison.OrdinalIgnoreCase))
                    IrcObject.QueueWrite("NOTICE " + User + " :" + Module1.CTCPChar + "AVATAR http://i764.photobucket.com/albums/xx282/sonicmike2/MMBotAvatar.png 640" + Module1.CTCPChar);
                if (Message.ToLowerInvariant().Trim(Module1.CTCPChar).StartsWith("action"))
                    Module1.WriteOutput(IrcObject.name + "\\" + Channel, string.Format(Module1.ColorChar + "18* {0}" + Module1.ColorChar + " {1}", User, Message.Remove(0, 8).TrimEnd(Module1.CTCPChar)), true);
                else
                    Module1.WriteOutput(IrcObject.name + "\\" + Channel, string.Format("* Received a CTCP {0} from {1}", Message.Trim(Module1.CTCPChar), User), false);
            }
            else if (chobj == null || chobj.GetUser(User) == null)
                Module1.WriteOutput(IrcObject.name + "\\" + Channel, string.Format(Module1.ColorChar + "18<{0}>" + Module1.ColorChar + " {1}", User, Message), true);
            else
                Module1.WriteOutput(IrcObject.name + "\\" + Channel, string.Format(Module1.ColorChar + "18<{2}{0}>" + Module1.ColorChar + " {1}", User, Message, chobj.GetUser(User).GetModeChar()), true);
            if (chobj != null)
            {
                if (chobj.GetUserStats(User, false) == null)
                    chobj.stats.Add(new IRCChanUserStats(User));
                chobj.GetUserStats(User, false).lastaction = DateTime.Now;
                if (Message.StartsWith(Module1.CTCPChar + "action", StringComparison.OrdinalIgnoreCase))
                {
                    chobj.GetUserStats(User, false).actions++;
                    chobj.GetUserStats(User, false).lastmessage = "saying \"* " + User + " " + Message.Remove(0, 8).TrimEnd(Module1.CTCPChar) + Module1.StopChar + "\".";
                }
                else
                {
                    chobj.GetUserStats(User, false).messages++;
                    chobj.GetUserStats(User, false).words += (ulong)Message.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Length;
                    chobj.GetUserStats(User, false).characters += (ulong)Message.Length;
                    chobj.GetUserStats(User, false).lastmessage = "saying \"" + Message + Module1.StopChar + "\".";
                }
            }
            if (Message.StartsWith(Module1.cmdchar))
                BotCommand(IrcObject, User, Channel, Message.Remove(0, Module1.cmdchar.Length));
            else if (Message.Strip().Split(' ')[0].TrimEnd(':').TrimEnd(',').Equals(IrcObject.IrcNick, StringComparison.OrdinalIgnoreCase))
                BotCommand(IrcObject, User, Channel, Module1.Recombine(Message.Split(' '), 1));
            else if (Channel == User)
                BotCommand(IrcObject, User, Channel, Message);
            else if (chobj != null && chobj.linkcheck != LinkCheckMode.Off)
            {
                int numlinks = 0;
                foreach (string item in Message.Split(' '))
                {
                    string item2 = item.Trim(Module1.CTCPChar).Strip();
                    if (item2.StartsWith("http://") || item2.StartsWith("https://"))
                    {
                        new System.Threading.Thread(Module1.LinkCheck).Start(new LinkCheckParams(IrcObject, Channel, item2, IrcObject.GetChannel(Channel).linkcheck == LinkCheckMode.On));
                        numlinks++;
                        if (numlinks == 3) break;
                    }
                }
            }
        }

        internal void ReceiveNotice(IRC IrcObject, string User, string Channel, string Message)
        {
            if (Channel.Equals(IrcObject.IrcNick, StringComparison.OrdinalIgnoreCase))
                Channel = User;
            IRCChannel chobj = IrcObject.GetChannel(Channel);
            if (chobj == null || chobj.GetUser(User) == null)
            {
                foreach (string item in Module1.IgnoreList)
                    if (User.Like(item))
                        return;
            }
            else
            {
                foreach (string item in Module1.IgnoreList)
                    if (chobj.GetUser(User).Mask.Like(item))
                        return;
            }
            if (!Message.StartsWith(Module1.CTCPChar.ToString()))
                Module1.WriteOutput(IrcObject.name + "\\" + Channel, string.Format(Module1.ColorChar + "28-" + Module1.ColorChar + "29{0}" + Module1.ColorChar + "28-" + Module1.ColorChar + " {1}", User, Message), false);
            else
            {
                Message = Message.Trim(Module1.CTCPChar);
                Module1.WriteOutput(IrcObject.name + "\\" + Channel, string.Format(Module1.ColorChar + "28-" + Module1.ColorChar + "29{0}" + Module1.ColorChar + "28-" + Module1.ColorChar + " {1}", User, Message), false);
                switch (Message.Split(' ')[0].ToUpper())
                {
                    case "PING":
                        IrcObject.SendNotice("Your reply time is: " + (DateTime.Now - System.DateTime.FromBinary(unchecked((long)ulong.Parse(Message.Split(' ')[1])))).ToStringCustM(), User);
                        break;
                }
            }
        }

        internal void UnknownCommand(IRC IrcObject, string Command)
        {
            Module1.WriteOutput(IrcObject.name, string.Format("Unknown Command: {0}", Command), true);
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        internal void BotCommand(IRC IrcObject, string User, string Channel, string CommandString)
        {
            if (Channel == IrcObject.IrcNick)
                Channel = User;
            IRCChannel ChanObj = IrcObject.GetChannel(Channel, true, User);
            if (!Module1.enabled & !User.Equals(Module1.OpName, StringComparison.CurrentCultureIgnoreCase))
                return;
            if (ChanObj.WaitTimer.Enabled & !User.Equals(Module1.OpName, StringComparison.CurrentCultureIgnoreCase))
            {
                IrcObject.QueueWrite("KICK " + Channel + " " + User + " :Stop flooding!");
                return;
            }
            if (CommandString.StartsWith("/") & User.Equals(Module1.OpName, StringComparison.CurrentCultureIgnoreCase))
            {
                IrcCommand(IrcObject, Channel, CommandString.Substring(1));
                return;
            }
            foreach (string item in Module1.BanList)
            {
                if (ChanObj.GetUser(User).Mask.Like(item))
                {
                    IrcObject.WriteMessage("No u", Channel);
                    return;
                }
            }
            string[] Command = CommandString.Split(' ');
            bool valid = true;
            try
            {
                if (CommandDictionary.ContainsKey(Command[0].Strip().ToLowerInvariant()))
                {
                    BotCommand cmd = CommandDictionary[Command[0].Strip().ToLowerInvariant()];
                    int i = 1;
                    while (i < Command.Length && cmd.SubCommands.ContainsKey(Command[i].Strip().ToLowerInvariant()))
                        cmd = cmd.SubCommands[Command[i++].Strip().ToLowerInvariant()];
                    Module1.CheckAccessLevel(ChanObj.AccessLevels.GetValueOrDefault(string.Join(" ", Command, 0, i).ToLowerInvariant().Strip(), cmd.AccessLevel), ChanObj.GetUser(User));
                    if (Command.Length - i < cmd.CMDMinLength || cmd.CMDMinLength == -1)
                        IrcObject.SendNotice("Incorrect parameters for command. Use " + Module1.UnderChar + "!help " + string.Join(" ", Command, 0, i) + Module1.UnderChar + " for help.", User);
                    else
                    {
                        string commandstr = string.Empty;
                        if (i < Command.Length)
                            commandstr = string.Join(" ", Command, i, Command.Length - i);
                        commandInvokeInfo info = new commandInvokeInfo(cmd.Function, IrcObject, Channel, User, commandstr);
                        if (cmd.SeparateThread)
                            new System.Threading.Thread(StartCommand).Start(info);
                        else
                            StartCommand(info);
                    }
                }
                else
                    valid = false;
                if (valid)
                {
                    if (ChanObj.GetUserStats(User, false) != null)
                        ChanObj.GetUserStats(User, false).commands += 1;
                    if (!User.Equals(Module1.OpName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (!ChanObj.CommandTimer.Enabled)
                        {
                            ChanObj.CommandTimer.Start();
                        }
                        ChanObj.Consecutivecmds += 1;
                        if (ChanObj.Consecutivecmds == 3)
                        {
                            IrcObject.WriteMessage("Command limit reached! Wait 10 seconds...", Channel);
                            IrcObject.QueueWrite("KICK " + Channel + " " + User + " :Stop flooding!");
                            ChanObj.WaitTimer.Start();
                        }
                    }
                }
                /*else
                {
                    if (Channel.StartsWith("#"))
                        IrcObject.WriteMessage("'" + Command[0] + "' is not recognized as an internal or external command, operable program or batch file.", Channel);
                }*/
            }
            catch (CommandAccessException ex) { if (ChanObj.displayaccesserror) IrcObject.WriteMessage(Module1.ColorChar + "04Error: " + ex.Message, Channel); }
#if !DEBUG
            catch (Exception ex)
            {
                stacktrace = ex.StackTrace;
                IrcObject.WriteMessage(Module1.ColorChar + "04", ex.GetType().Name + " in " + ex.Source + ": " + ex.Message, Channel);
            }
#endif
        }

        private struct commandInvokeInfo
        {
            public BotCommandFunc func;
            public IRC IrcObject;
            public string channel, user, command;

            public commandInvokeInfo(BotCommandFunc func, IRC IrcObject, string channel, string user, string command)
            {
                this.func = func;
                this.IrcObject = IrcObject;
                this.channel = channel;
                this.user = user;
                this.command = command;
            }
        }

        private void StartCommand(object param)
        {
            commandInvokeInfo info = (commandInvokeInfo)param;
            try { info.func(info.IrcObject, info.channel, info.user, info.command); }
            catch (CommandAccessException ex) { info.IrcObject.WriteMessage(Module1.ColorChar + "04Error: " + ex.Message, info.channel); }
#if !DEBUG
            catch (Exception ex)
            {
                stacktrace = ex.StackTrace;
                info.IrcObject.WriteMessage(Module1.ColorChar + "04", ex.GetType().Name + " in " + ex.Source + ": " + ex.Message, info.channel);
            }
#endif
        }

        public BotCommand? GetCommand(string command)
        {
            string[] Command = command.Split(' ');
            if (CommandDictionary.ContainsKey(Command[0].Strip().ToLowerInvariant()))
            {
                BotCommand cmd = CommandDictionary[Command[0].Strip().ToLowerInvariant()];
                int i = 1;
                while (i < Command.Length && cmd.SubCommands.ContainsKey(Command[i].Strip().ToLowerInvariant()))
                    cmd = cmd.SubCommands[Command[i++].Strip().ToLowerInvariant()];
                if (i == Command.Length)
                    return cmd;
            }
            return null;
        }

        public BotCommand[] GetCommands(string command)
        {
            List<BotCommand> commands = new List<BotCommand>();
            string[] Command = command.Split(' ');
            if (CommandDictionary.ContainsKey(Command[0].Strip().ToLowerInvariant()))
            {
                BotCommand cmd = CommandDictionary[Command[0].Strip().ToLowerInvariant()];
                commands.Add(cmd);
                int i = 1;
                while (i < Command.Length && cmd.SubCommands.ContainsKey(Command[i].Strip().ToLowerInvariant()))
                {
                    cmd = cmd.SubCommands[Command[i++].Strip().ToLowerInvariant()];
                    commands.Add(cmd);
                }
            }
            return commands.ToArray();
        }

        internal string stacktrace = "No errors.";

        public void IrcCommand(IRC IrcObject, string Channel, string CommandString)
        {
            string[] Command = CommandString.Split(' ');
            switch (Command[0].ToLower())
            {
                case "me":
                    IrcObject.WriteMessage(Module1.CTCPChar + "ACTION " + Module1.Recombine(Command, 1) + Module1.CTCPChar, Channel);
                    break;
                case "msg":
                    IrcObject.WriteMessage(Module1.Recombine(Command, 2), Command[1]);
                    break;
                case "say":
                    IrcObject.WriteMessage(Module1.Recombine(Command, 1), Channel);
                    break;
                case "kick":
                    if (Command.Length > 1)
                    {
                        string kickstr = null;
                        if (Command[1].StartsWith("#"))
                        {
                            kickstr = "KICK " + Command[1] + " " + Command[2];
                            if (Command.Length > 3)
                                kickstr += " :" + Module1.Recombine(Command, 3);
                        }
                        else
                        {
                            kickstr = "KICK " + Channel + " " + Command[1];
                            if (Command.Length > 2)
                                kickstr += " :" + Module1.Recombine(Command, 2);
                        }
                        IrcObject.QueueWrite(kickstr);
                    }
                    break;
                case "clear":
                    Module1.myForm.Invoke(Module1.myForm.ClearScrollbackDelegate, Channel);
                    break;
                case "allchan":
                    System.Collections.ObjectModel.Collection<string> lolchans = new System.Collections.ObjectModel.Collection<string>();
                    foreach (IRCChannel chan in IrcObject.IrcChannels)
                        if (chan.Active)
                            lolchans.Add(chan.Name);
                    foreach (string chan in lolchans)
                        IrcCommand(IrcObject, chan, Module1.Recombine(Command, 1).Replace("<chan>", chan));
                    break;
                case "timer":
                    if (Command.Length == 1)
                        if (Module1.timers.Count == 0)
                            Module1.WriteOutput(IrcObject.name + "\\" + Channel, "No timers installed.", true);
                        else
                        {
                            Module1.WriteOutput(IrcObject.name + "\\" + Channel, Module1.RevChar + " Ref#  Seconds  Repeat  Command " + Module1.RevChar, true);
                            for (int i = 0; i <= Module1.timers.Count - 1; i++)
                                Module1.WriteOutput(IrcObject.name + "\\" + Channel, string.Format(" {0,4} {1,8} {2,7} {3}", i + 1, Module1.timers[i].timer.Interval / 1000, Module1.timers[i].repeats, Module1.timers[i].action), true);
                        }
                    else
                        switch (Command[1].ToLower())
                        {
                            case "-repeat":
                                Module1.timers.Add(new TimerInfo(double.Parse(Command[3]), Module1.Recombine(Command, 4), int.Parse(Command[2]), IrcObject, Channel));
                                break;
                            case "-delete":
                                if (Module1.timers.Count <= Convert.ToInt32(Command[2]))
                                {
                                    Module1.timers.RemoveAt(int.Parse(Command[2]) - 1);
                                    Module1.WriteOutput(IrcObject.name + "\\" + Channel, "Timer " + Command[2] + " deleted.", true);
                                }
                                break;
                            default:
                                Module1.timers.Add(new TimerInfo(double.Parse(Command[1]), Module1.Recombine(Command, 2), 1, IrcObject, Channel));
                                break;
                        }
                    break;
                case "names":
                    if (Command.Length > 1)
                        IrcObject.GetChannel(Command[1]).People.Clear();
                    else
                        IrcObject.GetChannel(Channel).People.Clear();
                    IrcObject.QueueWrite(CommandString);
                    break;
                default:
                    IrcObject.QueueWrite(CommandString);
                    break;
            }
        }

        public Dictionary<string, BotCommand> CommandDictionary { get; private set; }
        internal Dictionary<string, BotModule> Modules { get; set; }
    }

    public abstract class BotModule
    {
        public bool LoadFailed { get; protected set; }

        public abstract void Shutdown();

        public virtual void Save() { }

        private string oldDir;
        private static object lockObj = new object();

        protected void ChangeDirectory()
        {
            System.Threading.Monitor.Enter(lockObj);
            if (oldDir != null) return;
            oldDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        }

        protected void RestoreDirectory()
        {
            if (oldDir != null)
                Environment.CurrentDirectory = oldDir;
            oldDir = null;
            System.Threading.Monitor.Exit(lockObj);
        }
    }

    public class CommonModule : BotModule
    {
        public override void Shutdown() { }

        internal void HelpCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                IrcObject.SendNotice("Help: http://" + HttpServer.Hostname + "/?page=help", user);
                IrcObject.SendNotice(convert_help_string(Module1.IrcApp.CommandDictionary["help"].HelpText), user);
                List<string> commandNames = new List<string>();
                foreach (BotCommand item in Module1.IrcApp.CommandDictionary.Values)
                    commandNames.Add(item.Name);
                IrcObject.SendNotice("Commands: " + string.Join(", ", commandNames), user);
                return;
            }
            string[] split = command.Split(' ');
            if (Module1.IrcApp.CommandDictionary.ContainsKey(split[0].Strip().ToLowerInvariant()))
            {
                BotCommand cmd = Module1.IrcApp.CommandDictionary[split[0].ToLowerInvariant()];
                int i = 1;
                while (i < split.Length && cmd.SubCommands.ContainsKey(split[i].Strip().ToLowerInvariant()))
                    cmd = cmd.SubCommands[split[i++].Strip().ToLowerInvariant()];
                IrcObject.SendNotice("Help for \"" + string.Join(" ", split, 0, i) + "\": http://" + HttpServer.Hostname + "/?page=help&command=" + Uri.EscapeDataString(string.Join(" ", split, 0, i)), user);
                IrcObject.SendNotice(convert_help_string(cmd.HelpText), user);
                if (cmd.SubCommands.Count > 0)
                {
                    List<string> commandNames = new List<string>();
                    foreach (BotCommand item in cmd.SubCommands.Values)
                        commandNames.Add(item.Name);
                    IrcObject.SendNotice("Subcommands of \"" + string.Join(" ", split, 0, i) + "\": " + string.Join(", ", commandNames), user);
                }
                else
                    IrcObject.SendNotice("Subcommands of \"" + string.Join(" ", split, 0, i) + "\": No subcommands.", user);
            }
            else
                IrcObject.SendNotice("Command " + split[0] + " not found.", user);
            return;
        }

        private string convert_help_string(string message)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                if (message.SafeSubstring(i, 2) == "<c")
                {
                    output.Append(Module1.ColorChar);
                    if (message.SafeSubstring(i, 3) == "<c=")
                    {
                        string[] colors = message.Substring(i + 3, message.IndexOf('>', i) - (i + 3)).Split(',');
                        output.Append(string.Join(",", colors));
                    }
                    i = message.IndexOf('>', i);
                }
                else if (message.SafeSubstring(i, 3) == "<r>")
                {
                    i += 2;
                    output.Append(Module1.RevChar);
                }
                else if (message.SafeSubstring(i, 3) == "<o>")
                {
                    i += 2;
                    output.Append(Module1.StopChar);
                }
                else if (message.SafeSubstring(i, 3) == "<b>")
                {
                    i += 2;
                    output.Append(Module1.BoldChar);
                }
                else if (message.SafeSubstring(i, 3) == "<u>")
                {
                    i += 2;
                    output.Append(Module1.UnderChar);
                }
                else if (message.SafeSubstring(i, 3) == "<i>")
                {
                    i += 2;
                    output.Append(Module1.ItalicChar);
                }
                else if (message.SafeSubstring(i, 5) == "<irc>")
                    i += 4;
                else if (message.SafeSubstring(i, 6) == "</irc>")
                    i += 5;
                else if (message.SafeSubstring(i, 5) == "<web>")
                {
                    int j = message.IndexOf("</web>", i + 5);
                    if (j == -1)
                        return output.ToString();
                    i = j + 5;
                }
                else
                    output.Append(message[i]);
            }
            return output.ToString();
        }

        void NickCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!Module1.enabled)
                if (!command.ToLower().Strip().EndsWith("|off"))
                    command += "|Off";
            IrcObject.QueueWrite("NICK " + command.Strip());
        }

        void BanCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.Strip();
            if (IrcObject.GetChannel(channel) != null && IrcObject.GetChannel(channel).GetUser(command) != null)
                command = IrcObject.GetChannel(channel).GetUser(command).Mask;
            if (Module1.BanList.IndexOf(command) == -1)
            {
                Module1.BanList.Add(command);
                foreach (IRCChannel chan in IrcObject.IrcChannels)
                    foreach (IRCUser userr in chan.GetUsersByMask(command))
                        IrcObject.WriteMessage(userr.name + " can not use commands.", chan.Name);
            }
        }

        void BanlistCommand(IRC IrcObject, string channel, string user, string command)
        {
            foreach (string name in Module1.BanList)
                IrcObject.SendNotice(name, user);
        }

        void UnbanCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.Strip();
            if (IrcObject.GetChannel(channel) != null && IrcObject.GetChannel(channel).GetUser(command) != null)
                command = IrcObject.GetChannel(channel).GetUser(command).Mask;
            if (Module1.BanList.IndexOf(command) > -1)
            {
                Module1.BanList.Remove(command);
                foreach (IRCChannel chan in IrcObject.IrcChannels)
                    foreach (IRCUser userr in chan.GetUsersByMask(command))
                        IrcObject.WriteMessage(userr.name + " can use commands again.", chan.Name);
            }
        }

        void VopAddCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            command = command.Strip();
            if (ChanObj.GetUser(command) != null)
                command = ChanObj.GetUser(command).Mask;
            if (ChanObj.VOP.IndexOf(command) == -1)
            {
                ChanObj.VOP.Add(command);
                foreach (IRCUser item in ChanObj.GetUsersByMask(command))
                    IrcObject.QueueWrite("MODE " + channel + " +v " + item.name);
            }
        }

        void VopDelCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            command = command.Strip();
            if (ChanObj.GetUser(command) != null)
                command = ChanObj.GetUser(command).Mask;
            if (ChanObj.VOP.IndexOf(command) > -1)
                ChanObj.VOP.Remove(command);
        }

        void VopListCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            foreach (string name in ChanObj.VOP)
                IrcObject.SendNotice(name, user);
        }

        void HopAddCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            command = command.Strip();
            if (ChanObj.GetUser(command) != null)
                command = ChanObj.GetUser(command).Mask;
            if (ChanObj.HOP.IndexOf(command) == -1)
            {
                ChanObj.HOP.Add(command);
                foreach (IRCUser item in ChanObj.GetUsersByMask(command))
                    IrcObject.QueueWrite("MODE " + channel + " +h " + item.name);
            }
        }

        void HopDelCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            command = command.Strip();
            if (ChanObj.GetUser(command) != null)
                command = ChanObj.GetUser(command).Mask;
            if (ChanObj.HOP.IndexOf(command) > -1)
                ChanObj.HOP.Remove(command);
        }

        void HopListCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            foreach (string name in ChanObj.HOP)
                IrcObject.SendNotice(name, user);
        }

        void AopAddCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            command = command.Strip();
            if (ChanObj.GetUser(command) != null)
                command = ChanObj.GetUser(command).Mask;
            if (ChanObj.AOP.IndexOf(command) == -1)
            {
                ChanObj.AOP.Add(command);
                foreach (IRCUser item in ChanObj.GetUsersByMask(command))
                    IrcObject.QueueWrite("MODE " + channel + " +o " + item.name);
            }
        }

        void AopDelCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            command = command.Strip();
            if (ChanObj.GetUser(command) != null)
                command = ChanObj.GetUser(command).Mask;
            if (ChanObj.AOP.IndexOf(command) > -1)
                ChanObj.AOP.Remove(command);
        }

        void AopListCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            foreach (string name in ChanObj.AOP)
                IrcObject.SendNotice(name, user);
        }

        void FeedAddCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            command = command.Strip();
            ChanObj.feeds.Add(new IRCChannel.FeedInfo(command));
            IrcObject.WriteMessage("Feed added!", channel);
        }

        void FeedDelCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            command = command.Strip();
            foreach (IRCChannel.FeedInfo feed in ChanObj.feeds)
                if (feed.url.Equals(command, StringComparison.OrdinalIgnoreCase) || (feed.title != null && feed.title.Equals(command, StringComparison.OrdinalIgnoreCase)))
                {
                    ChanObj.feeds.Remove(feed);
                    IrcObject.WriteMessage("Feed deleted.", channel);
                    break;
                }
        }

        void FeedResetCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            command = command.Strip();
            for (int i = 0; i < ChanObj.feeds.Count; i++)
            {
                IRCChannel.FeedInfo feed = ChanObj.feeds[i];
                if (feed.url.Equals(command, StringComparison.OrdinalIgnoreCase) || (feed.title != null && feed.title.Equals(command, StringComparison.OrdinalIgnoreCase)))
                {
                    ChanObj.feeds[i] = new IRCChannel.FeedInfo(feed.url);
                    IrcObject.WriteMessage("Feed reset.", channel);
                    break;
                }
            }
        }

        void FeedListCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            foreach (IRCChannel.FeedInfo feed in ChanObj.feeds)
                IrcObject.QueueWrite("NOTICE " + user + " :" + feed.url + (feed.title == null ? "" : " - " + feed.title));
        }

        void FeedIntervalCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                IRCChannel ChanObj = IrcObject.GetChannel(channel, true, user);
                Module1.CheckAccessLevel(UserModes.BotOp, ChanObj.GetUser(user));
                Module1.feedtimer.Interval = Module1.GetTimeSpan(command).Value.Milliseconds;
            }
            IrcObject.WriteMessage(TimeSpan.FromMilliseconds(Module1.feedtimer.Interval).ToStringCustM(), channel);
        }

        void FeedLastCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel, true, user);
            Uri ur = null;
            if (!Uri.TryCreate(command.Strip(), UriKind.Absolute, out ur))
                foreach (IRCChannel.FeedInfo feed in ChanObj.feeds)
                    if (feed.title != null && feed.title.Equals(command, StringComparison.CurrentCultureIgnoreCase))
                        ur = new Uri(feed.url);
            if (ur == null)
            {
                IrcObject.WriteMessage("Invalid feed!", channel);
                return;
            }
            XmlReader r = null;
            for (int trycnt = 0; trycnt < 3; trycnt++)
            {
                try
                {
                    r = XmlReader.Create(ur.ToString());
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
                Array.Sort(feed.Entries);
                AtomFeedEntry entry = feed.Entries[feed.Entries.Length - 1];
                string author = entry.Author != null ? entry.Author.Name : "";
                string ititle = System.Web.HttpUtility.HtmlDecode(entry.Title.Text).Replace("\r", " ").Replace("\n", " ").TrimExcessSpaces().Trim();
                Uri url = new Uri(ur, entry.Link.URL);
                IrcObject.WriteMessage(Module1.UnderChar + feed.Title.Text + Module1.UnderChar + ": " + Module1.UnderChar + ititle + Module1.UnderChar + (string.IsNullOrEmpty(author) ? "" : " by " + Module1.UnderChar + author + Module1.UnderChar) + ": " + url.ToString(), channel);
            }
            else
            {
                RssFeed feed = (RssFeed)xs.Deserialize(r);
                r.Close();
                Array.Sort(feed.Channel.Entries);
                RssFeedEntry entry = feed.Channel.Entries[feed.Channel.Entries.Length - 1];
                string author = entry.Author != null ? entry.Author : (entry.Creator != null ? entry.Creator : "");
                string ititle = System.Web.HttpUtility.HtmlDecode(entry.Title).Replace("\r", " ").Replace("\n", " ").TrimExcessSpaces().Trim();
                Uri url = new Uri(ur, entry.Link);
                IrcObject.WriteMessage(Module1.UnderChar + feed.Channel.Title + Module1.UnderChar + ": " + Module1.UnderChar + ititle + Module1.UnderChar + (string.IsNullOrEmpty(author) ? "" : " by " + Module1.UnderChar + author + Module1.UnderChar) + ": " + url.ToString(), channel);
            }
        }

        void LinkcheckCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel, true, user);
            if (!string.IsNullOrEmpty(command))
            {
                Module1.CheckAccessLevel(UserModes.Admin, ChanObj.GetUser(user));
                ChanObj.linkcheck = (LinkCheckMode)Enum.Parse(typeof(LinkCheckMode), command.Strip(), true);
            }
            IrcObject.WriteMessage("Linkchecker mode is " + ChanObj.linkcheck + ".", channel);
        }

        void LinkinfoCommand(IRC IrcObject, string channel, string user, string command)
        {
            string item2 = command.Strip();
            if (item2.StartsWith("http://") || item2.StartsWith("https://"))
                new System.Threading.Thread(Module1.LinkCheck).Start(new LinkCheckParams(IrcObject, channel, item2, true));
        }

        void CalcCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (command.Strip().Equals("two plus two", StringComparison.OrdinalIgnoreCase))
            {
                IrcObject.WriteMessage("Two plus two is...", channel);
                System.Threading.Thread.Sleep(1000);
                IrcObject.WriteMessage("Ten.", channel);
                IrcObject.WriteMessage("IN BASE FOUR I'M FINE!", channel);
                return;
            }
            int outbase = 10;
            string[] split = command.Split(' ');
            if (split[0].Equals("/base", StringComparison.OrdinalIgnoreCase))
            {
                outbase = int.Parse(split[1], System.Globalization.NumberStyles.None, System.Globalization.NumberFormatInfo.InvariantInfo);
                command = string.Join(" ", split, 2, split.Length - 2);
            }
            Microsoft.CSharp.CSharpCodeProvider cd = new Microsoft.CSharp.CSharpCodeProvider();
            string uid = DateTime.UtcNow.Ticks.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
            System.CodeDom.Compiler.CompilerParameters cp = new System.CodeDom.Compiler.CompilerParameters { OutputAssembly = "MMBotCode" + uid + ".dll" };
            string vars = string.Empty;
            foreach (KeyValuePair<string, string> v in Module1.Vars)
                vars += "private static double " + v.Key.Split(';')[0] + " = " + v.Value.Split(';')[0] + ";\n";
            string source = Properties.Resources.Calc.Replace("CustomCode/*uid*/", "CustomCode" + uid).Replace("//message", command.Split(';')[0]).Replace("//variables", vars);
            if (source.Contains("=>") | source.Contains("/*") | source.Contains("*/") | source.Contains("//"))
            {
                IrcObject.WriteMessage(Module1.ColorChar + "04NO U!", channel);
                return;
            }
            System.CodeDom.Compiler.CompilerResults cmpres = cd.CompileAssemblyFromSource(cp, source);
            int errs = 0;
            for (int er = 0; er < cmpres.Errors.Count; er++)
            {
                if (!cmpres.Errors[er].IsWarning)
                {
                    IrcObject.WriteMessage(Module1.ColorChar + "04Error " + cmpres.Errors[er].ErrorNumber + " at column " + cmpres.Errors[er].Column + ": " + cmpres.Errors[er].ErrorText, channel);
                    errs++;
                }
                if (errs == 5) break;
            }
            if (errs > 0)
                return;
            AppDomain MMBotCodeDomain;
            if (!Module1.isMonoRuntime)
                MMBotCodeDomain = GetSecureDomain();
            else
                MMBotCodeDomain = AppDomain.CreateDomain("MMBotCodeDomain");
            System.Reflection.Assembly assembly = MMBotCodeDomain.Load(File.ReadAllBytes(cmpres.PathToAssembly));
            Type typ = assembly.GetType("MMBotCodeClass");
            System.Reflection.MethodInfo CustomCodeFunc = typ.GetMethod("CustomCode" + uid, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.IgnoreCase, null, System.Reflection.CallingConventions.Standard, System.Type.EmptyTypes, null);
            CodeRunner asdfghjkl = new CodeRunner();
            asdfghjkl.CustomCodeDelegate = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), null, CustomCodeFunc);
            try
            {
                MMBotCodeDomain.DoCallBack(new CrossAppDomainDelegate(asdfghjkl.CustomCodeRunner));
                AppDomain.Unload(MMBotCodeDomain);
                dynamic result = asdfghjkl.CustomCodeResult;
                result = CalcResultHelper(outbase, Type.GetTypeCode(asdfghjkl.CustomCodeResult.GetType()), result);
                IrcObject.WriteMessage(result.ToString(), channel);
                Module1.Vars["ans"] = Convert.ToString(result);
            }
            catch (System.Security.SecurityException)
            {
                AppDomain.Unload(MMBotCodeDomain);
                IrcObject.WriteMessage(Module1.ColorChar + "04NO U!", channel);
            }
        }

        string CalcResultHelper(int outbase, TypeCode typecode, dynamic result)
        {
            switch (typecode)
            {
                case TypeCode.Boolean:
                    result = Convert.ToInt32(result);
                    goto case TypeCode.Int32;
                case TypeCode.Byte:
                    result = (byte)result;
                    return (outbase == 16 ? "0x" : "") + ((byte)result).ToBase(outbase);
                case TypeCode.Char:
                    result = (ushort)result;
                    goto case TypeCode.UInt16;
                case TypeCode.DateTime:
                    result = result.ToBinary();
                    goto case TypeCode.Int64;
                case TypeCode.Decimal:
                    result = (decimal)result;
                    return (outbase == 16 ? "0x" : "") + ((decimal)result).ToBase(outbase);
                case TypeCode.Double:
                    result = (double)result;
                    if (double.IsInfinity(result) | double.IsNaN(result)) outbase = 10;
                    return (outbase == 16 ? "0x" : "") + ((double)result).ToBase(outbase);
                case TypeCode.DBNull:
                case TypeCode.Empty:
                    result = 0;
                    goto case TypeCode.Int32;
                case TypeCode.Int16:
                    result = (short)result;
                    return (outbase == 16 ? "0x" : "") + ((short)result).ToBase(outbase);
                case TypeCode.Int32:
                    result = (int)result;
                    return (outbase == 16 ? "0x" : "") + ((int)result).ToBase(outbase);
                case TypeCode.Int64:
                    result = (long)result;
                    return (outbase == 16 ? "0x" : "") + ((long)result).ToBase(outbase);
                case TypeCode.SByte:
                    result = (sbyte)result;
                    return (outbase == 16 ? "0x" : "") + ((sbyte)result).ToBase(outbase);
                case TypeCode.Single:
                    result = (float)result;
                    if (float.IsInfinity(result) | float.IsNaN(result)) outbase = 10;
                    return (outbase == 16 ? "0x" : "") + ((float)result).ToBase(outbase);
                case TypeCode.UInt16:
                    result = (ushort)result;
                    return (outbase == 16 ? "0x" : "") + ((ushort)result).ToBase(outbase);
                case TypeCode.UInt32:
                    result = (uint)result;
                    return (outbase == 16 ? "0x" : "") + ((uint)result).ToBase(outbase);
                case TypeCode.UInt64:
                    result = (ulong)result;
                    return (outbase == 16 ? "0x" : "") + ((ulong)result).ToBase(outbase);
                default:
                    if (result is System.Collections.IEnumerable)
                    {
                        StringBuilder output = new StringBuilder("{");
                        bool first = true;
                        foreach (object o in (System.Collections.IEnumerable)result)
                        {
                            if (!first)
                                output.Append(",");
                            output.Append(" " + CalcResultHelper(outbase, Type.GetTypeCode(o.GetType()), o));
                            first = false;
                        }
                        return output + " }";
                    }
                    else if (outbase == 10)
                    {
                        double ans = Convert.ToDouble(result);
                        return ans.ToLongString();
                    }
                    else if (outbase == 16)
                        return "0x" + Module1.BaseConv(result.ToString(), 10, outbase);
                    else
                        return Module1.BaseConv(result.ToString(), 10, outbase);
            }
        }

        void CalchCommand(IRC IrcObject, string channel, string user, string command)
        {
            CalcCommand(IrcObject, channel, user, "/base 16 " + command);
        }

        private static AppDomain GetSecureDomain()
        {
            Evidence ev = new Evidence();
            ev.AddHostEvidence(new Zone(SecurityZone.Internet));
            PermissionSet internetPS = SecurityManager.GetStandardSandbox(ev);
            AppDomainSetup adSetup = new AppDomainSetup();
            adSetup.ApplicationBase = Environment.CurrentDirectory;
            AppDomain MMBotCodeDomain = AppDomain.CreateDomain("MMBotCodeDomain", ev, adSetup, internetPS);
            return MMBotCodeDomain;
        }

        void TimecalcCommand(IRC IrcObject, string channel, string user, string command)
        {
            string expr = command;
            string conv = null;
            if (command.Contains(" in "))
            {
                string[] split = command.Split(new string[] { " in " }, StringSplitOptions.None);
                expr = split[0];
                conv = split[1];
            }
            object result = Module1.timecalc(expr);
            string strres;
            if (result is DateTime)
            {
                DateTime t = (DateTime)result;
                if (!string.IsNullOrEmpty(conv))
                    t = TimeZoneInfo.ConvertTimeFromUtc(t, TimeZoneInfo.FindSystemTimeZoneById(conv));
                strres = t.ToString("F");
            }
            else if (result is TimeSpan)
            {
                TimeSpan t = (TimeSpan)result;
                if (!string.IsNullOrEmpty(conv))
                {
                    switch (conv.ToLowerInvariant())
                    {
                        case "sweep":
                        case "sweeps":
                            strres = (t.TotalDays / 790.833333333333).ToLongString();
                            strres += " sweep" + (strres == "1" ? "" : "s");
                            break;
                        case "y":
                        case "yr":
                        case "year":
                        case "years":
                            strres = (t.TotalDays / 365).ToLongString();
                            strres += " year" + (strres == "1" ? "" : "s");
                            break;
                        case "w":
                        case "wk":
                        case "week":
                        case "weeks":
                            strres = (t.TotalDays / 7).ToLongString();
                            strres += " week" + (strres == "1" ? "" : "s");
                            break;
                        case "d":
                        case "day":
                        case "days":
                            strres = t.TotalDays.ToLongString();
                            strres += " day" + (strres == "1" ? "" : "s");
                            break;
                        case "h":
                        case "hr":
                        case "hour":
                        case "hours":
                            strres = t.TotalHours.ToLongString();
                            strres += " hour" + (strres == "1" ? "" : "s");
                            break;
                        case "m":
                        case "min":
                        case "minute":
                        case "minutes":
                            strres = t.TotalMinutes.ToLongString();
                            strres += " minute" + (strres == "1" ? "" : "s");
                            break;
                        case "s":
                        case "sec":
                        case "second":
                        case "seconds":
                            strres = t.TotalSeconds.ToLongString();
                            strres += " second" + (strres == "1" ? "" : "s");
                            break;
                        case "cs":
                        case "centisecond":
                        case "centiseconds":
                            strres = (t.TotalMilliseconds / 10).ToLongString();
                            strres += " centisecond" + (strres == "1" ? "" : "s");
                            break;
                        case "f":
                        case "frame":
                        case "frames":
                        case "ntsc":
                        case "ntscframe":
                        case "ntscframes":
                            strres = (t.TotalMilliseconds / (1000 / 60.0)).ToLongString();
                            strres += " ntscframe" + (strres == "1" ? "" : "s");
                            break;
                        case "pal":
                        case "palframe":
                        case "palframes":
                            strres = (t.TotalMilliseconds / (1000 / 50.0)).ToLongString();
                            strres += " palframe" + (strres == "1" ? "" : "s");
                            break;
                        case "ms":
                        case "millisecond":
                        case "milliseconds":
                            strres = t.TotalMilliseconds.ToLongString();
                            strres += " millisecond" + (strres == "1" ? "" : "s");
                            break;
                        case "tick":
                        case "ticks":
                            strres = t.Ticks.ToString();
                            strres += " tick" + (strres == "1" ? "" : "s");
                            break;
                        default:
                            throw new ArgumentException("Unknown format code " + conv + ".");
                    }
                }
                else
                    strres = t.ToStringCustM();
            }
            else
                strres = result.ToString();
            IrcObject.WriteMessage(strres, channel);
        }

        void SeenCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Strip().Split(' ');
            List<string> targetaliases = IrcObject.GetAliases(split[0]) ?? new List<string>() { split[0] };
            if ((split.Length == 1 || split[0] != channel) && (targetaliases.Contains(user) | split[0].Equals("me", StringComparison.CurrentCultureIgnoreCase)))
                IrcObject.WriteMessage("Yes, you're here.", channel);
            else if ((split.Length == 1 || split[0] != channel) && (targetaliases.Contains(IrcObject.IrcNick) | split[0].Equals("you", StringComparison.CurrentCultureIgnoreCase)))
                IrcObject.WriteMessage("Yes, I'm here.", channel);
            else
            {
                IRCChannel ChanObj = IrcObject.GetChannel(split.Length > 1 ? split[1] : channel, true, user);
                IRCChanUserStats u = ChanObj.GetUserStats(user, true);
                IRCChanUserStats userobj = ChanObj.GetUserStats(split[0], true);
                if (userobj != null)
                {
                    if (ChanObj.GetUser(userobj.name) != null)
                        IrcObject.WriteMessage(userobj.name + " is currently in " + (split.Length > 1 ? split[1] : "the channel") + ". Their last action was " + (DateTime.Now - userobj.lastaction).ToStringCust(3) + " ago, " + userobj.lastmessage, channel);
                    else
                        IrcObject.WriteMessage("I last saw " + userobj.name + " " + (split.Length > 1 ? "in " + split[1] + " " : string.Empty) + (DateTime.Now - userobj.lastaction).ToStringCust(3) + " ago, " + userobj.lastmessage, channel);
                }
                else
                    IrcObject.WriteMessage("I haven't seen " + split[0] + (split.Length > 1 ? " in " + split[1] : string.Empty) + ".", channel);
            }
        }

        void SeenplusCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.Strip().Split(' ')[0];
            string seenmsg = "I haven't seen " + command + ".";
            DateTime latestaction = DateTime.MinValue;
            foreach (IRCChannel item in IrcObject.IrcChannels)
            {
                IRCChanUserStats userobj = item.GetUserStats(command, true);
                if (userobj != null && userobj.lastaction > latestaction)
                {
                    if (item.GetUser(userobj.name) != null)
                        seenmsg = userobj.name + " is currently in " + item.Name + ". Their last action was " + (DateTime.Now - userobj.lastaction).ToStringCust(3) + " ago, " + userobj.lastmessage;
                    else
                        seenmsg = "I last saw " + userobj.name + " in " + item.Name + " " + (DateTime.Now - userobj.lastaction).ToStringCust(3) + " ago, " + userobj.lastmessage;
                    latestaction = userobj.lastaction;
                }
            }
            IrcObject.WriteMessage(seenmsg, channel);
        }

        void JoinCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            if (IrcObject.GetChannel(Command[0]) != null)
            {
                if (IrcObject.GetChannel(Command[0]).Active)
                    IrcObject.WriteMessage("I'm already on that channel!", channel);
                else
                {
                    if (Command.Length > 1)
                    {
                        IrcObject.QueueWrite("JOIN " + Command[0] + " " + Command[1]);
                        IrcObject.GetChannel(Command[0]).Active = true;
                        IrcObject.GetChannel(Command[0]).Keyword = Command[1];
                    }
                    else if (!string.IsNullOrEmpty(IrcObject.GetChannel(Command[0]).Keyword))
                    {
                        IrcObject.QueueWrite("JOIN " + Command[0] + " " + IrcObject.GetChannel(Command[0]).Keyword);
                        IrcObject.GetChannel(Command[0]).Active = true;
                    }
                    else
                    {
                        IrcObject.QueueWrite("JOIN " + Command[0]);
                        IrcObject.GetChannel(Command[0]).Active = true;
                    }
                    Module1.myForm.Invoke(Module1.myForm.AddChannelDelegate, IrcObject.name + "\\" + Command[0]);
                }
            }
            else
            {
                if (Command.Length > 1)
                {
                    IrcObject.QueueWrite("JOIN " + Command[0] + " " + Command[1]);
                    IrcObject.IrcChannels.Add(new IRCChannel(IrcObject, Command[0], Command[1]));
                }
                else
                {
                    IrcObject.QueueWrite("JOIN " + Command[0]);
                    IrcObject.IrcChannels.Add(new IRCChannel(IrcObject, Command[0]));
                }
                Module1.myForm.Invoke(Module1.myForm.AddChannelDelegate, IrcObject.name + "\\" + Command[0]);
            }
        }

        void PartCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel, true, user);
            ChanObj.Active = false;
            if (string.IsNullOrEmpty(command))
                IrcObject.QueueWrite("PART " + channel + " :Part command issued by " + user + ".");
            else
                IrcObject.QueueWrite("PART " + channel + " :" + command);
            Module1.myForm.Invoke(Module1.myForm.RemoveChannelDelegate, channel);
        }

        void TobytesCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            IrcObject.WriteMessage(Module1.BytesToString(Module1.DataToBytes(Command[0].Strip(), string.Join(" ", Command, 1, Command.Length - 1))), channel);
        }

        void FrombytesCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            string type = Command[0].Strip();
            dynamic data = Module1.BytesToData(type, Module1.StringToBytes(string.Join(" ", Command, 1, Command.Length - 1)));
            switch (type)
            {
                case "byte":
                case "sbyte":
                case "short":
                case "int16":
                case "sword":
                case "ushort":
                case "uint16":
                case "word":
                case "int":
                case "int32":
                case "integer":
                case "sdword":
                case "uint":
                case "uint32":
                case "uinteger":
                case "dword":
                case "long":
                case "int64":
                case "sqword":
                case "ulong":
                case "uint64":
                case "qword":
                case "base64":
                case "datetime":
                case "date":
                case "time":
                    IrcObject.WriteMessage(data.ToString(), channel);
                    break;
                case "single":
                case "float":
                    IrcObject.WriteMessage(((float)data).ToLongString(), channel);
                    break;
                case "double":
                    IrcObject.WriteMessage(((double)data).ToLongString(), channel);
                    break;
                case "timespan":
                    IrcObject.WriteMessage(((TimeSpan)data).ToStringCust(), channel);
                    break;
                case "number":
                case "bigint":
                case "biginteger":
                    IrcObject.WriteMessage(data.ToString("R"), channel);
                    break;
                default:
                    IrcObject.WriteMessage("\"", data.ToString() + '"', channel);
                    break;
            }
        }

        void ConvertCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            FrombytesCommand(IrcObject, channel, user, Command[1] + ' ' + Module1.BytesToString(Module1.DataToBytes(Command[0].Strip(), string.Join(" ", Command, 2, Command.Length - 2))));
        }

        void BaseconvCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            IrcObject.WriteMessage(user + ": ", Module1.BaseConv(Command[0], int.Parse(Command[1]), int.Parse(Command[2])), channel);
        }

        void ByteconvCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            IrcObject.WriteMessage(user + ": ", Module1.ConvertBytesSize(Module1.ConvertSizeBytes(Command[0]), (Module1.ByteMultiples)Enum.Parse(typeof(Module1.ByteMultiples), Command[1], true)), channel);
        }

        void OffCommand(IRC IrcObject, string channel, string user, string command)
        {
            Module1.enabled = false;
            IrcObject.QueueWrite("NICK " + IrcObject.IrcNick + "|Off");
        }

        void OnCommand(IRC IrcObject, string channel, string user, string command)
        {
            Module1.enabled = true;
            IrcObject.QueueWrite("NICK " + IrcObject.IrcNick.Replace("|Off", ""));
        }

        void VardelCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.Strip();
            if (command.Equals("ans", StringComparison.OrdinalIgnoreCase))
            {
                IrcObject.WriteMessage("You can't delete that variable!", channel);
                return;
            }
            if (Module1.Vars.ContainsKey(command))
            {
                Module1.Vars.Remove(command);
                IrcObject.WriteMessage("Var " + command + " deleted.", channel);
            }
            else
                IrcObject.WriteMessage("Var " + command + " does not exist!", channel);
        }

        void VargetCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.Strip();
            if (Module1.Vars.ContainsKey(command))
                IrcObject.WriteMessage(command + "=" + Module1.Vars[command], channel);
            else
                IrcObject.WriteMessage("Var " + command + " does not exist!", channel);
        }

        void VarlistCommand(IRC IrcObject, string channel, string user, string command)
        {
            foreach (KeyValuePair<string, string> var in Module1.Vars)
                IrcObject.SendNotice(var.Key + "=" + var.Value, user);
        }

        void VarsetCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            string name = Command[0];
            string value = string.Join(" ", Command, 1, Command.Length - 1);
            bool valid = true;
            foreach (char item in name)
                if (!char.IsLetterOrDigit(item) & item != '_')
                {
                    valid = false;
                    break;
                }
            if (!char.IsLetter(name, 0) & name[0] != '_') valid = false;
            if (!valid)
            {
                IrcObject.WriteMessage("Invalid identifier!", channel);
                return;
            }
            Module1.Vars[name] = value;
            IrcObject.WriteMessage(name + "=" + value, channel);
        }

        void DieCommand(IRC IrcObject, string channel, string user, string command)
        {
            string message = string.IsNullOrEmpty(command) ? "die command issued by " + user : command;
            foreach (IRC server in Module1.IrcApp.IrcObjects)
                if (server.IrcConnection != null)
                    server.Disconnect(message);
            Module1.myForm.Invoke(new System.Windows.Forms.MethodInvoker(Module1.myForm.Close));
        }

        void DisconnectCommand(IRC IrcObject, string channel, string user, string command)
        {
            string message = string.IsNullOrEmpty(command) ? "disconnect command issued by " + user : command;
            if (IrcObject.IrcConnection != null)
                IrcObject.Disconnect(message);
            foreach (IRCChannel item in IrcObject.IrcChannels)
                foreach (IRCChanUserStats stat in item.stats)
                {
                    stat.onlinetimer.Stop();
                    stat.onlinesaved = stat.onlinetime;
                    stat.onlinetimer.Reset();
                    stat.refcount = 0;
                }
        }

        void ConnectCommand(IRC IrcObject, string channel, string user, string command)
        {
            foreach (ServerInfo inf in Module1.servinf)
                if (inf.name.Equals(command.Strip(), StringComparison.OrdinalIgnoreCase))
                {
                    Module1.myForm.ConnectNetwork(inf);
                    return;
                }
        }

        void OpCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            if (!channel.StartsWith("#"))
            {
                string newOp = user;
                if (Command.Length > 1)
                    newOp = Command[1];
                if (Command[0] == Module1.password)
                {
                    IrcObject.SendNotice("You are no longer the operator of this bot.", Module1.OpName);
                    Module1.OpName = newOp;
                    IrcObject.SendNotice("You are now the operator of this bot.", Module1.OpName);
                }
                else
                    IrcObject.SendNotice("Incorrect password.", user);
            }
        }

        void RawCommand(IRC IrcObject, string channel, string user, string command)
        {
            Module1.IrcApp.IrcCommand(IrcObject, channel, command);
        }

        void Md5Command(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] md5res = md5.ComputeHash(Module1.DataToBytes(Command[0].Strip(), Module1.Recombine(Command, 1)));
            System.Text.StringBuilder md5str = new System.Text.StringBuilder(md5res.Length * 2);
            foreach (byte item in md5res)
                md5str.Append(item.ToString("x2"));
            IrcObject.WriteMessage(md5str.ToString(), channel);
        }

        void Sha1Command(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create();
            byte[] sha1res = sha1.ComputeHash(Module1.DataToBytes(Command[0].Strip(), Module1.Recombine(Command, 1)));
            System.Text.StringBuilder sha1str = new System.Text.StringBuilder(sha1res.Length * 2);
            foreach (byte item in sha1res)
                sha1str.Append(item.ToString("x2"));
            IrcObject.WriteMessage(sha1str.ToString(), channel);
        }

        void Sha256Command(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] sha256res = sha256.ComputeHash(Module1.DataToBytes(Command[0].Strip(), Module1.Recombine(Command, 1)));
            System.Text.StringBuilder sha256str = new System.Text.StringBuilder(sha256res.Length * 2);
            foreach (byte item in sha256res)
                sha256str.Append(item.ToString("x2"));
            IrcObject.WriteMessage(sha256str.ToString(), channel);
        }

        void GreetingsCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel, true, user);
            if (!string.IsNullOrEmpty(command))
            {
                Module1.CheckAccessLevel(UserModes.BotOp, ChanObj.GetUser(user));
                switch (command.ToLower().Strip())
                {
                    case "on":
                        ChanObj.greeting = true;
                        IrcObject.WriteMessage("Greetings enabled.", channel);
                        break;
                    case "off":
                        ChanObj.greeting = false;
                        IrcObject.WriteMessage("Greetings disabled.", channel);
                        break;
                }
            }
            else
                IrcObject.WriteMessage("Greetings are " + (ChanObj.greeting ? "enabled." : "disabled."), channel);
        }

        void KickmeCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (string.IsNullOrEmpty(command)) command = "You asked for it.";
            IrcObject.QueueWrite("KICK " + channel + " " + user + " :" + command);
        }

        void RemovemeCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (string.IsNullOrEmpty(command)) command = "You asked for it.";
            IrcObject.QueueWrite("REMOVE " + user + " " + channel + " :" + command);
        }

        void PingCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(Module1.CTCPChar + "PING " + unchecked((ulong)DateTime.Now.ToBinary()).ToString() + Module1.CTCPChar, user);
        }

        void ModeCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel, true, user);
            IRCUser UserObj = ChanObj.GetUser(user);
            if (UserObj.mode > UserModes.Normal)
                IrcObject.WriteMessage(UserObj.mode.ToString() + " (+" + IrcObject.modechars[(int)UserObj.mode - 1 + IrcObject.voiceind] + ", " + IrcObject.prefixes[(int)UserObj.mode - 1 + IrcObject.voiceind] + ", " + ((int)UserObj.mode).ToString() + ")", channel);
            else
                IrcObject.WriteMessage("Normal (0)", channel);
        }

        void EightballCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(Module1.ballresps[Module1.Random.Next(Module1.ballresps.Length)], channel);
        }

        void CueballCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(Module1.ColorChar + "00,00" + new string(' ', Module1.Random.Next(5, 31)), channel);
        }

        void ActionCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            IrcObject.WriteMessage(Module1.CTCPChar + "ACTION " + string.Join(" ", Command, 1, Command.Length - 1) + Module1.CTCPChar, Command[0]);
        }

        void CycleCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (string.IsNullOrEmpty(command)) command = channel;
            IrcObject.QueueWrite("CYCLE " + command);
        }

        void GreetingCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel, true, user);
            IRCChanUserStats UserObj = ChanObj.GetUserStats(user, false);
            if (!string.IsNullOrEmpty(command))
            {
                if (command.Equals("off", StringComparison.OrdinalIgnoreCase))
                    UserObj.greeting = "";
                else
                    UserObj.greeting = command;
            }
            IrcObject.WriteMessage(user + ": Your current greeting message is \"" + UserObj.greeting + "\".", channel);
        }

        void SetgreetingCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] Command = command.Split(' ');
            string usr = Command[0];
            string message = string.Join(" ", Command, 1, Command.Length - 1);
            IRCChannel ChanObj = IrcObject.GetChannel(channel, true, user);
            IRCChanUserStats UserObj = ChanObj.GetUserStats(usr, false);
            if (!string.IsNullOrEmpty(command))
            {
                if (command.Equals("off", StringComparison.OrdinalIgnoreCase))
                    UserObj.greeting = "";
                else
                    UserObj.greeting = command;
            }
            IrcObject.WriteMessage(user + ": " + usr + "'s current greeting message is \"" + UserObj.greeting + "\".", channel);
        }

        void AccesslevelCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.ToLowerInvariant().Strip();
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            BotCommand? cmd = Module1.IrcApp.GetCommand(command);
            if (!cmd.HasValue)
            {
                IrcObject.WriteMessage("Command \"" + command + "\" not found.", channel);
                return;
            }
            UserModes oldmode = cmd.Value.AccessLevel;
            if (ChanObj.AccessLevels.ContainsKey(command))
                oldmode = ChanObj.AccessLevels[command];
            IrcObject.WriteMessage("Access level for command \"" + command + "\" is " + oldmode + ".", channel);
            return;
        }

        void SetaccesslevelCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.ToLowerInvariant().Strip();
            string[] commandSplit = command.Split(' ');
            string level = commandSplit[commandSplit.Length - 1];
            string com = string.Join(" ", commandSplit, 0, commandSplit.Length - 1);
            bool reset = level == "reset";
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            UserModes mode;
            if (!Enum.TryParse(level, true, out mode) & !reset)
            {
                BotCommand? cmd = Module1.IrcApp.GetCommand(command);
                if (!cmd.HasValue)
                {
                    IrcObject.WriteMessage("Command \"" + command + "\" not found.", channel);
                    return;
                }
                UserModes oldmode = cmd.Value.AccessLevel;
                if (ChanObj.AccessLevels.ContainsKey(command))
                    oldmode = ChanObj.AccessLevels[command];
                IrcObject.WriteMessage("Access level for command \"" + command + "\" is " + oldmode + ".", channel);
                return;
            }
            if (com == "allcmds")
            {
                if (reset)
                {
                    ChanObj.AccessLevels.Clear();
                    IrcObject.WriteMessage("Access level for all commands reset.", channel);
                }
                else
                {
                    foreach (KeyValuePair<string, BotCommand> cmd in Module1.IrcApp.CommandDictionary)
                        AccesslevelCommandHelper(ChanObj, cmd.Value, cmd.Key, mode);
                    IrcObject.WriteMessage("Access level for all commands set to " + mode + ".", channel);
                }
            }
            else
            {
                BotCommand? cmd = Module1.IrcApp.GetCommand(com);
                if (!cmd.HasValue)
                {
                    IrcObject.WriteMessage("Command \"" + com + "\" not found.", channel);
                    return;
                }
                if (reset)
                {
                    if (ChanObj.AccessLevels.ContainsKey(com))
                        ChanObj.AccessLevels.Remove(com);
                    IrcObject.WriteMessage("Access level for command \"" + com + "\" reset to " + cmd.Value.AccessLevel + ".", channel);
                }
                else if (mode >= cmd.Value.AccessLevel)
                {
                    UserModes oldmode = cmd.Value.AccessLevel;
                    if (ChanObj.AccessLevels.ContainsKey(com))
                        oldmode = ChanObj.AccessLevels[com];
                    ChanObj.AccessLevels[com] = mode;
                    IrcObject.WriteMessage("Access level for command \"" + com + "\" changed from " + oldmode + " to " + mode + ".", channel);
                }
                else
                    IrcObject.WriteMessage("Cannot set access level for command \"" + com + "\" lower than default " + cmd.Value.AccessLevel + ".", channel);
            }
        }

        void AccesslevelCommandHelper(IRCChannel ChanObj, BotCommand command, string fullname, UserModes mode)
        {
            if (mode >= command.AccessLevel)
                ChanObj.AccessLevels[fullname] = mode;
            foreach (KeyValuePair<string, BotCommand> cmd in command.SubCommands)
                AccesslevelCommandHelper(ChanObj, cmd.Value, fullname + " " + cmd.Key, mode);
        }

        void NoteCommand(IRC IrcObject, string channel, string user, string command)
        {
            List<string> split = new List<string>(command.Split(' '));
            bool aliases = true;
            NoteMode mode = NoteMode.JoinOrText;
            while (split.Count > 2)
            {
                if (split[0].ToLowerInvariant() == "/noaliases")
                {
                    aliases = false;
                    split.RemoveAt(0);
                }
                else if (split[0].ToLowerInvariant() == "/joinonly")
                {
                    mode = NoteMode.JoinOnly;
                    split.RemoveAt(0);
                }
                else if (split[0].ToLowerInvariant() == "/textonly")
                {
                    mode = NoteMode.TextOnly;
                    split.RemoveAt(0);
                }
                else
                    break;
            }
            Note note = new Note(Module1.Random.Next(), user, aliases, mode, split[0], DateTime.Now, string.Join(" ", split.ToArray(), 1, split.Count - 1));
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
			if (ChanObj.Notes.Where((item) => item.Target == note.Target & item.Sender == note.Sender).Count() >= 3)
			{
				IrcObject.WriteMessage("Can only leave three notes to the same person. Try deleting one first.", channel);
				return;
			}
			while (ChanObj.Notes.Any((item) => item.ID == note.ID))
				note.ID = Module1.Random.Next();
            ChanObj.Notes.Add(note);
            IrcObject.WriteMessage(user + ": left note for " + note.Target + " with ID " + note.ID.ToBase(36) + ".", channel);
        }

        void NotelistCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            foreach (Note item in ChanObj.Notes)
                if (item.Sender == user)
                    IrcObject.SendNotice("Note " + item.ID.ToBase(36) + " to " + item.Target + " " + (DateTime.Now - item.Date).ToStringCust(3) + " ago: " + item.Message, user);
        }

        void DelnoteCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            foreach (Note item in ChanObj.Notes)
                if (item.ID.ToBase(36) == command)
                {
                    if (item.Sender != user & !Module1.CheckAccessLevel(UserModes.BotOp, ChanObj.GetUser(user), false))
                    {
                        IrcObject.WriteMessage("This note was not created by you.", channel);
                        return;
                    }
                    ChanObj.Notes.Remove(item);
                    IrcObject.WriteMessage("Note " + item.ID.ToBase(36) + " deleted.", channel);
                    return;
                }
            IrcObject.WriteMessage("No note with ID " + command + " was found.", channel);
        }

        void StatsCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            IRCChannel chanobj = IrcObject.GetChannel(channel);
            if (split.Length > 1)
                chanobj = IrcObject.GetChannel(split[1]);
            IRCChanUserStats userobj = chanobj.GetUserStats(split[0], true);
            if (userobj != null)
            {
                List<string> items = new List<string>() { Module1.UnderChar + "Name: " + userobj.name + Module1.UnderChar };
                Dictionary<string, dynamic> stats = HttpServer.StatsToDict(userobj);
                foreach (KeyValuePair<string, string> stat in HttpServer.statitems)
                    if (stat.Key == "lastaction")
                        items.Add(Module1.UnderChar + stat.Value + ": " + Module1.ToStringCustShort(DateTime.Now - stats[stat.Key], 3) + " ago, " + stats["lastmessage"]);
                    else if (stat.Key == "onlinetime")
                        items.Add(Module1.UnderChar + stat.Value + ": " + Module1.ToStringCustShort(stats[stat.Key], 3) + Module1.UnderChar);
                    else if (stat.Key == "wordsperline" | stat.Key == "charsperline")
                        items.Add(Module1.UnderChar + stat.Value + ": " + Math.Round(stats[stat.Key], 2).ToString() + Module1.UnderChar);
                    else
                        items.Add(Module1.UnderChar + stat.Value + ": " + stats[stat.Key].ToString() + Module1.UnderChar);
                IrcObject.SendNotice(string.Join(" ", items.ToArray()), user);
            }
            else
                IrcObject.SendNotice("User " + split[0] + " not found.", user);
        }

        void GameCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(Module1.Game(), channel);
        }

        void DriveCommand(IRC IrcObject, string channel, string user, string command)
        {
            DriveInfo drvinf = new DriveInfo(command);
            if (!drvinf.IsReady)
                IrcObject.WriteMessage("Drive " + drvinf.Name + " (" + drvinf.DriveType.ToString() + ") is not ready.", channel);
            else
                IrcObject.WriteMessage("Drive " + drvinf.Name + " (" + drvinf.VolumeLabel + ") " + Module1.smartsize((ulong)(drvinf.TotalSize - drvinf.TotalFreeSpace)) + "/" + Module1.smartsize((ulong)drvinf.TotalSize) + " (" + ((double)(drvinf.TotalSize - drvinf.TotalFreeSpace) / (double)drvinf.TotalSize) * 100 + "%) " + drvinf.DriveType.ToString() + " " + drvinf.DriveFormat, channel);
        }

        void ScreenCommand(IRC IrcObject, string channel, string user, string command)
        {
            System.Windows.Forms.Screen scrn = System.Windows.Forms.Screen.AllScreens[int.Parse(command) - 1];
            IrcObject.WriteMessage(scrn.DeviceName + " (" + scrn.Bounds.Location.X + "," + scrn.Bounds.Y + ") " + scrn.Bounds.Width + "x" + scrn.Bounds.Height + " " + scrn.BitsPerPixel + "bpp" + (scrn.Primary ? " Primary" : ""), channel);
        }

        void ComputerCommand(IRC IrcObject, string channel, string user, string command)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    IrcObject.WriteMessage(Environment.MachineName + ": " + Module1.UnderChar + CSharp411.OSInfo.Name + " " + CSharp411.OSInfo.Edition + " " + CSharp411.OSInfo.ServicePack + Module1.UnderChar + " " + Module1.UnderChar + System.Globalization.CultureInfo.InstalledUICulture.EnglishName + Module1.UnderChar + " " + Module1.UnderChar + "Version " + CSharp411.OSInfo.VersionString + Module1.UnderChar + " Uptime: " + TimeSpan.FromMilliseconds((uint)Environment.TickCount).ToStringCust(), channel);
                    break;
                case PlatformID.Unix:
                    if (!File.Exists("/etc/lsb-release")) goto default;
                    Dictionary<string, Dictionary<string, string>> unixdist = IniFile.IniFile.Load("/etc/lsb-release");
                    IrcObject.WriteMessage(Environment.MachineName + ": " + Module1.UnderChar + unixdist[string.Empty]["DISTRIB_DESCRIPTION"].Trim('"') + Module1.UnderChar + " " + Module1.UnderChar + System.Globalization.CultureInfo.InstalledUICulture.EnglishName + Module1.UnderChar + " " + Module1.UnderChar + Environment.OSVersion.VersionString + Module1.UnderChar + " Uptime: " + TimeSpan.FromMilliseconds((uint)Environment.TickCount).ToStringCust(), channel);
                    break;
                default:
                    IrcObject.WriteMessage(Environment.MachineName + ": " + Environment.OSVersion.VersionString + " " + Module1.UnderChar + System.Globalization.CultureInfo.InstalledUICulture.EnglishName + Module1.UnderChar + " Uptime: " + TimeSpan.FromMilliseconds((uint)Environment.TickCount).ToStringCust(), channel);
                    break;
            }
        }

        void TranslateCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Split(' ');
            string message = string.Join(" ", split, 1, split.Length - 1);
            string oldtrans = Module1.translate;
            Module1.translate = split[0].ToLowerInvariant();
            try
            {
                IrcObject.WriteMessage(user + ": ", message, channel);
            }
            finally
            {
                Module1.translate = oldtrans;
            }
        }

        void TranslatemsgsCommand(IRC IrcObject, string channel, string user, string command)
        {
            Module1.translate = command.ToLowerInvariant();
            IrcObject.WriteMessage("Automatic message translation set to " + Module1.translate + ".", channel);
        }

        void TranslatecmdCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Split(' ');
            string message = string.Join(" ", split, 1, split.Length - 1);
            string oldtrans = Module1.translate;
            Module1.translate = split[0].ToLowerInvariant();
            try
            {
                Module1.IrcApp.BotCommand(IrcObject, user, channel, message);
            }
            finally
            {
                Module1.translate = oldtrans;
            }
        }

        void DnsCommand(IRC IrcObject, string channel, string user, string command)
        {
            IPHostEntry host = Dns.GetHostEntry(command);
            string message = Module1.UnderChar + "Hostname: " + host.HostName + Module1.UnderChar + " " + Module1.UnderChar + "IPs: " + string.Join(", ", Array.ConvertAll(host.AddressList, (i) => i.ToString())) + Module1.UnderChar;
            if (host.Aliases.Length > 0)
                message += " " + Module1.UnderChar + "Aliases: " + string.Join(", ", host.Aliases) + Module1.UnderChar;
            IrcObject.WriteMessage(message, channel);
        }

        void IdentCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.QueueWrite("PRIVMSG NickServ :identify " + IrcObject.NSPass);
        }

        void SayCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(command, channel);
        }

        void MsgCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Split(' ');
            IrcObject.WriteMessage(string.Join(" ", split, 1, split.Length - 1), split[0]);
        }

        void ChooseCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] options = Module1.ParseCommandLine(command);
            IrcObject.WriteMessage(user + ": ", options[Module1.Random.Next(0, options.Length)], channel);
        }

        void ProcessCommand(IRC IrcObject, string channel, string user, string command)
        {
            System.Diagnostics.Process[] procs = { System.Diagnostics.Process.GetCurrentProcess() };
            if (!string.IsNullOrWhiteSpace(command))
                procs = System.Diagnostics.Process.GetProcessesByName(command);
            foreach (System.Diagnostics.Process proc in procs)
                IrcObject.WriteMessage("Process Info: " + proc.MainModule.FileVersionInfo.FileDescription + " " + Module1.UnderChar + "ID: " + proc.Id + Module1.UnderChar + " " + Module1.UnderChar + "Uptime: " + (DateTime.Now - proc.StartTime).ToStringCust() + Module1.UnderChar + " " + Module1.UnderChar + "Priority: " + proc.PriorityClass.ToString() + Module1.UnderChar + " " + Module1.UnderChar + "Modules: " + proc.Modules.Count + Module1.UnderChar + " " + Module1.UnderChar + "Threads: " + proc.Threads.Count + Module1.UnderChar + " " + Module1.UnderChar + "Physical Memory: " + Module1.smartsize((ulong)proc.WorkingSet64) + Module1.UnderChar + " " + Module1.UnderChar + "Virtual Memory: " + Module1.smartsize((ulong)proc.VirtualMemorySize64) + Module1.UnderChar, channel);
        }

        void UptimeCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage("System Uptime: " + TimeSpan.FromMilliseconds((uint)Environment.TickCount).ToStringCust() + " Bot Uptime: " + (DateTime.Now - Process.GetCurrentProcess().StartTime).ToStringCust(), channel);
        }

        void Asmx86Command(IRC IrcObject, string channel, string user, string command)
        {
            File.WriteAllText("tmp.asm", "bits 32" + Environment.NewLine + command);
            Process asmx86 = Process.Start(new ProcessStartInfo("nasm.exe", "-o tmp.bin -Z err.txt tmp.asm") { UseShellExecute = false, CreateNoWindow = true });
            asmx86.WaitForExit();
            if (new FileInfo("err.txt").Length > 0)
                IrcObject.WriteMessage(File.ReadAllText("err.txt"), channel);
            else
                IrcObject.WriteMessage(Module1.BytesToString(File.ReadAllBytes("tmp.bin")), channel);
        }

        void CombinealiasesCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Split(' ');
            string last = split[split.Length - 1];
            for (int i = 0; i < split.Length - 1; i++)
            {
                List<string> a1 = IrcObject.GetAliases(split[i]);
                if (a1 == null) continue;
                if (!object.ReferenceEquals(a1, IrcObject.GetAliases(last)))
                {
                    IrcObject.Aliases.Remove(a1);
                    foreach (string item in a1)
                        IrcObject.AddAlias(last, item);
                }
            }
        }

        void DelstatsCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            if (ChanObj.GetUserStats(command.TrimEnd(), false) != null)
                ChanObj.stats.Remove(ChanObj.GetUserStats(command.TrimEnd(), false));
        }

        void AliasesCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                command = user;
            command = command.TrimEnd();
            if (IrcObject.GetAliases(command) != null)
                IrcObject.WriteMessage("Aliases of " + command + ": " + string.Join(" ", IrcObject.GetAliases(command)), channel);
        }

        void AddaliasCommand(IRC IrcObject, string channel, string user, string command)
        {
            List<string> split = new List<string>(command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            string u = split[0];
            split.RemoveAt(0);
            foreach (string item in split)
                IrcObject.AddAlias(u, item);
        }

        void DelaliasCommand(IRC IrcObject, string channel, string user, string command)
        {
            List<string> split = new List<string>(command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            string u = split[0];
            split.RemoveAt(0);
            foreach (string item in split)
                IrcObject.DelAlias(u, item);
        }

        void SplitaliasesCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Split(' ');
            List<string> source = IrcObject.GetAliases(split[0]);
            List<string> destination = new List<string>(split);
            destination.RemoveAt(0);
            source.RemoveAll((a) => destination.Contains(a));
            IrcObject.Aliases.Add(destination);
        }

        void MovealiasesCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Split(' ');
            string last = split[split.Length - 1];
            for (int i = 0; i < split.Length - 1; i++)
            {
                List<string> a1 = IrcObject.GetAliases(split[i]);
                if (a1 == null) continue;
                if (!object.ReferenceEquals(a1, IrcObject.GetAliases(last)))
                {
                    IrcObject.Aliases.Remove(a1);
                    IrcObject.AddAlias(last, split[i]);
                }
            }
        }

        void RemindmeCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Split(' ');
            int that = Array.IndexOf(split, "that");
            string message = string.Join(" ", split, that + 1, split.Length - (that + 1));
            string datestring = string.Join(" ", split, 1, that - 1);
            System.DateTime newdate = default(DateTime);
            switch (split[0].ToLowerInvariant())
            {
                case "in":
                    newdate = DateTime.Now + Module1.GetTimeSpan(datestring).Value;
                    break;
                case "at":
                    newdate = Module1.GetDate(datestring).Value;
                    break;
            }
            Module1.reminders.Add(new Module1.Reminder(IrcObject, user, message, newdate));
            IrcObject.WriteMessage("Reminder added for " + newdate + ".", channel);
        }

        void VbCommand(IRC IrcObject, string channel, string user, string command)
        {
            ulong uid = unchecked((ulong)DateTime.Now.ToBinary());
            System.CodeDom.Compiler.CodeDomProvider cd = new Microsoft.VisualBasic.VBCodeProvider();
            System.CodeDom.Compiler.CompilerParameters cp = new System.CodeDom.Compiler.CompilerParameters(new string[] { "System.dll", "System.Core.dll", "System.Numerics.dll", Assembly.GetExecutingAssembly().Location }) { OutputAssembly = "MMBotCode" + uid + ".dll" };
            System.CodeDom.Compiler.CompilerResults cmpres;
            string code = Properties.Resources.MyCode.Replace("'CODE GOES HERE", command);
            cmpres = cd.CompileAssemblyFromSource(cp, code);
            int errs = 0;
            for (int er = 0; er < cmpres.Errors.Count; er++)
            {
                if (!cmpres.Errors[er].IsWarning)
                {
                    IrcObject.WriteMessage("Error " + cmpres.Errors[er].ErrorNumber + " at column " + cmpres.Errors[er].Column + ": " + cmpres.Errors[er].ErrorText, channel);
                    errs++;
                }
                if (errs == 5) break;
            }
            if (errs > 0)
                return;
            System.Reflection.Assembly assembly = cmpres.CompiledAssembly;
            Type typ = assembly.GetType("MMBotCodeClass");
            System.Reflection.MethodInfo CustomCodeFunc = typ.GetMethod("CustomCode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.IgnoreCase, null, System.Reflection.CallingConventions.Standard, new[] { typeof(IRC), typeof(IRCChannel) }, null);
            IrcObject.WriteMessage(user + ": ", ((Func<IRC, IRCChannel, object>)Delegate.CreateDelegate(typeof(Func<IRC, IRCChannel, object>), null, CustomCodeFunc))(IrcObject, IrcObject.GetChannel(channel, true, user)).ToString(), channel);
        }

        void CsharpCommand(IRC IrcObject, string channel, string user, string command)
        {
            ulong uid = unchecked((ulong)DateTime.Now.ToBinary());
            System.CodeDom.Compiler.CodeDomProvider cd = new Microsoft.CSharp.CSharpCodeProvider();
            System.CodeDom.Compiler.CompilerParameters cp = new System.CodeDom.Compiler.CompilerParameters(new string[] { "System.dll", "System.Core.dll", "System.Numerics.dll", Assembly.GetExecutingAssembly().Location }) { OutputAssembly = "MMBotCode" + uid + ".dll" };
            string[] split = command.Split(' ');
            string code = command;
            if (split[0] == "/unsafe")
            {
                cp.CompilerOptions = "/unsafe";
                code = string.Join(" ", split, 1, split.Length - 1);
            }
            System.CodeDom.Compiler.CompilerResults cmpres;
            code = Properties.Resources.MyC_Code.Replace("//CODE GOES HERE", code);
            if (split[0] == "/unsafe")
                code = code.Replace("/*unsafe*/", "unsafe");
            cmpres = cd.CompileAssemblyFromSource(cp, code);
            int errs = 0;
            for (int er = 0; er < cmpres.Errors.Count; er++)
            {
                if (!cmpres.Errors[er].IsWarning)
                {
                    IrcObject.WriteMessage("Error " + cmpres.Errors[er].ErrorNumber + " at column " + cmpres.Errors[er].Column + ": " + cmpres.Errors[er].ErrorText, channel);
                    errs++;
                }
                if (errs == 5) break;
            }
            if (errs > 0)
                return;
            System.Reflection.Assembly assembly = cmpres.CompiledAssembly;
            Type typ = assembly.GetType("MMBotCodeClass");
            System.Reflection.MethodInfo CustomCodeFunc = typ.GetMethod("CustomCode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.IgnoreCase, null, System.Reflection.CallingConventions.Standard, new[] { typeof(IRC), typeof(IRCChannel) }, null);
            IrcObject.WriteMessage(user + ": ", ((Func<IRC, IRCChannel, object>)Delegate.CreateDelegate(typeof(Func<IRC, IRCChannel, object>), null, CustomCodeFunc))(IrcObject, IrcObject.GetChannel(channel, true, user)).ToString(), channel);
        }

        void StacktraceCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(Module1.IrcApp.stacktrace, channel);
        }

        void LagCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.QueueWrite("PING " + DateTime.Now.ToBinary());
        }

        void CmdcharCommand(IRC IrcObject, string channel, string user, string command)
        {
            if (!string.IsNullOrWhiteSpace(command))
                Module1.cmdchar = command.Split(' ')[0];
            IrcObject.WriteMessage("Command character is " + Module1.cmdchar, channel);
        }

        void ExecCommand(IRC IrcObject, string channel, string user, string command)
        {
            Module1.proclist.Add(new ProcessInfo(command, IrcObject, channel));
        }

        void MkickCommand(IRC IrcObject, string channel, string user, string command)
        {
            IRCChannel ChanObj = IrcObject.GetChannel(channel);
            List<string> kicks = new List<string>();
            UserModes md = (UserModes)Math.Min((int)ChanObj.GetUser(IrcObject.IrcNick).mode, (int)ChanObj.GetUser(user).mode);
            if (md > UserModes.Voice)
            {
                foreach (IRCUser p in ChanObj.People)
                    if (!p.name.Equals(IrcObject.IrcNick, StringComparison.OrdinalIgnoreCase) && p.mode <= md)
                        kicks.Add(p.name);
                IrcObject.QueueWrite("KICK " + channel + " " + string.Join(",", kicks.ToArray()) + " :Mass kick");
            }
        }

        void IgnoreCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.Strip();
            if (IrcObject.GetChannel(channel) != null && IrcObject.GetChannel(channel).GetUser(command) != null)
                command = IrcObject.GetChannel(channel).GetUser(command).Mask;
            if (Module1.IgnoreList.IndexOf(command) == -1)
            {
                Module1.IgnoreList.Add(command);
                IrcObject.WriteMessage(command + " ignored.", user);
            }
        }

        void IgnorelistCommand(IRC IrcObject, string channel, string user, string command)
        {
            foreach (string name in Module1.IgnoreList)
                IrcObject.QueueWrite("NOTICE " + user + " " + name);
        }

        void UnignoreCommand(IRC IrcObject, string channel, string user, string command)
        {
            command = command.Strip();
            if (IrcObject.GetChannel(channel) != null && IrcObject.GetChannel(channel).GetUser(command) != null)
                command = IrcObject.GetChannel(channel).GetUser(command).Mask;
            if (Module1.IgnoreList.IndexOf(command) > -1)
            {
                Module1.IgnoreList.Remove(command);
                IrcObject.WriteMessage(command + " unignored.", user);
            }
        }

        void AllchanCommand(IRC IrcObject, string channel, string user, string command)
        {
            List<string> lolchans = new List<string>();
            foreach (IRCChannel chanc in IrcObject.IrcChannels)
                if (chanc.Active)
                    lolchans.Add(chanc.Name);
            foreach (string chanst in lolchans)
                Module1.IrcApp.BotCommand(IrcObject, user, chanst, command.Replace("<chan>", chanst));
        }

        void AlluserCommand(IRC IrcObject, string channel, string user, string command)
        {
            foreach (IRCUser person in IrcObject.GetChannel(channel).People)
                if (!person.name.Equals(IrcObject.IrcNick, StringComparison.CurrentCultureIgnoreCase))
                    Module1.IrcApp.BotCommand(IrcObject, user, channel, command.Replace("<user>", person.name));
        }

        void ForCommand(IRC IrcObject, string channel, string user, string command)
        {
            string[] split = command.Split(' ');
            if (split[1].Equals("to", StringComparison.OrdinalIgnoreCase))
            {
                decimal loopst = decimal.Parse(split[0], System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo);
                decimal loopend = decimal.Parse(split[2], System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo);
                decimal loopstep = 1;
                string commandstr = string.Join(" ", split, 3, split.Length - 3);
                if (split[3].Equals("step", StringComparison.OrdinalIgnoreCase))
                {
                    commandstr = string.Join(" ", split, 5, split.Length - 5);
                    loopstep = decimal.Parse(split[4], System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo);
                }
                for (decimal loopcnt = loopst; loopcnt <= loopend; loopcnt += loopstep)
                    Module1.IrcApp.BotCommand(IrcObject, user, channel, commandstr.Replace("<loop>", loopcnt.ToString()));
            }
        }

        void UrldecodeCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(user + ": ", Uri.UnescapeDataString(command), channel);
        }

        void UrlencodeCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(user + ": ", Uri.EscapeDataString(command), channel);
        }

        void HtmldecodeCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(user + ": ", System.Web.HttpUtility.HtmlDecode(command), channel);
        }

        void HtmlencodeCommand(IRC IrcObject, string channel, string user, string command)
        {
            IrcObject.WriteMessage(user + ": ", HttpServer.convert_irc_string(command), channel);
        }
    }

    public struct BotCommand
    {
        public readonly string Name;
        public readonly string Module;
        public readonly UserModes AccessLevel;
        public readonly BotCommandFunc Function;
        public readonly int CMDMinLength;
        public readonly bool SeparateThread;
        public readonly string HelpText;
        public readonly Dictionary<string, BotCommand> SubCommands;

        public BotCommand(string name, string module, UserModes accessLevel, BotCommandFunc function, int cmdMinLength, bool separateThread, string helpText, params BotCommand[] subCommands)
        {
            Name = name.ToLowerInvariant();
            Module = module;
            AccessLevel = accessLevel;
            Function = function;
            CMDMinLength = cmdMinLength;
            SeparateThread = separateThread;
            HelpText = helpText;
            SubCommands = new Dictionary<string, BotCommand>();
            foreach (BotCommand item in subCommands)
            {
                try { SubCommands.Add(item.Name, item); }
                catch (ArgumentException) { Console.WriteLine("Item with name " + item.Name + " was already added."); }
            }
        }
    }
}
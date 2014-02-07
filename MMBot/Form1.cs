using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace MMBot
{
    public partial class Form1
    {

        public Form1()
        {
            InitializeComponent();
        }

        List<string> cmdhist = new List<string>();
        int cmdind;

        string laststr;
        private void TextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (cmdind == cmdhist.Count)
                        laststr = TextBox2.Text;
                    if (cmdind > 0)
                    {
                        cmdind -= 1;
                        TextBox2.Text = cmdhist[cmdind];
                    }
                    TextBox2.Select(TextBox2.TextLength, 0);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Down:
                    if (cmdind < cmdhist.Count - 1)
                    {
                        cmdind += 1;
                        TextBox2.Text = cmdhist[cmdind];
                    }
                    else if (cmdind == cmdhist.Count - 1)
                    {
                        cmdind += 1;
                        TextBox2.Text = laststr;
                    }
                    TextBox2.Select(TextBox2.TextLength, 0);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Enter:
                    IRC server = Module1.GetNetworkByName(TreeView1.SelectedNode.FullPath.Split('\\')[0]);
                    if (TextBox2.Text.StartsWith("!"))
                    {
                        if (TextBox2.Text.StartsWith("!!"))
                        {
                            server.WriteMessage(TextBox2.Text.Substring(1), TreeView1.SelectedNode.Name);
                        }
                        else if (TextBox2.Text.Equals("!break", StringComparison.OrdinalIgnoreCase))
                        {
                            server.Connect();
                        }
                        else
                        {
                            server.WriteMessage(TextBox2.Text, TreeView1.SelectedNode.Name);
                            Module1.IrcApp.BotCommand(server, Module1.OpName, TreeView1.SelectedNode.Name, TextBox2.Text.Substring(1));
                        }
                    }
                    else if (TextBox2.Text.StartsWith("/"))
                    {
                        if (TextBox2.Text.StartsWith("//"))
                        {
                            server.WriteMessage(TextBox2.Text.Substring(1), TreeView1.SelectedNode.Name);
                        }
                        else
                        {
                            Module1.IrcApp.IrcCommand(server, TreeView1.SelectedNode.Name, TextBox2.Text.Substring(1));
                        }
                    }
                    else
                    {
                        server.WriteMessage(TextBox2.Text, TreeView1.SelectedNode.Name);
                    }
                    cmdhist.Add(TextBox2.Text);
                    cmdind = cmdhist.Count;
                    TextBox2.Clear();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Tab:
                    if (TextBox2.SelectionStart > 0)
                    {
                        string str = string.Empty;
                        int i = TextBox2.SelectionStart - 1;
                        while (i > -1 && !char.IsWhiteSpace(TextBox2.Text[i]))
                        {
                            str = str.Insert(0, TextBox2.Text[i].ToString());
                            i -= 1;
                        }
                        foreach (IRCUser user in Module1.GetNetworkByName(TreeView1.SelectedNode.FullPath.Split('\\')[0]).GetChannel(TreeView1.SelectedNode.Name).People)
                        {
                            if (user.name.StartsWith(str, StringComparison.CurrentCultureIgnoreCase))
                            {
                                TextBox2.SelectionStart = i + 1;
                                TextBox2.SelectionLength = str.Length;
                                TextBox2.SelectedText = user.name + (i == -1 ? ": " : " ");
                                break;
                            }
                        }
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.K:
                    if (e.Control)
                    {
                        TextBox2.SelectedText = Module1.ColorChar.ToString();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.O:
                    if (e.Control)
                    {
                        TextBox2.SelectedText = Module1.StopChar.ToString();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.B:
                    if (e.Control)
                    {
                        TextBox2.SelectedText = Module1.BoldChar.ToString();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.R:
                    if (e.Control)
                    {
                        TextBox2.SelectedText = Module1.RevChar.ToString();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.U:
                    if (e.Control)
                    {
                        TextBox2.SelectedText = Module1.UnderChar.ToString();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;
            }
        }

        private GUIChanInfo GetDictionaryEntry(string key)
        {
            foreach (KeyValuePair<string, GUIChanInfo> item in Module1.chanscrollback)
            {
                if (item.Key.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                    return item.Value;
            }
            return null;
        }

        public delegate void AddChannel(string chanName);
        internal AddChannel AddChannelDelegate;
        private void AddChannelMethod(string chanName)
        {
            if (GetDictionaryEntry(chanName) == null)
                Module1.chanscrollback.Add(chanName, new GUIChanInfo());
            string netname = chanName.Split('\\')[0];
            TreeNode mynode = default(TreeNode);
            if (chanName.Contains("\\"))
            {
                chanName = chanName.Split('\\')[1];
                mynode = TreeView1.Nodes[netname].Nodes.Add(chanName, chanName);
            }
            else
            {
                mynode = TreeView1.Nodes.Add(chanName, chanName);
            }
            TreeView1.Sort();
            TreeView1.SelectedNode = mynode;
        }

        public delegate void RemoveChannel(string chanName);
        internal RemoveChannel RemoveChannelDelegate;
        private void RemoveChannelMethod(string chanName)
        {
            string netname = chanName.Split('\\')[0];
            TreeNode mynode = TreeView1.SelectedNode;
            if (TreeView1.SelectedNode.FullPath == chanName)
                mynode = TreeView1.SelectedNode.Parent;
            if (chanName.Contains("\\"))
            {
                chanName = chanName.Split('\\')[1];
                TreeView1.Nodes[netname].Nodes.RemoveByKey(chanName);
            }
            else
            {
                TreeView1.Nodes.RemoveByKey(chanName);
            }
            TreeView1.Sort();
            TreeView1.SelectedNode = mynode;
        }

		static readonly ReadOnlyCollection<Color> IRCColors = new ReadOnlyCollection<Color>(new Color[] {
		    Color.White,
		    Color.Black,
		    Color.FromArgb(0x7f),
		    Color.FromArgb(0x9300),
		    Color.Red,
		    Color.FromArgb(0x7f0000),
		    Color.FromArgb(0x9c009c),
		    Color.FromArgb(0xfc7f00),
		    Color.Yellow,
		    Color.FromArgb(0xfc00),
		    Color.FromArgb(0x9393),
		    Color.Aqua,
		    Color.FromArgb(0xfc),
		    Color.Magenta,
		    Color.FromArgb(0x7f7f7f),
		    Color.FromArgb(0xd2d2d2)
        });
		
		public delegate void MessageReceive(string channel, string message, bool newtab);
        internal MessageReceive MessageReceiveDelegate;
        private void MessageReceiveMethod(string channel, string message, bool newtab)
        {
            GUIChanInfo dictent = GetDictionaryEntry(channel);
            string netname = channel.Split('\\')[0];
            string channame = channel;
            if (channel.Contains("\\"))
            {
                channame = channel.Split('\\')[1];
                if (channame.Equals(netname, StringComparison.CurrentCultureIgnoreCase))
                    dictent = GetDictionaryEntry(netname);
            }
            if (newtab)
            {
                if (dictent == null)
                {
                    AddChannelMethod(channel);
                    dictent = GetDictionaryEntry(channel);
                }
                else
                {
                    if (channel.Contains("\\") & !channame.Equals(netname, StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (!TreeView1.Nodes[netname].Nodes.ContainsKey(channame))
                            TreeView1.SelectedNode = TreeView1.Nodes[netname].Nodes.Add(channame, channame);
                    }
                    else if (!TreeView1.Nodes.ContainsKey(netname))
                    {
                        TreeView1.SelectedNode = TreeView1.Nodes.Add(netname, netname);
                    }
                }
            }
            else
            {
                if (channel.Contains("\\") & !channame.Equals(netname, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!TreeView1.Nodes[netname].Nodes.ContainsKey(channame))
                        dictent = GetDictionaryEntry(TreeView1.SelectedNode.FullPath);
                }
                else if (!TreeView1.Nodes.ContainsKey(netname))
                {
                    dictent = GetDictionaryEntry(TreeView1.SelectedNode.FullPath);
                }
            }
            dictent.textbox.SelectionStart = dictent.textbox.TextLength;
            dictent.textbox.AppendText("[" + DateTime.Now.ToString("HH:mm") + "] ");
            Font fnt = dictent.textbox.Font;
            for (int i = 0; i <= message.Length - 1; i++)
            {
                dictent.textbox.SelectionFont = fnt;
                switch (message[i])
                {
                    case Module1.ColorChar:
                        try
                        {
                            i += 1;
                            if (char.IsNumber(message, i))
                            {
                                if (char.IsNumber(message, i + 1))
                                {
                                    dictent.textbox.SelectionColor = IRCColors[int.Parse(message.Substring(i, 2)) % 16];
                                    i += 2;
                                }
                                else
                                {
                                    dictent.textbox.SelectionColor = IRCColors[int.Parse(message.Substring(i, 1)) % 16];
                                    i += 1;
                                }
                                if (message[i] == ',')
                                {
                                    i += 1;
                                    if (char.IsNumber(message, i + 1) & char.IsNumber(message, i))
                                    {
                                        dictent.textbox.SelectionBackColor = IRCColors[int.Parse(message.Substring(i, 2)) % 16];
                                        i += 2;
                                    }
                                    else if (char.IsNumber(message, i))
                                    {
                                        dictent.textbox.SelectionBackColor = IRCColors[int.Parse(message.Substring(i, 1)) % 16];
                                        i += 1;
                                    }
                                }
                            }
                            else
                            {
                                dictent.textbox.SelectionColor = dictent.textbox.ForeColor;
                                dictent.textbox.SelectionBackColor = dictent.textbox.BackColor;
                            }
                            i -= 1;
                        }
                        catch (ArgumentOutOfRangeException) { }
                        break;
                    case Module1.RevChar:
                        Color x = dictent.textbox.SelectionColor;
                        dictent.textbox.SelectionColor = dictent.textbox.SelectionBackColor;
                        dictent.textbox.SelectionBackColor = x;
                        break;
                    case Module1.StopChar:
                        dictent.textbox.SelectionColor = dictent.textbox.ForeColor;
                        dictent.textbox.SelectionBackColor = dictent.textbox.BackColor;
                        dictent.textbox.SelectionFont = dictent.textbox.Font;
                        break;
                    case Module1.BoldChar:
                        if (!fnt.Bold)
                            fnt = new Font(dictent.textbox.Font, FontStyle.Bold | fnt.Style);
                        else
                            fnt = new Font(dictent.textbox.Font, fnt.Style & (~FontStyle.Bold));
                        break;
                    case Module1.UnderChar:
                        if (!fnt.Underline)
                            fnt = new Font(dictent.textbox.Font, FontStyle.Underline | fnt.Style);
                        else
                            fnt = new Font(dictent.textbox.Font, fnt.Style & (~FontStyle.Underline));
                        break;
                    case Module1.ItalicChar:
                        if (!fnt.Italic)
                            fnt = new Font(dictent.textbox.Font, fnt.Style | FontStyle.Italic);
                        else
                            fnt = new Font(dictent.textbox.Font, fnt.Style & (~FontStyle.Italic));
                        break;
                    default:
                        dictent.textbox.AppendText(message[i].ToString());
                        break;
                }
            }
            dictent.textbox.SelectionColor = dictent.textbox.ForeColor;
            dictent.textbox.SelectionBackColor = dictent.textbox.BackColor;
            dictent.textbox.SelectionFont = dictent.textbox.Font;
            dictent.textbox.AppendText(Environment.NewLine);
            dictent.textbox.ScrollToCaret();
            try
            {
                if (TreeView1.SelectedNode != null && !channel.Equals(TreeView1.SelectedNode.FullPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (dictent.newlines < 2)
                        dictent.newlines = Module1.GetNetworkByName(netname) != null && message.ToLower().Contains(Module1.GetNetworkByName(netname).IrcNick.ToLower()) ? 2 : 1;
                    TreeView1.Invalidate();
                }
            }
            catch
            {
            }
        }

        public delegate void ChangeUser(IRC network, string oldnick, string newnick);
        internal ChangeUser ChangeUserDelegate;
        private void ChangeUserMethod(IRC network, string oldnick, string newnick)
        {
            if (TreeView1.Nodes[network.name].Nodes.ContainsKey(oldnick))
            {
                TreeNode nod = TreeView1.Nodes[network.name].Nodes[oldnick];
                nod.Name = newnick;
                nod.Text = newnick;
            }
            if (GetDictionaryEntry(network.name + "\\" + oldnick) != null)
            {
                Module1.chanscrollback.Add(network.name + "\\" + newnick, GetDictionaryEntry(network.name + "\\" + oldnick));
                Module1.chanscrollback.Remove(network.name + "\\" + oldnick);
            }
        }

        public delegate void ClearScrollback(string channel);
        internal ClearScrollback ClearScrollbackDelegate;
        private void ClearScrollbackMethod(string channel)
        {
            GUIChanInfo x = GetDictionaryEntry(channel);
            if (x != null)
            {
                x.textbox.Rtf = string.Empty;
                x.newlines = 0;
            }
        }

        EventWaitHandle mon = new EventWaitHandle(false, EventResetMode.AutoReset);
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Thread t = new Thread(CloseConnections);
            t.Start();
            mon.WaitOne(10000);
            if (t.ThreadState != ThreadState.Stopped)
                t.Abort();
            Module1.Quit();
        }

        void CloseConnections()
        {
            foreach (IRC IrcObject in Module1.IrcApp.IrcObjects)
                IrcObject.Disconnect("Shutting down...");
            mon.Set();
        }

        Dialog1 dlg1 = new Dialog1();
        private void Form1_Load(object sender, EventArgs e)
        {
            Module1.Debug = System.Diagnostics.Debugger.IsAttached;
            Module1.myForm = this;
            AddChannelDelegate = new AddChannel(AddChannelMethod);
            RemoveChannelDelegate = new RemoveChannel(RemoveChannelMethod);
            MessageReceiveDelegate = new MessageReceive(MessageReceiveMethod);
            ChangeUserDelegate = new ChangeUser(ChangeUserMethod);
            ClearScrollbackDelegate = new ClearScrollback(ClearScrollbackMethod);
#if !DEBUG
            BackgroundWorker1.RunWorkerAsync();
        }

        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
#endif
            foreach (string item in System.IO.Directory.GetFiles(Environment.CurrentDirectory, "MMBotCode*.dll"))
                System.IO.File.Delete(item);
            Module1.servinf = ServerInfo.Load("MMBot.ini");
            if (System.IO.File.Exists("global.ini"))
            {
                GlobalSettings settings = GlobalSettings.Load("global.ini");
                Module1.BanList = settings.BanList;
                Module1.IgnoreList = settings.IgnoreList;
                Module1.OpName = settings.OpName;
                Module1.password = settings.Password;
            }
            Module1.IrcApp = new cIRC();
            foreach (ServerInfo inf in Module1.servinf)
                Module1.IrcApp.AddConnection(inf);
            Module1.IrcApp.LoadCommands();
            Module1.server.Start();
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;
#if !DEBUG
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
#endif
            bool connected = false;
            foreach (ServerInfo inf in Module1.servinf)
            {
                dlg1.ListBox1.Items.Add(inf.name);
                if (inf.autoconnect)
                {
                    connected = true;
                    ConnectNetwork(inf);
                }
            }
            if (!connected)
            {
                dlg1.ListBox1.SelectedIndex = 0;
                dlg1.ShowDialog(this);
                ConnectNetwork(Module1.servinf[dlg1.ListBox1.SelectedIndex]);
            }
            Module1.remindertimer.Elapsed += Module1.remindertimer_Elapsed;
            Module1.savetimer.Elapsed += Module1.savetimer_Elapsed;
            Module1.feedtimer.Elapsed += Module1.feedtimer_Elapsed;
        }

#if DEBUG
        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        { }
        private void BackgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        { }
#endif

        TreeNode clickitem;
        private void TreeView1_DrawNode(object sender, System.Windows.Forms.DrawTreeNodeEventArgs e)
        {
            try
            {
                // Define the default color of the brush as black.
                Brush myBrush = Brushes.Black;

                // Determine the color of the brush to draw each item based on   
                // the index of the item to draw.
                switch (GetDictionaryEntry(e.Node.FullPath).newlines)
                {
                    case 1:
                        myBrush = Brushes.Red;
                        break;
                    case 2:
                        myBrush = Brushes.Blue;
                        break;
                }
                // Draw the current item text based on the current 
                // Font and the custom brush settings.
                e.Graphics.DrawString(e.Node.Name, TreeView1.Font, myBrush, e.Bounds.X, e.Bounds.Y);
            }
            catch
            {
            }
        }

        private void ContextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (clickitem.Parent == null)
                e.Cancel = true;
        }

        private void RemoveToolStripMenuItem_Click(System.Object sender, System.EventArgs e)
        {
            if (clickitem.Name.StartsWith("#"))
            {
                Module1.GetNetworkByName(clickitem.Parent.Name).QueueWrite("PART " + clickitem.Name);
                Module1.GetNetworkByName(clickitem.Parent.Name).GetChannel(clickitem.Name).Active = false;
            }
            RemoveChannelMethod(clickitem.FullPath);
        }

        internal void RichTextBox1_LinkClicked(object sender, System.Windows.Forms.LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void TreeView1_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            GUIChanInfo x = GetDictionaryEntry(e.Node.FullPath);
            TableLayoutPanel1.SuspendLayout();
            TableLayoutPanel1.Controls.Remove(TableLayoutPanel1.GetControlFromPosition(1, 0));
            TableLayoutPanel1.Controls.Add(x.textbox, 1, 0);
            TableLayoutPanel1.ResumeLayout();
            x.newlines = 0;
            TreeView1.Invalidate();
            TextBox2.Select();
        }

        private void TreeView1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            TreeViewHitTestInfo hit = TreeView1.HitTest(e.Location);
            if (hit != null)
                clickitem = hit.Node;
        }

        private void NetworkToolStripMenuItem_Click(System.Object sender, System.EventArgs e)
        {
            if (dlg1.ShowDialog(this) == DialogResult.OK)
                ConnectNetwork(Module1.servinf[dlg1.ListBox1.SelectedIndex]);
        }

        internal void ConnectNetwork(ServerInfo network)
        {
            IRC net = Module1.GetNetworkByName(network.name);
            if (net.Connected) return;
            Invoke(MessageReceiveDelegate, network.name, "Connecting to " + network.name + "...", true);
            if (string.IsNullOrEmpty(net.IrcNick))
            {
                using (NetworkInfoDialog dlg = new NetworkInfoDialog())
                {
                    if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        net.IrcNick = dlg.textBox1.Text;
                        net.NSPass = dlg.textBox2.Text;
                    }
                    else
                        return;
                }
            }
            net.Connect();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NotifyIcon1.Visible = false;
            Show();
            WindowState = laststate;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        FormWindowState laststate = FormWindowState.Maximized;
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                NotifyIcon1.Visible = true;
            }
            else
            {
                laststate = WindowState;
            }
        }

        private void NotifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            NotifyIcon1.Visible = false;
            Show();
            WindowState = laststate;
        }
    }
}
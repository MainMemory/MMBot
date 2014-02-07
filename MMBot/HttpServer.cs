using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Collections.Specialized;
using System.Linq;

namespace MMBot
{
    public class HttpServer
    {
        class PageHit
        {
            public PageHit(HttpListenerRequest request, HttpListenerResponse response)
            {
                Timestamp = DateTime.UtcNow;
                Address = request.RemoteEndPoint.Address;
                UserAgent = request.UserAgent ?? "None Specified";
                Page = request.RawUrl;
                Status = response.StatusCode + " " + response.StatusDescription;
            }

            public DateTime Timestamp { get; private set; }
            public IPAddress Address { get; private set; }
            public string UserAgent { get; private set; }
            public string Page { get; private set; }
            public string Status { get; private set; }
        }

		public const string Hostname = "localhost";
        private HttpListener listener = new HttpListener();
        private string servpath = Path.Combine(Environment.CurrentDirectory, "http");
        private Thread listenThread;
        List<PageHit> hits = new List<PageHit>();

        private static readonly DateTime cookiedate = DateTime.Now.AddYears(100);

        public void Start()
        {
			return;
			// If you want to enable the webserver you will have to use the "netsh http add urlacl" command, or run MMBot as an administrator.
            listener.Prefixes.Add("http://" + Hostname + ":80/");
            listener.Start();
            listenThread = new Thread(Listen);
            listenThread.Start();
        }

        private void Listen()
        {
            while (!Module1.quitting)
            {
                try { new Thread(ProcessRequest).Start(listener.GetContext()); }
                catch (HttpListenerException) { }
            }
        }

        public void Stop()
        {
            listener.Stop();
            listenThread.Abort();
        }

        object sync = new object();
        private void ProcessRequest(object param)
        {
            try
            {
                HttpListenerContext context = (HttpListenerContext)param;
                HttpListenerRequest request = context.Request;
                Dictionary<string, string> cookies = new Dictionary<string, string>();
                foreach (Cookie item in request.Cookies)
                    cookies.Add(item.Name, item.Value);
                HttpListenerResponse response = context.Response;
                Stream respstr = response.OutputStream;
                string ur = request.RawUrl;
                NameValueCollection options = request.QueryString;
                if (ur.IndexOf('?') > -1)
                    ur = ur.Remove(ur.IndexOf('?'));
                ur = Uri.UnescapeDataString(ur);
                bool admin = IPAddress.IsLoopback(request.RemoteEndPoint.Address);
                switch (request.HttpMethod)
                {
                    case "GET":
                    case "HEAD":
                        Stream page = null;
                        if (ur == "/")
                        {
                            lock (sync)
                                page = new MemoryStream(Encoding.UTF8.GetBytes(MainPage(options, cookies, admin)));
                            response.ContentType = "text/html";
                        }
                        else if (ur == "/settheme")
                        {
                            if (array_key_exists("theme", options)) response.AppendCookie(new Cookie("theme", options["theme"]) { Expires = cookiedate });
                            if (array_key_exists("troll", options)) response.AppendCookie(new Cookie("troll", options["troll"]) { Expires = cookiedate });
                            response.Redirect((request.UrlReferrer ?? new Uri("/?page=themes", UriKind.Relative)).ToString());
                            hits.Add(new PageHit(request, response));
                            response.Close();
                            break;
                        }
                        else if (ur == "/coffee.html")
                        {
                            response.StatusCode = 418;
                            response.StatusDescription = "I'm a teapot";
                            page = new MemoryStream(Encoding.UTF8.GetBytes("<html><head><title>418 I'm a teapot</title></head><body><h1>I'm a teapot</h1><p>You requested coffee, but this server is a teapot.</p></body></html>"));
                            response.ContentType = "text/html";
                        }
                        else if (ur == "/myadmin/scripts/setup.php" || ur == "/MyAdmin/scripts/setup.php" ||
                            ur == "/phpmyadmin/scripts/setup.php" || ur == "/pma/scripts/setup.php" || ur == "/cgi-bin/php")
                        {
                            page = new MemoryStream(Encoding.UTF8.GetBytes("You can try as much as you want, but this server isn't running PHP."));
                            response.ContentType = "text/plain";
                        }
                        else if (File.Exists(servpath + ur))
                        {
                            response.ContentType = (string)Microsoft.Win32.Registry.GetValue("HKey_Classes_Root\\" + Path.GetExtension(ur), "Content Type", "application/octet-stream");
                            response.AddHeader("Last-Modified", File.GetLastWriteTimeUtc(servpath + ur).ToString("r"));
                            int tries = 0;
                        retry:
                            try { page = File.OpenRead(servpath + ur); }
                            catch (IOException) { if (tries++ < 10) { Thread.Sleep(10); goto retry; } }
                            catch (UnauthorizedAccessException)
                            {
                                response.StatusCode = 403;
                                response.StatusDescription = "Forbidden";
                                response.ContentType = "text/html";
                                page = new MemoryStream(Encoding.UTF8.GetBytes("<html><head><title>403 Forbidden</title></head><body><h1>Forbidden</h1><p>You don't have permission to access " + ur + " on this server.</p></body></html>"));
                            }
                        }
                        else
                        {
                            response.StatusCode = 404;
                            response.StatusDescription = "Not Found";
                            response.ContentType = "text/html";
                            page = new MemoryStream(Encoding.UTF8.GetBytes("<html><head><title>404 Not Found</title></head><body><h1>Not Found</h1><p>The requested URL " + ur + " was not found on this server.</p></body></html>"));
                        }
                        response.ContentLength64 = page.Length;
                        if (request.HttpMethod == "GET")
                            page.CopyTo(respstr);
                        page.Close();
                        if (!ur.StartsWith("/feeds/"))
                            hits.Add(new PageHit(request, response));
                        try { respstr.Flush(); respstr.Close(); response.Close(); }
                        catch { }
                        break;
                    case "POST":
                        if (!admin) goto default;
                        StreamReader sr = new StreamReader(request.InputStream);
                        NameValueCollection postData = HttpUtility.ParseQueryString(sr.ReadToEnd());
                        sr.Close();
                        try
                        {
                            switch (postData["page"])
                            {
                                case "stats":
                                    switch (postData["action"])
                                    {
                                        case "delete":
                                            foreach (string key in postData.AllKeys)
                                                if (key.StartsWith("user_"))
                                                {
                                                    string[] userparts = key.Substring(5).Split('.');
                                                    List<IRC> networks = Module1.IrcApp.IrcObjects;
                                                    if (userparts[0] != "*")
                                                        networks = new List<IRC>() { Module1.GetNetworkByName(userparts[0]) };
                                                    foreach (IRC network in networks)
                                                        if (network != null)
                                                        {
                                                            List<IRCChannel> channels = network.IrcChannels;
                                                            if (userparts[1] != "*")
                                                                channels = new List<IRCChannel>() { network.GetChannel(userparts[1]) };
                                                            foreach (IRCChannel channel in channels)
                                                                if (channel != null)
                                                                {
                                                                    IRCChanUserStats stats = channel.GetUserStats(userparts[2], false);
                                                                    if (stats != null)
                                                                        channel.stats.Remove(stats);
                                                                }
                                                        }
                                                }
                                            response.Redirect(request.UrlReferrer.ToString());
                                            break;
                                        case "combine":
                                            foreach (string key in postData.AllKeys)
                                                if (key.StartsWith("user_"))
                                                {
                                                    string[] userparts = key.Substring(5).Split('.');
                                                    List<IRC> networks = Module1.IrcApp.IrcObjects;
                                                    if (userparts[0] != "*")
                                                        networks = new List<IRC>() { Module1.GetNetworkByName(userparts[0]) };
                                                    foreach (IRC network in networks)
                                                        if (network != null)
                                                        {
                                                            if (!object.ReferenceEquals(network.GetAliases(userparts[2]), network.GetAliases(postData["user"])))
                                                            {
                                                                List<string> aliases = network.GetAliases(userparts[2]);
                                                                network.Aliases.Remove(aliases);
                                                                foreach (string item in aliases)
                                                                    network.AddAlias(postData["user"], item);
                                                            }
                                                        }
                                                }
                                            response.Redirect(request.UrlReferrer.ToString());
                                            break;
                                        default:
                                            response.StatusCode = 501;
                                            response.StatusDescription = "Not Implemented";
                                            break;
                                    }
                                    break;
                                default:
                                    response.StatusCode = 501;
                                    response.StatusDescription = "Not Implemented";
                                    break;
                            }
                        }
                        catch { }
                        hits.Add(new PageHit(request, response));
                        response.Close();
                        break;
                    default:
                        response.StatusCode = 501;
                        response.StatusDescription = "Not Implemented";
                        hits.Add(new PageHit(request, response));
                        response.Close();
                        break;
                }
            }
            catch (HttpListenerException)
            {
            }
        }

        static string[] irccolors = { "#ffffff", "#000000", "#00007f", "#009300", "#ff0000", "#7f0000", "#9c009c", "#fc7f00", "#ffff00", "#00fc00", "#009393", "#00ffff", "#0000fc", "#ff00ff", "#7f7f7f", "#d2d2d2" };
        internal static string convert_irc_string(string message)
        {
            bool bold = false;
            bool underline = false;
            bool italic = false;
            string forecolor = null;
            string backcolor = null;
            bool stylechanged = false;
            bool plaintext = true;
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == Module1.ColorChar)
                {
                    try
                    {
                        i += 1;
                        if (char.IsDigit(message, i))
                        {
                            if (char.IsDigit(message[i + 1]))
                            {
                                forecolor = irccolors[int.Parse(message.Substring(i, 2), System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo) % 16];
                                i += 2;
                            }
                            else
                            {
                                forecolor = irccolors[int.Parse(message.Substring(i, 1), System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo) % 16];
                                i += 1;
                            }
                            if (message[i] == ',')
                            {
                                i += 1;
                                if (char.IsDigit(message[i + 1]) & char.IsDigit(message[i]))
                                {
                                    backcolor = irccolors[int.Parse(message.Substring(i, 2), System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo) % 16];
                                    i += 2;
                                }
                                else if (char.IsDigit(message[i]))
                                {
                                    backcolor = irccolors[int.Parse(message.Substring(i, 1), System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo) % 16];
                                    i += 1;
                                }
                            }
                            else
                                backcolor = null;
                        }
                        else
                        {
                            forecolor = null;
                            backcolor = null;
                        }
                        i -= 1;
                    }
                    catch { }
                    stylechanged = true;
                }
                else if (message[i] == Module1.RevChar)
                {
                    if (forecolor != null)
                    {
                        string x = forecolor;
                        if (backcolor != null)
                            forecolor = backcolor;
                        else
                            forecolor = irccolors[0];
                        backcolor = x;
                    }
                    else
                    {
                        forecolor = irccolors[0];
                        backcolor = irccolors[1];
                    }
                    stylechanged = true;
                }
                else if (message[i] == Module1.StopChar)
                {
                    bold = false;
                    underline = false;
                    italic = false;
                    forecolor = null;
                    backcolor = null;
                    stylechanged = true;
                }
                else if (message[i] == Module1.BoldChar)
                {
                    bold = !bold;
                    stylechanged = true;
                }
                else if (message[i] == Module1.UnderChar)
                {
                    underline = !underline;
                    stylechanged = true;
                }
                else if (message[i] == Module1.ItalicChar)
                {
                    italic = !italic;
                    stylechanged = true;
                }
                else if (message[i] == '\n')
                    output.Append("<br />");
                else
                {
                    if (stylechanged)
                        output.Append(generate_html_tag(bold, underline, italic, forecolor, backcolor, ref plaintext));
                    stylechanged = false;
                    output.Append(HttpUtility.HtmlEncode(message[i]));
                }
            }
            if (!plaintext)
                output.Append("</span>");
            return output.ToString();
        }

        static string generate_html_tag(bool bold, bool underline, bool italic, string forecolor, string backcolor, ref bool plaintext)
        {
            StringBuilder output = new StringBuilder();
            if (!plaintext)
                output.Append("</span>");
            if (bold | underline | italic | (forecolor != null) | (backcolor != null))
            {
                output.Append("<span style=\"");
                if (bold)
                    output.Append("font-weight:bold;");
                if (underline)
                    output.Append("text-decoration:underline;");
                if (italic)
                    output.Append("font-style:italic;");
                if (forecolor != null)
                    output.Append("color:" + forecolor + ";");
                if (backcolor != null)
                    output.Append("background-color:" + backcolor + ";");
                output.Append("\">");
                plaintext = false;
            }
            else
                plaintext = true;
            return output.ToString();
        }

        private string getpage, cookietheme, theme, cookietroll, troll;

        Dictionary<string, Dictionary<string, string>> pages = new Dictionary<string, Dictionary<string, string>>() {
            { "home", new Dictionary<string, string>() { { "title", "" }, { "menu", "Home" } } },
            { "stats", new Dictionary<string, string>() { { "title", "Channel Statistics" }, { "menu", "Statistics" } } },
            { "httpstats", new Dictionary<string, string>() { { "title", "HTTP Server Statistics" }, { "menu", "HTTP Server Stats" } } },
            { "help", new Dictionary<string, string>() { { "title", "Help" }, { "menu", "Help" } } },
            { "themes", new Dictionary<string, string>() { { "title", "Theme Switcher" }, { "menu", "Themes" } } }
        };

        Dictionary<string, string> themes = new Dictionary<string, string>() {
            { "default", "Default" },
            { "amy", "Default (Amy)" },
            { "plain", "Plain" },
            { "mspa", "MS Paint Adventures" },
            { "scratch", "Scratch" },
            { "sbahj", "Sweet Bro and Hella Jeff" },
            { "cascade", "Cascade" },
            { "pink", "Pink" },
            { "rand", "Random" },
            { "english", "English (<span style=\"color:red\">warning: flashing colors</span>)" }
        };

        Dictionary<string, string> trolls = new Dictionary<string, string>() {
            { "none", "None" },
            { "AA", "apocalypseArisen" },
            { "AT", "adiosToreador" },
            { "TA", "twinArmageddons" },
            { "CG", "carcinoGeneticist" },
            { "AC", "arsenicCatnip" },
            { "GA", "grimAuxiliatrix" },
            { "GC", "gallowsCalibrator" },
            { "AG", "arachnidsGrip" },
            { "CT", "centaursTesticle" },
            { "TC", "terminallyCapricious" },
            { "CA", "caligulasAquarium" },
            { "CC", "cuttlefishCuller" },
            { "UU", "uranianUmbra" },
            { "uu", "undyingUmbrage" },
            { "rand", "Random" },
            { "SBaHJ", "Sweet Bro and Hella Jeff" }
        };

        private string MainPage(NameValueCollection options, Dictionary<string, string> cookies, bool admin)
        {
            StringBuilder pageContent = new StringBuilder();
            try
            {
                pageContent.AppendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                pageContent.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\">");
                pageContent.AppendLine("<head>");
                pageContent.AppendLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"/>");
                getpage = options["page"] ?? "home";
                Dictionary<string, string> curpage = pages[getpage];
                cookietheme = "default";
                if (cookies.ContainsKey("theme")) cookietheme = cookies["theme"];
                theme = options["theme"] ?? cookietheme;
                if (DateTime.UtcNow.Month == 4 & DateTime.UtcNow.Day == 1)
                    theme = "sbahj";
                if (DateTime.UtcNow.Month == 4 & DateTime.UtcNow.Day == 13)
                    theme = "mspa";
                if (!themes.ContainsKey(theme)) theme = "default";
                if (theme == "rand")
                {
                    Random rand = new Random();
                    List<string> themekeys = new List<string>();
                    foreach (string item in themes.Keys)
                        themekeys.Add(item);
                    theme = themekeys[rand.Next(0, themekeys.Count - 1)];
                }
                pageContent.AppendLine("<link href=\"" + theme + ".css\" rel=\"stylesheet\" type=\"text/css\" media=\"all\" />");
                cookietroll = "none";
                if (cookies.ContainsKey("troll"))
                    cookietroll = cookies["troll"];
                troll = options["troll"] ?? (DateTime.UtcNow.Month == 4 & DateTime.UtcNow.Day == 1 ? "SBaHJ" : cookietroll);
                if (!trolls.ContainsKey(troll)) troll = "none";
                if (troll == "rand")
                {
                    Random rand = new Random();
                    List<string> trollkeys = new List<string>();
                    foreach (string item in trolls.Keys)
                        trollkeys.Add(item);
                    troll = trollkeys[rand.Next(0, trollkeys.Count - 1)];
                }
                pageContent.AppendLine("<link href=\"favicon.ico\" rel=\"shortcut icon\"/>");
                pageContent.Append("<title>MMBot");
                if (!string.IsNullOrEmpty(curpage["title"]))
                    pageContent.Append(" - " + curpage["title"]);
                pageContent.AppendLine("</title>");
                pageContent.AppendLine("<script type=\"text/javascript\" src=\"h3h3h3.js\"></script>");
                pageContent.AppendLine("<script type=\"text/javascript\" src=\"themes.js\"></script>");
                pageContent.AppendLine("</head>");
                pageContent.AppendLine("<body onload=\"trollify('" + troll + "'); setupTheme('" + theme + "');\">");
                pageContent.AppendLine("<table class=\"main\">");
                pageContent.AppendLine("<tr class=\"main\">");
                pageContent.AppendLine("<td class=\"menu\" id=\"siteMenu\">");
                pageContent.AppendLine("<h2>Menu</h2>");
                foreach (KeyValuePair<string, Dictionary<string, string>> item in pages)
                    if (!string.IsNullOrEmpty(item.Value["menu"]))
                    {
                        pageContent.Append("<p class=\"center\">");
                        if (!getpage.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                            pageContent.Append("<a href=\"?page=" + item.Key + "\">");
                        pageContent.Append(item.Value["menu"]);
                        if (!getpage.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                            pageContent.Append("</a>");
                        pageContent.AppendLine("</p>");
                    }
                pageContent.AppendLine("<p class=\"center\"><a href=\"http://sf94.reimuhakurei.net/wiki/index.php?title=MMBot\">Wiki</a></p>");
                pageContent.AppendLine("<p class=\"center\"><a href=\"http://mm.reimuhakurei.net/\">MainMemory&apos;s site</a></p>");
                pageContent.AppendLine("</td>");
                pageContent.AppendLine("<td class=\"body\" id=\"siteContent\">");
                switch (getpage)
                {
                    case "home":
                        pageContent.AppendLine("<h1><strong>MMBot IRC Bot by MainMemory</strong></h1>");
                        pageContent.AppendLine("<p class=\"left\">Welcome to MMBot's web interface! Select a page from the menu on the left.</p>");
                        break;
                    case "stats":
                        pageContent.Append(StatsPage(options, admin));
                        break;
                    case "httpstats":
                        pageContent.Append(HttpStatsPage(options));
                        break;
                    case "help":
                        pageContent.Append(HelpPage(options));
                        break;
                    case "themes":
                        pageContent.Append(ThemesPage(options, cookies));
                        break;
                }
                pageContent.AppendLine("</td>");
                pageContent.AppendLine("</tr>");
                pageContent.AppendLine("</table>");
                pageContent.AppendLine("</body>");
                pageContent.AppendLine("</html>");
            }
            catch (Exception ex)
            {
                pageContent.AppendLine("<pre>" + HttpUtility.HtmlEncode(ex.ToString()) + "</pre>");
            }
            return pageContent.ToString();

        }

        internal static Dictionary<string, string> statitems = new Dictionary<string, string>() {
            { "messages", "Messages" },
            { "words", "Words" },
            { "wordsperline", "WPL" },
            { "characters", "Chars" },
            { "charsperline", "CPL" },
            { "charsperword", "CPW" },
            { "actions", "Actions" },
            { "commands", "Commands" },
            { "kicks", "Kicks" },
            { "kicked", "Kicked" },
            { "joins", "Joins" },
            { "parts", "Parts" },
            { "quits", "Quits" },
            { "pingquits", "Ping Timeouts" },
            { "modes", "Modes" },
            { "lastaction", "Last Action" },
            { "onlinetime", "Online Time" }
        };

        private string StatsPage(NameValueCollection options, bool admin)
        {
            StringBuilder pageContent = new StringBuilder();
            try
            {
                pageContent.AppendLine("<h1><strong>IRC Channel Stats by MMBot</strong></h1>");
                Dictionary<string, dynamic> total = InitStats();
                if (!array_key_exists("network", options))
                {
                    if (array_key_exists("user", options))
                    {
                        string getuser = options["user"];
                        pageContent.AppendLine("<h2>Statistics for " + HttpUtility.HtmlEncode(getuser) + "</h2>");
                        List<Dictionary<string, dynamic>> people = new List<Dictionary<string, dynamic>>();
                        foreach (IRC network in Module1.IrcApp.IrcObjects)
                            foreach (IRCChannel channel in network.IrcChannels)
                                if (!channel.Hidden)
                                {
                                    IRCChanUserStats person = channel.GetUserStats(getuser, true);
                                    if (string.Equals(person.name, getuser, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Dictionary<string, dynamic> val = StatsToDict(person);
                                        val.Add("network", network.name);
                                        val.Add("channel", channel.Name);
                                        people.Add(val);
                                    }
                                }
                        if (people.Count > 0)
                        {
                            CalcTotal(total, people);
                            string sort = "channel";
                            string order = "a";
                            SortPeople(options, people, ref sort, ref order);
                            if (admin)
                            {
                                pageContent.AppendLine("<form action=\"/\" method=\"post\">");
                                pageContent.AppendLine("<input type=\"hidden\" name=\"page\" value=\"" + getpage + "\">");
                            }
                            Dictionary<string, string> statitems = new Dictionary<string, string>() { { "channel", "Channel" } };
                            statitems.AddRange(HttpServer.statitems);
                            WriteStatsTable(pageContent, sort, order, "?page=" + getpage + "&amp;user=" + Uri.EscapeDataString(getuser), statitems, people, total, admin, true, (person) => "?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(person["network"]) + "&amp;channel=" + Uri.EscapeDataString(person["channel"]), (person) => person["network"], (person) => person["channel"], (person) => getuser);
                            if (admin)
                            {
                                pageContent.AppendLine("<input type=\"hidden\" name=\"action\" value=\"delete\" />");
                                pageContent.AppendLine("<p class=\"left\"><input type=\"submit\" value=\"Delete\" /></p>");
                                pageContent.AppendLine("</form>");
                            }
                        }
                        else
                            pageContent.AppendLine("<p class=\"center\">User " + HttpUtility.HtmlEncode(getuser) + " not found.</p>");
                        pageContent.AppendLine("<p class=\"left\"><a href=\"?page=" + getpage + "\">&lt; Back to network list</a></p>");
                    }
                    else
                    {
                        List<Dictionary<string, dynamic>> networks = new List<Dictionary<string, dynamic>>();
                        total.Add("peakusers", 0ul);
                        total.Add("users", 0ul);
                        foreach (IRC network in Module1.IrcApp.IrcObjects)
                        {
                            Dictionary<string, dynamic> net = InitStats();
                            net.Add("name", network.name);
                            net.Add("peakusers", 0ul);
                            net.Add("users", 0ul);
                            List<Dictionary<string, dynamic>> items = new List<Dictionary<string, dynamic>>();
                            foreach (IRCChannel channel in network.IrcChannels)
                            {
                                foreach (IRCChanUserStats person in channel.stats)
                                    items.Add(StatsToDict(person));
                                net["peakusers"] = Math.Max(net["peakusers"], (ulong)channel.peakusers);
                                net["users"] = net["users"] + (ulong)channel.People.Count;
                                total["users"] = total["users"] + (ulong)channel.People.Count;
                            }
                            total["peakusers"] = total["peakusers"] + net["peakusers"];
                            CalcTotal(net, items);
                            networks.Add(net);
                        }
                        CalcTotal(total, networks);
                        Dictionary<string, List<Dictionary<string, dynamic>>> tmpppl = new Dictionary<string, List<Dictionary<string, dynamic>>>(StringComparer.OrdinalIgnoreCase);
                        foreach (IRC network in Module1.IrcApp.IrcObjects)
                        {
                            foreach (IRCChannel channel in network.IrcChannels)
                                foreach (IRCChanUserStats person in channel.stats)
                                {
                                    if (tmpppl.ContainsKey(person.name))
                                        tmpppl[person.name].Add(StatsToDict(person));
                                    else
                                        tmpppl.Add(person.name, new List<Dictionary<string, dynamic>>() { StatsToDict(person) });
                                }
                            foreach (List<string> aliaslist in network.Aliases)
                                for (int i = 0; i < aliaslist.Count; i++)
                                    if (tmpppl.ContainsKey(aliaslist[i]))
                                    {
                                        for (int j = i + 1; j < aliaslist.Count; j++)
                                            if (tmpppl.ContainsKey(aliaslist[j]))
                                            {
                                                tmpppl[aliaslist[i]].AddRange(tmpppl[aliaslist[j]]);
                                                tmpppl.Remove(aliaslist[j]);
                                            }
                                        break;
                                    }
                        }
                        List<Dictionary<string, dynamic>> people = new List<Dictionary<string, dynamic>>();
                        foreach (KeyValuePair<string, List<Dictionary<string, dynamic>>> p in tmpppl)
                        {
                            Dictionary<string, dynamic> person = InitStats();
                            person.Add("name", p.Key);
                            CalcTotal(person, p.Value);
                            people.Add(person);
                        }
                        string sort = "messages";
                        string order = "d";
                        SortPeople(options, networks, ref sort, ref order);
                        SortPeople(options, people, ref sort, ref order);
                        if (people.Count > 25)
                            people.RemoveRange(25, people.Count - 25);
                        pageContent.AppendLine("<h2>Network Statistics</h2>");
                        Dictionary<string, string> statitems = new Dictionary<string, string>() { { "name", "Name" } };
                        statitems.AddRange(HttpServer.statitems);
                        statitems.Add("peakusers", "Peak Users");
                        statitems.Add("users", "Users");
                        WriteStatsTable(pageContent, sort, order, "?page=" + getpage, statitems, networks, total, false, true, (network) => "?page=" + getpage + "&amp;network=" + network["name"], null, null, null);
                        string urlhead = "<a href=\"?page=" + getpage;
                        pageContent.AppendLine("<h2>Top 25 Users</h2>");
                        if (admin)
                        {
                            pageContent.AppendLine("<form action=\"/\" method=\"post\">");
                            pageContent.AppendLine("<input type=\"hidden\" name=\"page\" value=\"" + getpage + "\">");
                        }
                        statitems = new Dictionary<string, string>() { { "name", "Name" } };
                        statitems.AddRange(HttpServer.statitems);
                        WriteStatsTable(pageContent, sort, order, "?page=" + getpage, statitems, people, null, admin, false, (person) => "?page=" + getpage + "&amp;user=" + Uri.EscapeDataString(person["name"]), (person) => "*", (person) => "*", (person) => person["name"]);
                        if (admin)
                        {
                            pageContent.AppendLine("<p class=\"left\">Action: <input type=\"radio\" name=\"action\" value=\"delete\" checked=\"checked\" /> Delete <input type=\"radio\" name=\"action\" value=\"combine\" /> Combine</p>");
                            pageContent.AppendLine("<p class=\"left\">Name: <input type=\"text\" name=\"user\" /></p>");
                            pageContent.AppendLine("<p class=\"left\"><input type=\"submit\" value=\"Go\" /></p>");
                            pageContent.AppendLine("</form>");
                        }
                        pageContent.AppendLine("<p class=\"center\">Search for user:</p>");
                        pageContent.AppendLine("<form action=\"\">");
                        pageContent.AppendLine("<input type=\"hidden\" name=\"page\" value=\"" + getpage + "\" />");
                        pageContent.AppendLine("<div style=\"text-align: center\"><input type=\"text\" name=\"user\" /><input type=\"submit\" value=\"Go\" /></div>");
                        pageContent.AppendLine("</form>");
                    }
                }
                else
                {
                    string getnet = options["network"];
                    IRC network = Module1.GetNetworkByName(getnet);
                    if (network == null)
                    {
                        pageContent.AppendLine("<p class=\"center\">Network " + HttpUtility.HtmlEncode(getnet) + " not found.</p>");
                        pageContent.AppendLine("<p class=\"left\"><a href=\"?page=" + getpage + "\">&lt; Back to network list</a></p>");
                    }
                    else if (!array_key_exists("channel", options))
                    {
                        if (array_key_exists("user", options))
                        {
                            string getuser = options["user"];
                            pageContent.AppendLine("<h2>Statistics for " + HttpUtility.HtmlEncode(getuser) + " on " + HttpUtility.HtmlEncode(getnet) + "</h2>");
                            List<Dictionary<string, dynamic>> people = new List<Dictionary<string, dynamic>>();
                            foreach (IRCChannel channel in network.IrcChannels)
                                if (!channel.Hidden)
                                {
                                    IRCChanUserStats person = channel.GetUserStats(getuser, true);
                                    if (person != null)
                                    {
                                        Dictionary<string, dynamic> val = StatsToDict(person);
                                        val.Add("channel", channel.Name);
                                        people.Add(val);
                                    }
                                }
                            if (people.Count > 0)
                            {
                                CalcTotal(total, people);
                                string sort = "channel";
                                string order = "a";
                                SortPeople(options, people, ref sort, ref order);
                                string urlhead = "<a href=\"?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "&amp;user=" + Uri.EscapeDataString(getuser);
                                if (admin)
                                {
                                    pageContent.AppendLine("<form action=\"/\" method=\"post\">");
                                    pageContent.AppendLine("<input type=\"hidden\" name=\"page\" value=\"" + getpage + "\">");
                                }
                                Dictionary<string, string> statitems = new Dictionary<string, string>() { { "channel", "Channel" } };
                                statitems.AddRange(HttpServer.statitems);
                                WriteStatsTable(pageContent, sort, order, "?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "&amp;user=" + Uri.EscapeDataString(getuser), statitems, people, total, admin, true, (person) => "?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "&amp;channel=" + Uri.EscapeDataString(person["channel"]), (person) => getnet, (person) => person["channel"], (person) => getuser);
                                if (admin)
                                {
                                    pageContent.AppendLine("<input type=\"hidden\" name=\"action\" value=\"delete\" />");
                                    pageContent.AppendLine("<p class=\"left\"><input type=\"submit\" value=\"Delete\" /></p>");
                                    pageContent.AppendLine("</form>");
                                }
                            }
                            else
                                pageContent.AppendLine("<p class=\"center\">User " + HttpUtility.HtmlEncode(getuser) + " not found.</p>");
                            pageContent.AppendLine("<p class=\"left\"><a href=\"?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "\">&lt; Back to channel list</a></p>");
                        }
                        else
                        {
                            List<Dictionary<string, dynamic>> channels = new List<Dictionary<string, dynamic>>();
                            total.Add("peakusers", 0ul);
                            total.Add("users", 0ul);
                            foreach (IRCChannel channel in network.IrcChannels)
                            {
                                if (channel.Hidden) continue;
                                Dictionary<string, dynamic> chan = InitStats();
                                chan.Add("name", channel.Name);
                                List<Dictionary<string, dynamic>> items = new List<Dictionary<string, dynamic>>();
                                foreach (IRCChanUserStats person in channel.stats)
                                    items.Add(StatsToDict(person));
                                CalcTotal(chan, items);
                                chan.Add("peakusers", channel.peakusers);
                                chan.Add("users", (ulong)channel.People.Count);
                                total["peakusers"] = Math.Max(total["peakusers"], channel.peakusers);
                                total["users"] = total["users"] + (ulong)channel.People.Count;
                                channels.Add(chan);
                            }
                            CalcTotal(total, channels);
                            Dictionary<string, List<Dictionary<string, dynamic>>> tmpppl = new Dictionary<string, List<Dictionary<string, dynamic>>>(StringComparer.OrdinalIgnoreCase);
                            foreach (IRCChannel channel in network.IrcChannels)
                                foreach (IRCChanUserStats person in channel.stats)
                                {
                                    if (tmpppl.ContainsKey(person.name))
                                        tmpppl[person.name].Add(StatsToDict(person));
                                    else
                                        tmpppl.Add(person.name, new List<Dictionary<string, dynamic>>() { StatsToDict(person) });
                                }
                            foreach (List<string> aliaslist in network.Aliases)
                                for (int i = 0; i < aliaslist.Count; i++)
                                    if (tmpppl.ContainsKey(aliaslist[i]))
                                    {
                                        for (int j = i + 1; j < aliaslist.Count; j++)
                                            if (tmpppl.ContainsKey(aliaslist[j]))
                                            {
                                                tmpppl[aliaslist[i]].AddRange(tmpppl[aliaslist[j]]);
                                                tmpppl.Remove(aliaslist[j]);
                                            }
                                        break;
                                    }
                            List<Dictionary<string, dynamic>> people = new List<Dictionary<string, dynamic>>();
                            foreach (KeyValuePair<string, List<Dictionary<string, dynamic>>> p in tmpppl)
                            {
                                Dictionary<string, dynamic> person = InitStats();
                                person.Add("name", p.Key);
                                CalcTotal(person, p.Value);
                                people.Add(person);
                            }
                            string sort = "messages";
                            string order = "d";
                            SortPeople(options, channels, ref sort, ref order);
                            SortPeople(options, people, ref sort, ref order);
                            if (people.Count > 25)
                                people.RemoveRange(25, people.Count - 25);
                            pageContent.AppendLine("<h2>Channel Statistics for " + HttpUtility.HtmlEncode(getnet) + "</h2>");
                            string urlhead = "<a href=\"?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet);
                            Dictionary<string, string> statitems = new Dictionary<string, string>() { { "name", "Name" } };
                            statitems.AddRange(HttpServer.statitems);
                            statitems.Add("peakusers", "Peak Users");
                            statitems.Add("users", "Users");
                            WriteStatsTable(pageContent, sort, order, "?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet), statitems, channels, total, false, true, (channel) => "?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "&amp;channel=" + Uri.EscapeDataString(channel["name"]), null, null, null);
                            pageContent.AppendLine("<h2>Top 25 Users</h2>");
                            if (admin)
                            {
                                pageContent.AppendLine("<form action=\"/\" method=\"post\">");
                                pageContent.AppendLine("<input type=\"hidden\" name=\"page\" value=\"" + getpage + "\">");
                            }
                            statitems = new Dictionary<string, string>() { { "name", "Name" } };
                            statitems.AddRange(HttpServer.statitems);
                            WriteStatsTable(pageContent, sort, order, "?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet), statitems, people, null, admin, false, (person) => "?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "&amp;user=" + Uri.EscapeDataString(person["name"]), (person) => getnet, (person) => "*", (person) => person["name"]);
                            if (admin)
                            {
                                pageContent.AppendLine("<p class=\"left\">Action: <input type=\"radio\" name=\"action\" value=\"delete\" checked=\"checked\" /> Delete <input type=\"radio\" name=\"action\" value=\"combine\" /> Combine</p>");
                                pageContent.AppendLine("<p class=\"left\">Name: <input type=\"text\" name=\"user\" /></p>");
                                pageContent.AppendLine("<p class=\"left\"><input type=\"submit\" value=\"Go\" /></p>");
                                pageContent.AppendLine("</form>");
                            }
                            pageContent.AppendLine("<p class=\"center\">Search for user:</p>");
                            pageContent.AppendLine("<form action=\"\">");
                            pageContent.AppendLine("<input type=\"hidden\" name=\"page\" value=\"" + getpage + "\" />");
                            pageContent.AppendLine("<input type=\"hidden\" name=\"network\" value=\"" + HttpUtility.HtmlEncode(getnet) + "\" />");
                            pageContent.AppendLine("<div style=\"text-align: center\"><input type=\"text\" name=\"user\" /><input type=\"submit\" value=\"Go\" /></div>");
                            pageContent.AppendLine("</form>");
                            pageContent.AppendLine("<p class=\"left\"><a href=\"?page=" + getpage + "\">&lt; Back to network list</a></p>");
                        }
                    }
                    else
                    {
                        string getchan = options["channel"];
                        IRCChannel channel = network.GetChannel(getchan);
                        if (channel == null)
                        {
                            pageContent.AppendLine("<p class=\"center\">Channel " + HttpUtility.HtmlEncode(getchan) + " not found.</p>");
                            pageContent.AppendLine("<p class=\"left\"><a href=\"?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "\">&lt; Back to channel list</a></p>");
                        }
                        else
                        {
                            string keyword = options["keyword"] ?? string.Empty;
                            if (channel.Keyword != keyword)
                            {
                                pageContent.AppendLine("<h2>Enter Keyword for " + HttpUtility.HtmlEncode(getchan) + "</h2>");
                                pageContent.AppendLine("<form action=\"\">");
                                pageContent.AppendLine("<input type=\"hidden\" name=\"page\" value=\"" + getpage + "\" />");
                                pageContent.AppendLine("<input type=\"hidden\" name=\"network\" value=\"" + HttpUtility.HtmlEncode(getnet) + "\" />");
                                pageContent.AppendLine("<input type=\"hidden\" name=\"channel\" value=\"" + HttpUtility.HtmlEncode(getchan) + "\" />");
                                pageContent.AppendLine("<div style=\"text-align: center\"><input type=\"text\" name=\"keyword\" /><input type=\"submit\" value=\"Go\" /></div>");
                                pageContent.AppendLine("</form>");
                                pageContent.AppendLine("<p class=\"left\"><a href=\"?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "\">&lt; Back to channel list</a></p>");
                            }
                            else
                            {
                                List<IRCUser> users = new List<IRCUser>(channel.People);
                                users.Sort(new Comparison<IRCUser>((a, b) => { if (a.mode == b.mode) return a.name.CompareTo(b.name); else return -a.mode.CompareTo(b.mode); }));
                                List<Dictionary<string, dynamic>> stats = new List<Dictionary<string, dynamic>>();
                                Dictionary<string, IRCChanUserStats> tmpppl = new Dictionary<string, IRCChanUserStats>(channel.stats.Count, StringComparer.OrdinalIgnoreCase);
                                foreach (IRCChanUserStats person in channel.stats)
                                    tmpppl.Add(person.name, person);
                                foreach (List<string> aliaslist in network.Aliases)
                                    for (int i = 0; i < aliaslist.Count; i++)
                                        if (tmpppl.ContainsKey(aliaslist[i]))
                                        {
                                            for (int j = i + 1; j < aliaslist.Count; j++)
                                                if (tmpppl.ContainsKey(aliaslist[j]))
                                                {
                                                    IRCChanUserStats result = new IRCChanUserStats();
                                                    result.name = tmpppl[aliaslist[i]].name;
                                                    result.Combine(tmpppl[aliaslist[i]]);
                                                    result.Combine(tmpppl[aliaslist[j]]);
                                                    tmpppl[aliaslist[i]] = result;
                                                    tmpppl.Remove(aliaslist[j]);
                                                }
                                            break;
                                        }
                                foreach (KeyValuePair<string, IRCChanUserStats> person in tmpppl)
                                    stats.Add(StatsToDict(person.Value));
                                CalcTotal(total, stats);
                                string sort = "messages";
                                string order = "d";
                                SortPeople(options, stats, ref sort, ref order);
                                pageContent.AppendLine("<h2><a href=\"irc://" + Uri.EscapeDataString(network.IrcServer) + "/" + Uri.EscapeDataString(getchan.Substring(1)) + "\">" + HttpUtility.HtmlEncode(getchan) + "</a> [<a href=\"http://chat.mibbit.com/?server=" + Uri.EscapeDataString(network.IrcServer) + "&amp;channel=" + Uri.EscapeDataString(getchan) + (string.IsNullOrEmpty(keyword) ? "" : "&amp;key=" + Uri.EscapeDataString(keyword)) + "\">webchat</a>] on " + HttpUtility.HtmlEncode(getnet) + "</h2>");
                                pageContent.AppendLine("<p class=\"center\">Topic is: " + convert_irc_string(channel.Topic) + "</p>");
                                string urlhead = "?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "&amp;channel=" + Uri.EscapeDataString(getchan);
                                if (!string.IsNullOrEmpty(keyword))
                                    urlhead += "&amp;keyword=" + Uri.EscapeDataString(keyword);
                                pageContent.AppendLine("<table border=\"1\" class=\"center\">");
                                pageContent.AppendLine("<tr><th>Name</th><th>Host</th><th>Mode</th></tr>");
                                foreach (IRCUser person in users)
                                {
                                    pageContent.Append("<tr>");
                                    pageContent.Append("<td>");
                                    if (person.mode > UserModes.Normal)
                                        pageContent.Append(HttpUtility.HtmlEncode(network.prefixes[(int)person.mode - 1 + network.voiceind]));
                                    pageContent.Append("<a href=\"?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "&amp;user=" + Uri.EscapeDataString(person.name) + "\">" + HttpUtility.HtmlEncode(person.name) + "</a></td>");
                                    pageContent.Append("<td>" + HttpUtility.HtmlEncode(person.user + "@" + person.host) + "</td>");
                                    pageContent.Append("<td>");
                                    if (person.mode > UserModes.Normal)
                                        pageContent.Append(HttpUtility.HtmlEncode(person.mode.ToString() + " (+" + network.modechars[(int)person.mode - 1 + network.voiceind] + ")"));
                                    else
                                        pageContent.Append("Normal");
                                    pageContent.AppendLine("</td></tr>");
                                }
                                pageContent.AppendLine("</table>");
                                pageContent.AppendLine("<h2><a name=\"stats\">Statistics</a></h2>");
                                if (admin)
                                {
                                    pageContent.AppendLine("<form action=\"/\" method=\"post\">");
                                    pageContent.AppendLine("<input type=\"hidden\" name=\"page\" value=\"" + getpage + "\">");
                                }
                                Dictionary<string, string> statitems = new Dictionary<string, string>() { { "name", "Name" } };
                                statitems.AddRange(HttpServer.statitems);
                                WriteStatsTable(pageContent, sort, order, urlhead, statitems, stats, total, admin, true, (person) => "?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "&amp;user=" + Uri.EscapeDataString(person["name"]), (person) => getnet, (person) => getchan, (person) => person["name"]);
                                if (admin)
                                {
                                    pageContent.AppendLine("<p class=\"left\">Action: <input type=\"radio\" name=\"action\" value=\"delete\" checked=\"checked\" /> Delete <input type=\"radio\" name=\"action\" value=\"combine\" /> Combine</p>");
                                    pageContent.AppendLine("<p class=\"left\">Name: <input type=\"text\" name=\"user\" /></p>");
                                    pageContent.AppendLine("<p class=\"left\"><input type=\"submit\" value=\"Go\" /></p>");
                                    pageContent.AppendLine("</form>");
                                }
                                pageContent.AppendLine("<p class=\"left\"><a href=\"?page=" + getpage + "&amp;network=" + Uri.EscapeDataString(getnet) + "\">&lt; Back to channel list</a></p>");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                pageContent.AppendLine("<pre>" + HttpUtility.HtmlEncode(ex.ToString()) + "</pre>");
            }
            return pageContent.ToString();
        }

        private void WriteStatsTable(StringBuilder pageContent, string sort, string order, string currentpage, Dictionary<string, string> statitems, List<Dictionary<string, dynamic>> people, Dictionary<string, dynamic> total, bool admin, bool percentmode, Func<Dictionary<string, dynamic>, string> urlfunc, Func<Dictionary<string, dynamic>, string> netfunc, Func<Dictionary<string, dynamic>, string> chanfunc, Func<Dictionary<string, dynamic>, string> userfunc)
        {
            if (total == null) percentmode = false;
            pageContent.AppendLine("<table border=\"1\" class=\"center\">");
            pageContent.Append("<tr><th>#</th>");
            foreach (KeyValuePair<string, string> item in statitems)
            {
                string key = item.Key;
                string value = item.Value;
                pageContent.Append("<th");
                if (sort == key)
                    pageContent.Append(" class=\"highlight\"");
                pageContent.Append(">" + value + " ");
                if (sort != key | order != "a")
                    pageContent.Append("<a href=\"" + currentpage + "&amp;sort=" + key + "&amp;order=a#stats\">");
                pageContent.Append("&#9650;");
                if (sort != key | order != "a")
                    pageContent.Append("</a>");
                if (sort != key | order != "d")
                    pageContent.Append("<a href=\"" + currentpage + "&amp;sort=" + key + "&amp;order=d#stats\">");
                pageContent.Append("&#9660;");
                if (sort != key | order != "d")
                    pageContent.Append("</a>");
                pageContent.Append("</th>");
            }
            if (admin)
                pageContent.Append("<th></th>");
            pageContent.AppendLine("</tr>");
            int i = 1;
            foreach (Dictionary<string, dynamic> person in people)
            {
                bool first = true;
                string name = string.Empty;
                pageContent.Append("<tr><td>" + i++ + "</td>");
                foreach (KeyValuePair<string, string> item in statitems)
                {
                    string key = item.Key;
                    string value = item.Value;
                    pageContent.Append("<td");
                    if (sort == key)
                        pageContent.Append(" class=\"highlight\"");
                    if (key == "lastaction")
                    {
                        if (person["lastaction"] == DateTime.MinValue)
                            pageContent.Append(">Never</td>");
                        else
                            pageContent.Append(">" + Module1.ToStringCustShort(DateTime.Now - person["lastaction"], 3) + " ago,<br />" + convert_irc_string(person["lastmessage"]) + "</td>");
                    }
                    else if (key == "onlinetime")
                    {
                        pageContent.Append(">" + Module1.ToStringCustShort(person[key], 3));
                        if (percentmode)
                            pageContent.Append("<br />" + (total[key] > TimeSpan.Zero ? Math.Round((person["onlinetime"].Ticks / (double)total["onlinetime"].Ticks) * 100.0, 2) : 0) + "%");
                        pageContent.Append("</td>");
                    }
                    else if (key == "wordsperline" | key == "charsperline" | key == "charsperword")
                        pageContent.Append(">" + Math.Round(person[key], 2) + "</td>");
                    else if (first)
                    {
                        pageContent.Append("><a href=\"" + urlfunc(person) + "\">" + HttpUtility.HtmlEncode(person[key]) + "</a></td>");
                        name = person[key];
                        first = false;
                    }
                    else
                    {
                        pageContent.Append(">" + person[key]);
                        if (percentmode)
                            pageContent.Append("<br />" + (total[key] > 0 ? Math.Round((person[key] / (double)total[key]) * 100.0, 2) : 0) + "%");
                        pageContent.Append("</td>");
                    }
                }
                if (admin)
                    pageContent.Append("<td>" + make_checkbox("user_" + netfunc(person) + "." + chanfunc(person) + "." + userfunc(person), false) + "</td>");
                pageContent.AppendLine("</tr>");
            }
            if (total != null)
            {
                bool first = true;
                pageContent.Append("<tr><td></td>");
                foreach (KeyValuePair<string, string> item in statitems)
                {
                    string key = item.Key;
                    string value = item.Value;
                    pageContent.Append("<td");
                    if (sort == key)
                        pageContent.Append(" class=\"highlight\"");
                    if (key == "lastaction")
                        pageContent.Append(">" + Module1.ToStringCustShort(DateTime.Now - total["lastaction"], 3) + " ago,<br />" + convert_irc_string(total["lastmessage"]) + "</td>");
                    else if (key == "onlinetime")
                        pageContent.Append(">" + Module1.ToStringCustShort(TimeSpan.FromTicks(total[key].Ticks / (long)people.Count), 3) + "</td>");
                    else if (key == "wordsperline" | key == "charsperline" | key == "charsperword")
                        pageContent.Append(">" + Math.Round(total[key], 2) + "</td>");
                    else if (first)
                    {
                        pageContent.Append(">Average</td>");
                        first = false;
                    }
                    else
                        pageContent.Append(">" + Math.Round(total[key] / (double)people.Count, 2) + "</td>");
                }
                if (admin)
                    pageContent.Append("<td></td>");
                pageContent.AppendLine("</tr>");
                pageContent.Append("<tr><td></td>");
                first = true;
                foreach (KeyValuePair<string, string> item in statitems)
                {
                    string key = item.Key;
                    string value = item.Value;
                    pageContent.Append("<td");
                    if (sort == key)
                        pageContent.Append(" class=\"highlight\"");
                    if (key == "lastaction")
                        pageContent.Append(">" + Module1.ToStringCustShort(DateTime.Now - total["lastaction"], 3) + " ago,<br />" + convert_irc_string(total["lastmessage"]) + "</td>");
                    else if (key == "onlinetime")
                        pageContent.Append(">" + Module1.ToStringCustShort(total[key], 3) + "</td>");
                    else if (key == "wordsperline" | key == "charsperline" | key == "charsperword")
                        pageContent.Append(">" + Math.Round(total[key], 2) + "</td>");
                    else if (first)
                    {
                        pageContent.Append(">Total</td>");
                        first = false;
                    }
                    else
                        pageContent.Append(">" + total[key] + "</td>");
                }
                if (admin)
                    pageContent.Append("<td></td>");
                pageContent.AppendLine("</tr>");
            }
            pageContent.Append("</table>");
        }

        private static Dictionary<string, dynamic> InitStats()
        {
            Dictionary<string, dynamic> total = new Dictionary<string, dynamic>();
            foreach (string key in statitems.Keys)
                switch (key)
                {
                    case "lastaction":
                        total.Add(key, DateTime.MinValue);
                        break;
                    case "wordsperline":
                    case "charsperline":
                    case "charsperword":
                        total.Add(key, 0d);
                        break;
                    case "onlinetime":
                        total.Add(key, TimeSpan.Zero);
                        break;
                    default:
                        total.Add(key, 0ul);
                        break;
                }
            total.Add("lastmessage", string.Empty);
            return total;
        }

        private void SortPeople(NameValueCollection options, List<Dictionary<string, dynamic>> people, ref string sort, ref string order)
        {
            sort = options["sort"] ?? sort;
            order = options["order"] ?? order;
            string s = sort;
            try
            {
                people.Sort(new Comparison<Dictionary<string, dynamic>>((a, b) => (a[s]).CompareTo(b[s])));
                if (order == "d")
                    people.Reverse();
            }
            catch { }
        }

        private static void CalcTotal(Dictionary<string, dynamic> total, List<Dictionary<string, dynamic>> people)
        {
            foreach (string key in statitems.Keys)
                foreach (Dictionary<string, dynamic> person in people)
                    switch (key)
                    {
                        case "lastaction":
                            if (person["lastaction"] > total["lastaction"])
                            {
                                total["lastaction"] = person["lastaction"];
                                total["lastmessage"] = person["lastmessage"];
                            }
                            break;
                        case "onlinetime":
                            if (person[key] > total[key])
                                total[key] = person[key];
                            break;
                        case "wordsperline":
                        case "charsperline":
                        case "charsperword":
                            break;
                        default:
                            total[key] = total[key] + person[key];
                            break;
                    }
            total["wordsperline"] = total["messages"] != 0 ? total["words"] / Convert.ToDouble(total["messages"]) : 0d;
            total["charsperline"] = total["messages"] != 0 ? total["characters"] / Convert.ToDouble(total["messages"]) : 0d;
            total["charsperword"] = total["words"] != 0 ? total["characters"] / Convert.ToDouble(total["words"]) : 0d;
        }

        internal static Dictionary<string, dynamic> StatsToDict(IRCChanUserStats person)
        {
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            result.Add("name", person.name);
            result.Add("lastmessage", person.lastmessage ?? string.Empty);
            foreach (string item in statitems.Keys)
                result.Add(item, typeof(IRCChanUserStats).InvokeMember(item, System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, null, person, null));
            return result;
        }

        private bool array_key_exists(string p, NameValueCollection options)
        {
            return options[p] != null;
        }

        private string make_input(string name, string value)
        {
            return "<input type=\"text\" name=\"" + HttpUtility.HtmlEncode(name) + "\" id=\"" + HttpUtility.HtmlEncode(name) + "\" style=\"text-align:right\" value=\"" + HttpUtility.HtmlEncode(value) + "\" />";
        }

        private string make_input(string name, string value, int length)
        {
            return "<input type=\"text\" name=\"" + HttpUtility.HtmlEncode(name) + "\" id=\"" + HttpUtility.HtmlEncode(name) + "\" maxlength=\"" + length + "\" size=\"" + length + "\" style=\"text-align:right\" value=\"" + HttpUtility.HtmlEncode(value) + "\" />";
        }

        private string make_checkbox(string name, bool @checked)
        {
            string result = "<input type=\"checkbox\" name=\"" + HttpUtility.HtmlEncode(name) + "\" id=\"" + HttpUtility.HtmlEncode(name) + "\"";
            if (@checked)
                result += " checked=\"checked\"";
            return result + " />";
        }

        private string HttpStatsPage(NameValueCollection options)
        {
            StringBuilder pageContent = new StringBuilder();
            try
            {
                pageContent.AppendLine("<h1><strong>MMBot HTTP Server Stats</strong></h1>");
                if (array_key_exists("pg", options))
                {
                    string page = options["pg"];
                    pageContent.AppendFormat("<h2>Hits for page <a href=\"{0}\">{0}</a></h2>", HttpUtility.HtmlEncode(page));
                    pageContent.AppendLine();
                    pageContent.AppendLine("<table>");
                    pageContent.AppendLine("<tr><th>Timestamp</th><th>IP Address</th><th>User Agent</th><th>Status</th></tr>");
                    foreach (PageHit item in hits.Where((a) => a.Page == page).Reverse())
                    {
                        pageContent.AppendFormat("<tr><td>{0}</td><td><a href=\"?page=httpstats&ip={2}\">{1}</a></td><td><a href=\"?page=httpstats&ua={4}\">{3}</a></td><td>{5}</td></tr>", HttpUtility.HtmlEncode(item.Timestamp), HttpUtility.HtmlEncode(item.Address), HttpUtility.HtmlEncode(Uri.EscapeDataString(item.Address.ToString())), HttpUtility.HtmlEncode(item.UserAgent), HttpUtility.HtmlEncode(Uri.EscapeDataString(item.UserAgent)), HttpUtility.HtmlEncode(item.Status));
                        pageContent.AppendLine();
                    }
                    pageContent.AppendLine("</table>");
                }
                else if (array_key_exists("ip", options))
                {
                    IPAddress ip = IPAddress.Parse(options["ip"]);
                    pageContent.AppendFormat("<h2>Hits for IP Address {0}</h2>", HttpUtility.HtmlEncode(ip));
                    pageContent.AppendLine();
                    pageContent.AppendLine("<table>");
                    pageContent.AppendLine("<tr><th>Timestamp</th><th>Page</th><th>User Agent</th><th>Status</th></tr>");
                    foreach (PageHit item in hits.Where((a) => a.Address.Equals(ip)).Reverse())
                    {
                        pageContent.AppendFormat("<tr><td>{0}</td><td><a href=\"?page=httpstats&pg={2}\">{1}</a> (<a href=\"{1}\">Go</a>)</td><td><a href=\"?page=httpstats&ua={4}\">{3}</a></td><td>{5}</td></tr>", HttpUtility.HtmlEncode(item.Timestamp), HttpUtility.HtmlEncode(item.Page), HttpUtility.HtmlEncode(Uri.EscapeDataString(item.Page)), HttpUtility.HtmlEncode(item.UserAgent), HttpUtility.HtmlEncode(Uri.EscapeDataString(item.UserAgent)), HttpUtility.HtmlEncode(item.Status));
                        pageContent.AppendLine();
                    }
                    pageContent.AppendLine("</table>");
                }
                else if (array_key_exists("ua", options))
                {
                    string agent = options["ua"];
                    pageContent.AppendFormat("<h2>Hits for User Agent {0}</h2>", HttpUtility.HtmlEncode(agent));
                    pageContent.AppendLine();
                    pageContent.AppendLine("<table>");
                    pageContent.AppendLine("<tr><th>Timestamp</th><th>IP Address</th><th>Page</th><th>Status</th></tr>");
                    foreach (PageHit item in hits.Where((a) => a.UserAgent == agent).Reverse())
                    {
                        pageContent.AppendFormat("<tr><td>{0}</td><td><a href=\"?page=httpstats&ip={2}\">{1}</a></td><td><a href=\"?page=httpstats&pg={4}\">{3}</a> (<a href=\"{3}\">Go</a>)</td><td>{5}</td></tr>", HttpUtility.HtmlEncode(item.Timestamp), HttpUtility.HtmlEncode(item.Address), HttpUtility.HtmlEncode(Uri.EscapeDataString(item.Address.ToString())), HttpUtility.HtmlEncode(item.Page), HttpUtility.HtmlEncode(Uri.EscapeDataString(item.Page)), HttpUtility.HtmlEncode(item.Status));
                        pageContent.AppendLine();
                    }
                    pageContent.AppendLine("</table>");
                }
                else
                {
                    pageContent.AppendLine("<h2>50 Most Recent</h2>");
                    pageContent.AppendLine("<table class=\"center\">");
                    pageContent.AppendLine("<tr><th>Timestamp</th><th>IP Address</th><th>Page</th><th>User Agent</th><th>Status</th></tr>");
                    foreach (PageHit item in Enumerable.Reverse(hits).Take(Math.Min(hits.Count, 50)))
                    {
                        pageContent.AppendFormat("<tr><td>{0}</td><td><a href=\"?page=httpstats&ip={2}\">{1}</a></td><td><a href=\"?page=httpstats&pg={4}\">{3}</a> (<a href=\"{3}\">Go</a>)</td><td><a href=\"?page=httpstats&ua={6}\">{5}</a></td><td>{7}</td></tr>", HttpUtility.HtmlEncode(item.Timestamp), HttpUtility.HtmlEncode(item.Address), HttpUtility.HtmlEncode(Uri.EscapeDataString(item.Address.ToString())), HttpUtility.HtmlEncode(item.Page), HttpUtility.HtmlEncode(Uri.EscapeDataString(item.Page)), HttpUtility.HtmlEncode(item.UserAgent), HttpUtility.HtmlEncode(Uri.EscapeDataString(item.UserAgent)), HttpUtility.HtmlEncode(item.Status));
                        pageContent.AppendLine();
                    }
                    pageContent.AppendLine("</table>");
                    pageContent.AppendLine("<table style=\"border-style:none\">");
                    pageContent.AppendLine("<tr style=\"border-style:none\">");
                    pageContent.AppendLine("<td class=\"body\">");
                    pageContent.AppendLine("<h2>Top 50 Pages</h2>");
                    pageContent.AppendLine("<table class=\"center\">");
                    pageContent.AppendLine("<tr><th>Hits</th><th>Page</th></tr>");
                    {
                        Dictionary<string, int> pagehits = new Dictionary<string, int>();
                        foreach (PageHit hit in hits)
                            if (pagehits.ContainsKey(hit.Page))
                                pagehits[hit.Page]++;
                            else
                                pagehits[hit.Page] = 1;
                        List<KeyValuePair<string, int>> hitcnts = System.Linq.Enumerable.ToList(pagehits);
                        hitcnts.Sort((a, b) => -a.Value.CompareTo(b.Value));
                        if (hitcnts.Count > 50)
                            hitcnts.RemoveRange(50, hitcnts.Count - 50);
                        foreach (KeyValuePair<string, int> page in hitcnts)
                        {
                            pageContent.AppendFormat("<tr><td>{0}</td><td><a href=\"?page=httpstats&amp;pg={2}\">{1}</a> (<a href=\"{1}\">Go</a>)</td></tr>",
                                page.Value.ConvertToString(), HttpUtility.HtmlEncode(page.Key), HttpUtility.HtmlEncode(Uri.EscapeDataString(page.Key)));
                            pageContent.AppendLine();
                        }
                    }
                    pageContent.AppendLine("</table>");
                    pageContent.AppendLine("</td>");
                    pageContent.AppendLine("<td class=\"body\">");
                    pageContent.AppendLine("<h2>Top 50 IPs</h2>");
                    pageContent.AppendLine("<table class=\"center\">");
                    pageContent.AppendLine("<tr><th>Hits</th><th>IP</th></tr>");
                    {
                        Dictionary<IPAddress, int> pagehits = new Dictionary<IPAddress, int>();
                        foreach (PageHit hit in hits)
                            if (pagehits.ContainsKey(hit.Address))
                                pagehits[hit.Address]++;
                            else
                                pagehits[hit.Address] = 1;
                        List<KeyValuePair<IPAddress, int>> hitcnts = System.Linq.Enumerable.ToList(pagehits);
                        hitcnts.Sort((a, b) => -a.Value.CompareTo(b.Value));
                        if (hitcnts.Count > 50)
                            hitcnts.RemoveRange(50, hitcnts.Count - 50);
                        foreach (KeyValuePair<IPAddress, int> page in hitcnts)
                        {
                            pageContent.AppendFormat("<tr><td>{0}</td><td><a href=\"?page=httpstats&amp;ip={2}\">{1}</a></td></tr>",
                                page.Value.ConvertToString(), HttpUtility.HtmlEncode(page.Key), HttpUtility.HtmlEncode(Uri.EscapeDataString(page.Key.ToString())));
                            pageContent.AppendLine();
                        }
                    }
                    pageContent.AppendLine("</table>");
                    pageContent.AppendLine("</td>");
                    pageContent.AppendLine("<td class=\"body\">");
                    pageContent.AppendLine("<h2>Top 50 User Agents</h2>");
                    pageContent.AppendLine("<table class=\"center\">");
                    pageContent.AppendLine("<tr><th>Hits</th><th>Agent</th></tr>");
                    {
                        Dictionary<string, int> pagehits = new Dictionary<string, int>();
                        foreach (PageHit hit in hits)
                            if (pagehits.ContainsKey(hit.UserAgent))
                                pagehits[hit.UserAgent]++;
                            else
                                pagehits[hit.UserAgent] = 1;
                        List<KeyValuePair<string, int>> hitcnts = System.Linq.Enumerable.ToList(pagehits);
                        hitcnts.Sort((a, b) => -a.Value.CompareTo(b.Value));
                        if (hitcnts.Count > 50)
                            hitcnts.RemoveRange(50, hitcnts.Count - 50);
                        foreach (KeyValuePair<string, int> page in hitcnts)
                        {
                            pageContent.AppendFormat("<tr><td>{0}</td><td><a href=\"?page=httpstats&amp;ua={2}\">{1}</a></td></tr>",
                                page.Value.ConvertToString(), HttpUtility.HtmlEncode(page.Key), HttpUtility.HtmlEncode(Uri.EscapeDataString(page.Key)));
                            pageContent.AppendLine();
                        }
                    }
                    pageContent.AppendLine("</table>");
                    pageContent.AppendLine("</td>");
                    pageContent.AppendLine("</tr>");
                    pageContent.AppendLine("</table>");
                }
            }
            catch (Exception ex)
            {
                pageContent.AppendLine("<pre>" + HttpUtility.HtmlEncode(ex.ToString()) + "</pre>");
            }
            return pageContent.ToString();
        }

        private string HelpPage(NameValueCollection options)
        {
            StringBuilder pageContent = new StringBuilder();
            try
            {
                pageContent.AppendLine("<h1><strong>Help</strong></h1>");
                string getcommand = options["command"];
                if (getcommand == null)
                {
                    pageContent.AppendLine("<p class=\"left\">Select a command from the list.</p>");

                    Dictionary<string, List<BotCommand>[]> cmds = new Dictionary<string, List<BotCommand>[]>();
                    foreach (KeyValuePair<string, BotModule> item in Module1.IrcApp.Modules)
                    {
                        List<BotCommand>[] modcmds = new List<BotCommand>[7];
                        for (int i = 0; i < 7; i++)
                            modcmds[i] = new List<BotCommand>();
                        cmds.Add(item.Key, modcmds);
                    }
                    foreach (BotCommand cmd in Module1.IrcApp.CommandDictionary.Values)
                        cmds[cmd.Module][cmd.AccessLevel == UserModes.BotOp ? 6 : (int)cmd.AccessLevel].Add(cmd);
                    foreach (KeyValuePair<string, List<BotCommand>[]> item in cmds)
                    {
                        bool hascmds = false;
                        for (int i = 0; i < 7; i++)
                        {
                            item.Value[i].Sort((a, b) => a.Name.CompareTo(b.Name));
                            if (item.Value[i].Count > 0) hascmds = true;
                        }
                        if (!hascmds) continue;
                        pageContent.AppendLine("<ul>");
                        pageContent.AppendLine("<li>" + HttpUtility.HtmlEncode(item.Key));
                        pageContent.AppendLine("<ul>");
                        for (int i = 0; i < 7; i++)
                        {
                            if (item.Value[i].Count == 0) continue;
                            pageContent.AppendLine("<li>" + (i == 6 ? UserModes.BotOp.ToString() : ((UserModes)i).ToString()));
                            pageContent.AppendLine("<ul>");
                            foreach (BotCommand cmd in item.Value[i])
                                pageContent.AppendLine("<li><a href=\"?page=" + getpage + "&amp;command=" + Uri.EscapeDataString(cmd.Name) + "\">" + HttpUtility.HtmlEncode(cmd.Name) + "</a></li>");
                            pageContent.AppendLine("</ul></li>");
                        }
                        pageContent.AppendLine("</ul></li>");
                        pageContent.AppendLine("</ul>");
                    }
                }
                else
                {
                    BotCommand? cmd = Module1.IrcApp.GetCommand(getcommand);
                    if (!cmd.HasValue)
                    {
                        pageContent.AppendLine("<p class=\"left\">Command " + HttpUtility.HtmlEncode(getcommand) + " not found.</p>");
                        pageContent.AppendLine("<p class=\"left\"><a href=\"?page=" + getpage + "\">&lt; Back to command list</a></p>");
                    }
                    else
                    {
                        BotCommand[] cmds = Module1.IrcApp.GetCommands(getcommand);
                        pageContent.AppendLine("<h2>Help for " + HttpUtility.HtmlEncode(getcommand) + "</h2>");
                        pageContent.AppendLine("<p class=\"left\">Module: " + HttpUtility.HtmlEncode(cmd.Value.Module) + "</p>");
                        pageContent.AppendLine("<h3>Command Hierarchy</h3>");
                        string full = string.Empty;
                        foreach (BotCommand item in cmds)
                        {
                            if (full.Length > 0)
                                full += " ";
                            full += item.Name;
                            pageContent.AppendLine("<ul>");
                            pageContent.Append("<li>");
                            if (full != getcommand)
                                pageContent.Append("<a href=\"?page=" + getpage + "&amp;command=" + Uri.EscapeDataString(full) + "\">");
                            pageContent.Append(HttpUtility.HtmlEncode(item.Name));
                            if (full != getcommand)
                                pageContent.AppendLine("</a>");
                        }
                        if (cmd.Value.SubCommands.Count > 0)
                        {
                            pageContent.AppendLine("<ul>");
                            foreach (KeyValuePair<string, BotCommand> item in cmd.Value.SubCommands)
                                pageContent.AppendLine("<li><a href=\"?page=" + getpage + "&amp;command=" + Uri.EscapeDataString(getcommand + " " + item.Key) + "\">" + HttpUtility.HtmlEncode(item.Key) + "</a></li>");
                            pageContent.Append("</ul>");
                        }
                        pageContent.AppendLine("</li>");
                        for (int i = 0; i < cmds.Length - 1; i++ )
                            pageContent.AppendLine("</ul></li>");
                        pageContent.AppendLine("</ul>");
                        pageContent.AppendLine("<p class=\"left\">" + convert_help_string(cmd.Value.HelpText) + "</p>");
                        pageContent.AppendLine("<p class=\"left\">Access level: " + cmd.Value.AccessLevel.ToString() + "</p>");
                        pageContent.AppendLine("<p class=\"left\"><a href=\"?page=" + getpage + "\">&lt; Back to command list</a></p>");
                    }
                }
            }
            catch (Exception ex)
            {
                pageContent.AppendLine("<pre>" + HttpUtility.HtmlEncode(ex.ToString()) + "</pre>");
            }
            return pageContent.ToString();
        }

        private string convert_help_string(string message)
        {
            bool bold = false;
            bool underline = false;
            bool italic = false;
            string forecolor = null;
            string backcolor = null;
            bool stylechanged = false;
            bool plaintext = true;
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                if (message.SafeSubstring(i, 2) == "<c")
                {
                    if (message.SafeSubstring(i, 3) == "<c=")
                    {
                        string[] colors = message.Substring(i + 3, message.IndexOf('>', i) - (i + 3)).Split(',');
                        forecolor = irccolors[int.Parse(colors[0], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo) % 16];
                        if (colors.Length == 2)
                            backcolor = irccolors[int.Parse(colors[1], System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo) % 16];
                        else
                            backcolor = null;
                    }
                    else
                    {
                        forecolor = null;
                        backcolor = null;
                    }
                    i = message.IndexOf('>', i);
                    stylechanged = true;
                }
                else if (message.SafeSubstring(i, 3) == "<r>")
                {
                    i += 2;
                    if (forecolor != null)
                    {
                        string x = forecolor;
                        if (backcolor != null)
                            forecolor = backcolor;
                        else
                            forecolor = irccolors[0];
                        backcolor = x;
                    }
                    else
                    {
                        forecolor = irccolors[0];
                        backcolor = irccolors[1];
                    }
                    stylechanged = true;
                }
                else if (message.SafeSubstring(i, 3) == "<o>")
                {
                    i += 2;
                    bold = false;
                    underline = false;
                    italic = false;
                    forecolor = null;
                    backcolor = null;
                    stylechanged = true;
                }
                else if (message.SafeSubstring(i, 3) == "<b>")
                {
                    i += 2;
                    bold = !bold;
                    stylechanged = true;
                }
                else if (message.SafeSubstring(i, 3) == "<u>")
                {
                    i += 2;
                    underline = !underline;
                    stylechanged = true;
                }
                else if (message.SafeSubstring(i, 3) == "<i>")
                {
                    i += 2;
                    italic = !italic;
                    stylechanged = true;
                }
                else if (message.SafeSubstring(i, 5) == "<web>")
                    i += 4;
                else if (message.SafeSubstring(i, 6) == "</web>")
                    i += 5;
                else if (message.SafeSubstring(i, 5) == "<irc>")
                {
                    int j = message.IndexOf("</irc>", i + 5);
                    if (j == -1)
                        return output.ToString();
                    i = j + 5;
                }
                else if (message[i] == '\n')
                    output.Append("<br />");
                else
                {
                    if (stylechanged)
                        output.Append(generate_html_tag(bold, underline, italic, forecolor, backcolor, ref plaintext));
                    stylechanged = false;
                    output.Append(HttpUtility.HtmlEncode(message[i]));
                }
            }
            if (!plaintext)
                output.Append("</span>");
            return output.ToString();
        }

        private string ThemesPage(NameValueCollection options, Dictionary<string, string> cookies)
        {
            StringBuilder pageContent = new StringBuilder();
            string cookietheme = cookies.ContainsKey("theme") ? cookies["theme"] : null;
            string cookietroll = cookies.ContainsKey("troll") ? cookies["troll"] : null;
            try
            {
                pageContent.AppendLine("<h1><strong>Theme Switcher</strong></h1>");
                pageContent.AppendLine("<h2>Themes</h2>");
                foreach (KeyValuePair<string, string> item in themes)
                {
                    pageContent.Append("<p class=\"left\">");
                    if (!item.Key.Equals(cookietheme, StringComparison.OrdinalIgnoreCase))
                        pageContent.Append("<a href=\"settheme?theme=" + item.Key + "\">");
                    pageContent.Append(item.Value);
                    if (!item.Key.Equals(cookietheme, StringComparison.OrdinalIgnoreCase))
                        pageContent.Append("</a>");
                    pageContent.Append(" (");
                    if (!theme.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                        pageContent.Append("<a href=\"?page=" + getpage + "&amp;theme=" + item.Key + "\">");
                    pageContent.Append("Preview");
                    if (!theme.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                        pageContent.Append("</a>");
                    pageContent.AppendLine(")</p>");
                }
                pageContent.AppendLine("<h2>Trolls</h2>");
                foreach (KeyValuePair<string, string> item in trolls)
                {
                    pageContent.Append("<p class=\"left\">");
                    if (!item.Key.Equals(cookietroll, StringComparison.OrdinalIgnoreCase))
                        pageContent.Append("<a href=\"settheme?troll=" + item.Key + "\">");
                    pageContent.Append(item.Value);
                    if (!item.Key.Equals(cookietroll, StringComparison.OrdinalIgnoreCase))
                        pageContent.Append("</a>");
                    pageContent.Append(" (");
                    if (!troll.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                        pageContent.Append("<a href=\"?page=" + getpage + "&amp;troll=" + item.Key + "\">");
                    pageContent.Append("Preview");
                    if (!troll.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                        pageContent.Append("</a>");
                    pageContent.AppendLine(")</p>");
                }
            }
            catch (Exception ex)
            {
                pageContent.AppendLine("<pre>" + HttpUtility.HtmlEncode(ex.ToString()) + "</pre>");
            }
            return pageContent.ToString();
        }
    }
}
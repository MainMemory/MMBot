using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;
using System.Drawing;
using System.Collections.ObjectModel;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using IniFile;

namespace MMBot
{
    public static class Module1
    {
        public static string OpName = "yournick";
        internal static string password = "yourpass";
        internal static bool enabled = true;
        public static List<string> BanList = new List<string>();
        public static List<string> IgnoreList = new List<string>();
        public const char CTCPChar = '\u0001';
        public const char BoldChar = '\u0002';
        public const char ColorChar = '\u0003';
        public const char StopChar = '\u000F';
        public const char RevChar = '\u0016';
        public const char ItalicChar = '\u001D';
        public const char UnderChar = '\u001F';
        static public cIRC IrcApp;
        static internal bool quitting;
        static internal readonly System.Timers.Timer remindertimer = new System.Timers.Timer(1000)
        {
            Enabled = true,
            AutoReset = true
        };
        static internal readonly System.Timers.Timer savetimer = new System.Timers.Timer(5 * 60 * 1000)
        {
            Enabled = true,
            AutoReset = true
        };
        static internal List<Reminder> reminders = new List<Reminder>();
        static internal bool Debug;
        static internal readonly bool isMonoRuntime = Type.GetType("Mono.Runtime") != null;
        static internal string translate = "off";
        static internal readonly string[] translatetypes = { "off", "allcaps", "invcaps", "nocaps", "altcaps", "rainbow", "reverse", "underline", "bold", "invert", "swap", "scramble" };
        static internal readonly string[] trolls = { "hs-aa", "hs-ac", "hs-ag", "hs-at", "hs-ca", "hs-cc", "hs-cg", "hs-ct", "hs-ga", "hs-gc", "hs-ta", "hs-tc", "hs-tc2", "hs-uu", "hs-uu2" };
        static internal readonly System.Timers.Timer feedtimer = new System.Timers.Timer(300000)
        {
            Enabled = true,
            AutoReset = true
        };
        static internal List<ProcessInfo> proclist = new List<ProcessInfo>();
        static internal Form1 myForm;
        static internal Dictionary<string, GUIChanInfo> chanscrollback = new Dictionary<string, GUIChanInfo>();
        static internal string cmdchar = "!";
        static internal List<ServerInfo> servinf = new List<ServerInfo>();
        static internal List<TimerInfo> timers = new List<TimerInfo>();
        static internal HttpServer server = new HttpServer();
        static internal Dictionary<string, string> Vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "ans", "0" } };
        public const string useragent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/5.0)";

        public static void Save()
        {
            GlobalSettings settings = new GlobalSettings();
            if (File.Exists("global.ini")) settings = GlobalSettings.Load("global.ini");
            settings.BanList = Module1.BanList;
            settings.IgnoreList = Module1.IgnoreList;
            settings.Save("global.ini");
            Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();
            Newtonsoft.Json.JsonTextWriter jw;
            foreach (IRC IrcObject in IrcApp.IrcObjects)
            {
                bool[] aliasref = new bool[IrcObject.Aliases.Count];
                foreach (IRCChannel chan in IrcObject.IrcChannels)
                {
                    List<IRCChanUserStats> newlist = new List<IRCChanUserStats>();
                    foreach (IRCChanUserStats item in chan.stats)
                        if ((DateTime.Now - item.lastaction).TotalDays <= 120 || chan.GetUser(item.name) != null)
                            newlist.Add(item);
                    chan.stats = newlist;
                    int i = 0;
                    while (i < chan.stats.Count)
                    {
                        IRCChanUserStats item = chan.stats[i];
                        int j = IrcObject.Aliases.IndexOf(IrcObject.GetAliases(item.name));
                        if (j > -1) aliasref[j] = true;
                        if (!object.ReferenceEquals(chan.GetUserStats(item.name, false), item) & chan.GetUserStats(item.name, false) != null)
                        {
                            chan.GetUserStats(item.name, false).Combine(item);
                            chan.stats.RemoveAt(i);
                        }
                        else
                            i++;
                    }
                }
                List<List<string>> aliases = new List<List<string>>();
                for (int i = 0; i < IrcObject.Aliases.Count; i++)
                    if (aliasref[i])
                        aliases.Add(IrcObject.Aliases[i]);
                IrcObject.Aliases = aliases;
                StreamWriter stw = new StreamWriter(Path.Combine(Environment.CurrentDirectory, IrcObject.name + ".json"));
                jw = new Newtonsoft.Json.JsonTextWriter(stw);
                jw.Formatting = Newtonsoft.Json.Formatting.Indented;
                jw.IndentChar = '\t';
                jw.Indentation = 1;
                js.Serialize(jw, IrcObject);
                jw.Close();
                stw.Close();
            }
            foreach (KeyValuePair<string, BotModule> item in IrcApp.Modules)
                item.Value.Save();
        }

        public static void Quit()
        {
            quitting = true;
            try { server.Stop(); }
            catch { }
            foreach (IRC IrcObject in IrcApp.IrcObjects)
                foreach (IRCChannel chan in IrcObject.IrcChannels)
                {
                    if (chan.GetUserStats(IrcObject.IrcNick, false) != null)
                        chan.GetUserStats(IrcObject.IrcNick, false).quits += 1;
                    foreach (IRCChanUserStats stat in chan.stats)
                    {
                        stat.onlinetimer.Stop();
                        stat.onlinesaved = stat.onlinetime;
                        stat.onlinetimer.Reset();
                        stat.refcount = 0;
                    }
                }
            foreach (KeyValuePair<string, BotModule> module in IrcApp.Modules)
                module.Value.Shutdown();
            Save();
            Environment.Exit(0);
        }

        static string[] operators = {
		"||",
		"&&",
		"==",
		"!=",
		"<=",
		">=",
		">>",
		"<<",
		">",
		"<",
		"&",
		"|",
		"xor",
		"sin",
		"cos",
		"tan",
		"sqrt",
		"ceil",
		"floor",
		"trunc",
		"log",
		"ln",
		"+",
		"-",
		"*",
		"/",
		"\\",
		"%",
		"^",
		"e"
	};
        public static double calc(string expression)
        {
            expression = expression.Replace(" ", "").ToLower();
            //parentheses loop
            while (expression.IndexOf('(') > -1)
            {
                int level = 1;
                int i1 = expression.IndexOf('(');
                int i2 = -1;
                for (int i = i1 + 1; i <= expression.LastIndexOf(')'); i++)
                {
                    if (expression[i] == '(')
                        level += 1;
                    if (expression[i] == ')')
                    {
                        level -= 1;
                        i2 = i;
                    }
                    if (level == 0)
                        break;
                }
                if (i2 == -1)
                {
                    throw new ArgumentException("There are not enough closing parentheses!");
                }
                else
                {
                    expression = expression.Substring(0, i1) + calc(expression.Substring(i1 + 1, i2 - (i1 + 1))) + expression.Substring(i2 + 1);
                }
            }
            List<dynamic> expr = new List<dynamic>();
            string num = string.Empty;
            int c = 0;
            bool found = false;
            //string parsing loop
            while (c < expression.Length)
            {
                foreach (string item in operators)
                {
                    if (expression.SafeSubstring(c, item.Length) == item)
                    {
                        //If (c > 2 And c < (expression.Length - 1)) AndAlso ((item = "+" Or item = "-") And expression(c - 1) = "e"c And Char.IsNumber(expression(c - 2)) And Char.IsNumber(expression(c + 1))) Then Exit For
                        if (item == "-" | item == "+")
                        {
                            if (c == 0)
                                break;
                            if (expr.Count > 0 && Array.IndexOf(operators, expr[expr.Count - 1]) > -1 & num.Length == 0)
                                break;
                        }
                        if (item == "e")
                        {
                            if (!string.IsNullOrEmpty(num) && num.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                                break;
                        }
                        found = true;
                        if (!string.IsNullOrEmpty(num))
                            expr.Add(Str2Num(num));
                        expr.Add(item);
                        num = string.Empty;
                        c += item.Length;
                        break;
                    }
                }
                if (!found)
                {
                    num += expression[c];
                    c += 1;
                }
                found = false;
            }
            if (num != string.Empty)
                expr.Add(Str2Num(num));
            try
            {
                //calculation loop
                while (expr.Count > 1)
                {
                    int i = 0;
                    i = expr.FirstIndexOf("sin", "cos", "tan", "sqrt", "ceil", "floor", "trunc", "log", "ln");
                    if (i > -1)
                    {
                        switch ((string)expr[i])
                        {
                            case "sin":
                                expr[i] = Math.Sin(Convert.ToDouble(expr[i + 1]));
                                break;
                            case "cos":
                                expr[i] = Math.Cos(Convert.ToDouble(expr[i + 1]));
                                break;
                            case "tan":
                                expr[i] = Math.Tan(Convert.ToDouble(expr[i + 1]));
                                break;
                            case "sqrt":
                                expr[i] = Math.Sqrt(Convert.ToDouble(expr[i + 1]));
                                break;
                            case "ceil":
                                expr[i] = Math.Ceiling(Convert.ToDouble(expr[i + 1]));
                                break;
                            case "floor":
                                expr[i] = Math.Floor(Convert.ToDouble(expr[i + 1]));
                                break;
                            case "trunc":
                                expr[i] = Math.Truncate(Convert.ToDouble(expr[i + 1]));
                                break;
                            case "log":
                                expr[i] = Math.Log10(Convert.ToDouble(expr[i + 1]));
                                break;
                            case "ln":
                                expr[i] = Math.Log(Convert.ToDouble(expr[i + 1]));
                                break;
                        }
                        expr.RemoveAt(i + 1);
                        continue;
                    }
                    i = expr.IndexOf("^");
                    if (i > -1)
                    {
                        expr[i - 1] = Math.Pow(Convert.ToDouble(expr[i - 1]), Convert.ToDouble(expr[i + 1]));
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.IndexOf("e");
                    if (i > -1)
                    {
                        if (expr[i + 1] == "+" | expr[i + 1] == "-")
                        {
                            expr[i - 1] += expr[i] + expr[i + 1] + expr[i + 2];
                            expr.RemoveAt(i);
                        }
                        else
                        {
                            expr[i - 1] += expr[i] + expr[i + 1];
                        }
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.FirstIndexOf("*", "/", "\\", "%");
                    if (i > -1)
                    {
                        switch ((string)expr[i])
                        {
                            case "*":
                                expr[i - 1] = (double.Parse(expr[i - 1]) * double.Parse(expr[i + 1])).ToLongString();
                                break;
                            case "/":
                                expr[i - 1] = (double.Parse(expr[i - 1]) / double.Parse(expr[i + 1])).ToLongString();
                                break;
                            case "\\":
                                expr[i - 1] = (long.Parse(expr[i - 1]) / long.Parse(expr[i + 1])).ToString();
                                break;
                            case "%":
                                expr[i - 1] = (double.Parse(expr[i - 1]) % double.Parse(expr[i + 1])).ToLongString();
                                break;
                        }
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.FirstIndexOf("+", "-");
                    if (i > -1)
                    {
                        switch ((string)expr[i])
                        {
                            case "+":
                                expr[i - 1] = (double.Parse(expr[i - 1]) + double.Parse(expr[i + 1])).ToLongString();
                                break;
                            case "-":
                                expr[i - 1] = (double.Parse(expr[i - 1]) - double.Parse(expr[i + 1])).ToLongString();
                                break;
                        }
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.FirstIndexOf(">>", "<<");
                    if (i > -1)
                    {
                        switch ((string)expr[i])
                        {
                            case ">>":
                                expr[i - 1] = (long.Parse(expr[i - 1]) >> int.Parse(expr[i + 1])).ToString();
                                break;
                            case "<<":
                                expr[i - 1] = (long.Parse(expr[i - 1]) << int.Parse(expr[i + 1])).ToString();
                                break;
                        }
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.FirstIndexOf(">=", "<=", ">", "<");
                    if (i > -1)
                    {
                        switch ((string)expr[i])
                        {
                            case ">=":
                                expr[i - 1] = double.Parse(expr[i - 1]) >= double.Parse(expr[i + 1]) ? "1" : "0";
                                break;
                            case "<=":
                                expr[i - 1] = double.Parse(expr[i - 1]) <= double.Parse(expr[i + 1]) ? "1" : "0";
                                break;
                            case ">":
                                expr[i - 1] = double.Parse(expr[i - 1]) > double.Parse(expr[i + 1]) ? "1" : "0";
                                break;
                            case "<":
                                expr[i - 1] = double.Parse(expr[i - 1]) < double.Parse(expr[i + 1]) ? "1" : "0";
                                break;
                        }
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.FirstIndexOf("==", "!=");
                    if (i > -1)
                    {
                        switch ((string)expr[i])
                        {
                            case "==":
                                expr[i - 1] = double.Parse(expr[i - 1]) == double.Parse(expr[i + 1]) ? "1" : "0";
                                break;
                            case "!=":
                                expr[i - 1] = double.Parse(expr[i - 1]) != double.Parse(expr[i + 1]) ? "1" : "0";
                                break;
                        }
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.IndexOf("&");
                    if (i > -1)
                    {
                        expr[i - 1] = (long.Parse(expr[i - 1]) & long.Parse(expr[i + 1])).ToString();
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.IndexOf("xor");
                    if (i > -1)
                    {
                        expr[i - 1] = (long.Parse(expr[i - 1]) ^ long.Parse(expr[i + 1])).ToString();
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.IndexOf("|");
                    if (i > -1)
                    {
                        expr[i - 1] = (long.Parse(expr[i - 1]) | long.Parse(expr[i + 1])).ToString();
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.IndexOf("&&");
                    if (i > -1)
                    {
                        expr[i - 1] = Convert.ToBoolean(double.Parse(expr[i - 1])) & Convert.ToBoolean(double.Parse(expr[i + 1])) ? "1" : "0";
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.IndexOf("||");
                    if (i > -1)
                    {
                        expr[i - 1] = Convert.ToBoolean(double.Parse(expr[i - 1])) | Convert.ToBoolean(double.Parse(expr[i + 1])) ? "1" : "0";
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    string message = "No identifiable operators remaining! Expression:";
                    foreach (string item in expr)
                    {
                        message += " " + item;
                    }
                    throw new ArgumentException(message);
                }
                return double.Parse(expr[0]);
            }
            catch (InvalidCastException ex)
            {
                string message = ex.Message + " Expression:";
                foreach (string item in expr)
                {
                    message += " " + item;
                }
                throw new ArgumentException(message, ex);
            }
        }

        public static dynamic Str2Num(string str)
        {
            if (str.StartsWith("0x") | str.StartsWith("&H")) return long.Parse(str.Substring(2), System.Globalization.NumberStyles.HexNumber);
            if (str.StartsWith("0b") | str.StartsWith("&B")) return long.Parse(BaseConv(str.Substring(2), 2, 10));
            if (str.Equals("infinty", StringComparison.CurrentCultureIgnoreCase))
                return double.PositiveInfinity;
            if (str.Equals("-infinty", StringComparison.CurrentCultureIgnoreCase))
                return double.NegativeInfinity;
            if (str.Equals("nan", StringComparison.CurrentCultureIgnoreCase))
                return double.NaN;
            return double.Parse(str);
        }

        public static int FirstIndexOf<T>(this List<T> a, params T[] b)
        {
            int functionReturnValue = 0;
            functionReturnValue = a.IndexOf(b[0]);
            int n = 0;
            for (int i = 1; i <= b.Length - 1; i++)
            {
                if (functionReturnValue == -1)
                {
                    functionReturnValue = a.IndexOf(b[i]);
                }
                else
                {
                    n = a.IndexOf(b[i]);
                    if (n > -1 & n < functionReturnValue)
                        functionReturnValue = n;
                }
            }
            return functionReturnValue;
        }

        static string[] timeoperators = {
		    "==",
		    "!=",
		    "<=",
		    ">=",
            "<",
            ">",
		    "+",
		    "-",
	    };

        public static object timecalc(string expression)
        {
            //parentheses loop
            while (expression.IndexOf('(') > -1)
            {
                int level = 1;
                int i1 = expression.IndexOf('(');
                int i2 = -1;
                for (int i = i1 + 1; i <= expression.LastIndexOf(')'); i++)
                {
                    if (expression[i] == '(')
                        level += 1;
                    if (expression[i] == ')')
                    {
                        level -= 1;
                        i2 = i;
                    }
                    if (level == 0)
                        break;
                }
                if (i2 == -1)
                    throw new ArgumentException("There are not enough closing parentheses!");
                else
                    expression = expression.Substring(0, i1) + timecalc(expression.Substring(i1 + 1, i2 - (i1 + 1))) + expression.Substring(i2 + 1);
            }
            List<dynamic> expr = new List<dynamic>();
            string num = string.Empty;
            int c = 0;
            bool found = false;
            //string parsing loop
            while (c < expression.Length)
            {
                foreach (string item in timeoperators)
                {
                    if (expression.SafeSubstring(c, item.Length) == item)
                    {
                        if (item == "-" | item == "+")
                        {
                            if (c == 0)
                                break;
                            if (expr.Count > 0 && Array.IndexOf(timeoperators, expr[expr.Count - 1]) > -1 & num.Length == 0)
                                break;
                        }
                        found = true;
                        if (!string.IsNullOrEmpty(num))
                            expr.Add(num.Trim());
                        expr.Add(item);
                        num = string.Empty;
                        c += item.Length;
                        break;
                    }
                }
                if (!found)
                {
                    num += expression[c];
                    c++;
                }
                found = false;
            }
            if (num != string.Empty)
                expr.Add(num.Trim());
            for (int t = 0; t < expr.Count; t++)
            {
                if (timeoperators.Contains((object)expr[t])) continue;
                DateTime? d = GetDate(expr[t]);
                if (d.HasValue)
                    expr[t] = d.Value;
                else
                {
                    TimeSpan? ts = GetTimeSpan(expr[t]);
                    if (ts.HasValue)
                        expr[t] = ts.Value;
                    else
                        throw new FormatException("The string \"" + expr[t] + "\" could not be parsed as a DateTime or TimeSpan.");
                }
            }
            try
            {
                //calculation loop
                while (expr.Count > 1)
                {
                    int i = 0;
                    i = expr.FirstIndexOf("+", "-");
                    if (i > -1)
                    {
                        switch ((string)expr[i])
                        {
                            case "+":
                                expr[i - 1] = expr[i - 1] + expr[i + 1];
                                break;
                            case "-":
                                expr[i - 1] = expr[i - 1] - expr[i + 1];
                                break;
                        }
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.FirstIndexOf(">=", "<=", ">", "<");
                    if (i > -1)
                    {
                        switch ((string)expr[i])
                        {
                            case ">=":
                                expr[i - 1] = expr[i - 1] >= expr[i + 1];
                                break;
                            case "<=":
                                expr[i - 1] = expr[i - 1] <= expr[i + 1];
                                break;
                            case ">":
                                expr[i - 1] = expr[i - 1] > expr[i + 1];
                                break;
                            case "<":
                                expr[i - 1] = expr[i - 1] < expr[i + 1];
                                break;
                        }
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    i = expr.FirstIndexOf("==", "!=");
                    if (i > -1)
                    {
                        switch ((string)expr[i])
                        {
                            case "==":
                                expr[i - 1] = expr[i - 1] == expr[i + 1];
                                break;
                            case "!=":
                                expr[i - 1] = expr[i - 1] != expr[i + 1];
                                break;
                        }
                        expr.RemoveAt(i);
                        expr.RemoveAt(i);
                        continue;
                    }
                    string message = "No identifiable operators remaining! Expression:";
                    foreach (string item in expr)
                    {
                        message += " " + item;
                    }
                    throw new ArgumentException(message);
                }
                return expr[0];
            }
            catch (InvalidCastException ex)
            {
                string message = ex.Message + " Expression:";
                foreach (object item in expr)
                {
                    message += " " + item.ToString();
                }
                throw new ArgumentException(message, ex);
            }
        }

        public static readonly DateTime UnixEpoch = DateTime.Parse("January 1, 1970", System.Globalization.DateTimeFormatInfo.InvariantInfo);

        public static readonly DateTime UnixMax = UnixEpoch.AddSeconds(int.MaxValue);

        public static readonly Dictionary<string, DateTime> customdates = new Dictionary<string, DateTime>() {
            { "christmas", DateTime.Parse("Dec 25", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "john egbert's birthday", DateTime.Parse("April 13, 1996", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "rose lalonde's birthday", DateTime.Parse("December 4, 1995", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "dave strider's birthday", DateTime.Parse("December 3, 1995", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "jade harley's birthday", DateTime.Parse("December 1, 1995", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "problem sleuth", DateTime.Parse("March 10, 2008", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of problem sleuth", DateTime.Parse("April 7, 2009", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck beta", DateTime.Parse("April 10, 2009", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck beta", DateTime.Parse("April 12, 2009", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck", DateTime.Parse("April 13, 2009", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 1", DateTime.Parse("April 13, 2009", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 1", DateTime.Parse("June 7, 2009", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 2", DateTime.Parse("June 10, 2009", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 2", DateTime.Parse("October 11, 2009", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 3", DateTime.Parse("October 14, 2009", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 3", DateTime.Parse("January 14, 2010", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck intermission", DateTime.Parse("January 15, 2010", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck intermission", DateTime.Parse("February 9, 2010", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 4", DateTime.Parse("February 9, 2010", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 4", DateTime.Parse("June 5, 2010", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 5", DateTime.Parse("June 12, 2010", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 5 act 1", DateTime.Parse("June 12, 2010", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 5 act 1", DateTime.Parse("September 19, 2010", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 5 act 2", DateTime.Parse("September 19, 2010", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 5 act 2", DateTime.Parse("October 25, 2011", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 5", DateTime.Parse("October 25, 2011", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck intermission 2", DateTime.Parse("October 31, 2011", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck intermission 2", DateTime.Parse("November 2, 2011", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 6", DateTime.Parse("November 11, 2011", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 6 act 1", DateTime.Parse("November 11, 2011", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 6 act 1", DateTime.Parse("December 8, 2011", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 6 intermission 1", DateTime.Parse("December 10, 2011", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 6 intermission 1", DateTime.Parse("December 29, 2011", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 6 act 2", DateTime.Parse("January 1, 2012", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 6 act 2", DateTime.Parse("March 8, 2012", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 6 intermission 2", DateTime.Parse("March 9, 2012", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "end of homestuck act 6 intermission 2", DateTime.Parse("April 2, 2012", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "homestuck act 6 act 3", DateTime.Parse("April 13, 2012", System.Globalization.DateTimeFormatInfo.InvariantInfo) },
            { "unix epoch", UnixEpoch },
            { "unix max", UnixMax }
        };

        public static DateTime? GetDate(string str)
        {
            if (customdates.ContainsKey(str.ToLowerInvariant()))
                return customdates[str.ToLowerInvariant()];
            if (str.Equals("yesterday", StringComparison.OrdinalIgnoreCase))
                return DateTime.UtcNow.Date.AddDays(-1);
            if (str.Equals("today", StringComparison.OrdinalIgnoreCase))
                return DateTime.UtcNow.Date;
            if (str.Equals("now", StringComparison.OrdinalIgnoreCase))
                return DateTime.UtcNow;
            if (str.Equals("localtime", StringComparison.OrdinalIgnoreCase))
                return DateTime.Now;
            if (str.Equals("tomorrow", StringComparison.OrdinalIgnoreCase))
                return DateTime.UtcNow.Date.AddDays(1);
            DateTime result = DateTime.MinValue;
            if (DateTime.TryParse(str, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.AdjustToUniversal, out result))
                return result;
            return null;
        }

        public static char[] numberchars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };

        public static TimeSpan? GetTimeSpan(string str)
        {
            TimeSpan result = TimeSpan.Zero;
            if (TimeSpan.TryParse(str, out result))
                return result;
            str = str.Replace(",", "").Replace(" ", "");
            string num = string.Empty;
            string type = string.Empty;
            List<Tuple<string, string>> parts = new List<Tuple<string, string>>();
            bool negative = false;
            if (str.StartsWith("-"))
            {
                negative = true;
                str = str.Substring(1);
            }
            foreach (char item in str)
            {
                if (Array.IndexOf(numberchars, item) > -1)
                {
                    if (!string.IsNullOrEmpty(type))
                    {
                        if (string.IsNullOrEmpty(num)) return null;
                        parts.Add(new Tuple<string, string>(num, type));
                        num = string.Empty;
                        type = string.Empty;
                    }
                    num += item;
                }
                else
                    type += item;
            }
            if (string.IsNullOrEmpty(num)) return null;
            parts.Add(new Tuple<string, string>(num, type));
            result = TimeSpan.Zero;
            foreach (Tuple<string, string> item in parts)
            {
                double number = double.Parse(item.Item1, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo);
                switch (item.Item2.ToLowerInvariant())
                {
                    case "sweep":
                    case "sweeps":
                        result += TimeSpan.FromDays(790.833333333333 * number);
                        break;
                    case "y":
                    case "yr":
                    case "year":
                    case "years":
                        result += TimeSpan.FromDays(365 * number);
                        break;
                    case "w":
                    case "wk":
                    case "week":
                    case "weeks":
                        result += TimeSpan.FromDays(7 * number);
                        break;
                    case "d":
                    case "day":
                    case "days":
                        result += TimeSpan.FromDays(number);
                        break;
                    case "h":
                    case "hr":
                    case "hour":
                    case "hours":
                        result += TimeSpan.FromHours(number);
                        break;
                    case "m":
                    case "min":
                    case "minute":
                    case "minutes":
                        result += TimeSpan.FromMinutes(number);
                        break;
                    case "":
                    case "s":
                    case "sec":
                    case "second":
                    case "seconds":
                        result += TimeSpan.FromSeconds(number);
                        break;
                    case "cs":
                    case "centisecond":
                    case "centiseconds":
                        result += TimeSpan.FromMilliseconds(number * 10);
                        break;
                    case "f":
                    case "frame":
                    case "frames":
                    case "ntsc":
                    case "ntscframe":
                    case "ntscframes":
                        result += TimeSpan.FromTicks((long)(number * (TimeSpan.TicksPerSecond / 60.0)));
                        break;
                    case "pal":
                    case "palframe":
                    case "palframes":
                        result += TimeSpan.FromTicks((long)(number * (TimeSpan.TicksPerSecond / 50.0)));
                        break;
                    case "ms":
                    case "millisecond":
                    case "milliseconds":
                        result += TimeSpan.FromMilliseconds(number);
                        break;
                    case "tick":
                    case "ticks":
                        result += TimeSpan.FromTicks((long)number);
                        break;
                    default:
                        return null;
                }
            }
            if (negative) result = -result;
            return result;
        }

        public static bool Contains<T>(this T[] array, T item)
        {
            foreach (T it in array)
                if (it.Equals(item))
                    return true;
            return false;
        }

        static internal string[] ballresps = {
		"As I see it, yes",
		"It is certain",
		"It is decidedly so",
		"Most likely",
		"Outlook good",
		"Signs point to yes",
		"Without a doubt",
		"Yes",
		"Yes - definitely",
		"You may rely on it",
		"Reply hazy, try again",
		"Ask again later",
		"Better not tell you now",
		"Cannot predict now",
		"Concentrate and ask again",
		"Don't count on it",
		"My reply is no",
		"My sources say no",
		"Outlook not so good",
		"Very doubtful"

	};

        static string[] sadxlvls = {
		"Hedgehog Hammer",
		"Emerald Coast",
		"Windy Valley",
		"Twinkle Park",
		"Speed Highway",
		"Red Mountain",
		"Sky Deck",
		"Lost World",
		"Ice Cap",
		"Casinopolis",
		"Final Egg",
		"???",
		"Hot Shelter",
		"???",
		"???",
		"Chaos 0",
		"Chaos 2",
		"Chaos 4",
		"Chaos 6",
		"Perfect Chaos",
		"Egg Hornet",
		"Egg Walker",
		"Egg Viper",
		"ZERO",
		"E-101 Beta",
		"E-101mkII",
		"Station Square",
		"???",
		"???",
		"Egg Carrier Outside",
		"???",
		"???",
		"Egg Carrier Inside",
		"Mystic Ruins",
		"The Past",
		"Twinkle Circuit",
		"Sky Chase Act 1",
		"Sky Chase Act 2",
		"Sand Hill",
		"Chao Garden",
		"Chao Garden",
		"Chao Garden",
		"Chao Garden"
	};
        static string[] sadxchars = { "Sonic", "Eggman", "Tails", "Knuckles", "Tikal", "Amy", "Gamma", "Big" };
        static string[][] skclvls = {
		new string[] { "Angel Island Zone Act 1", "Angel Island Zone Act 2" },
		new string[] { "Hydrocity Zone Act 1", "Hydrocity Zone Act 2" },
		new string[] { "Marble Garden Zone Act 1", "Marble Garden Zone Act 2" },
		new string[] { "Carnival Night Zone Act 1", "Carnival Night Zone Act 2" },
		new string[] { "Flying Battery Zone Act 1", "Flying Battery Zone Act 2" },
		new string[] { "Icecap Zone Act 1", "Icecap Zone Act 2" },
		new string[] { "Launch Base Zone Act 1", "Launch Base Zone Act 2" },
		new string[] { "Mushroom Hill Zone Act 1", "Mushroom Hill Zone Act 2" },
		new string[] { "Sandopolis Zone Act 1", "Sandopolis Zone Act 2" },
		new string[] { "Lava Reef Zone Act 1", "Lava Reef Zone Act 2" },
		new string[] { "Sky Sanctuary Zone", "Sky Sanctuary Zone" },
		new string[] { "Death Egg Zone Act 1", "Death Egg Zone Act 2" },
		new string[] { "The Doomsday Zone", "???" },
		new string[] { "???", "Ending" },
		new string[] { "Azure Lake", "???" },
		new string[] { "Balloon Park", "???" },
		new string[] { "Desert Palace", "???" },
		new string[] { "Chrome Gadget", "???" },
		new string[] { "Endless Mine", "???" },
		new string[] { "Gumball Bonus", "???" },
		new string[] { "Glowing Spheres Bonus", "???" },
		new string[] { "Slot Machine Bonus", "???" },
		new string[] { "Lava Reef Zone Boss", "Hidden Palace Zone" },
		new string[] { "Death Egg Zone Final Boss", "Hidden Palace Shrine" }
	    };
        static string[] skcchars = { "Sonic & Tails", "Sonic", "Tails", "Knuckles" };
        static string[] skcgames = { "Sonic 3 & Knuckles", "Sonic & Knuckles", "Sonic 3" };
        static string[] shlvls = {
                                     "Unknown", // 0
                                     "Unknown", // 1
                                     "Seaside Hill", // 2
                                     "Ocean Palace", // 3
                                     "Grand Metropolis", // 4
                                     "Power Plant", // 5
                                     "Casino Park", // 6
                                     "BINGO Highway", // 7
                                     "Rail Canyon", // 8
                                     "Bullet Station", // 9
                                     "Frog Forest", // 10
                                     "Lost Jungle", // 11
                                     "Hang Castle", // 12
                                     "Mystic Mansion", // 13
                                     "Egg Fleet", // 14
                                     "Final Fortress", // 15
                                     "Egg Hawk", // 16
                                     "Team Battle 1", // 17
                                     "Robot Carnival", // 18
                                     "Egg Albatross", // 19
                                     "Team Battle 2", // 20
                                     "Robot Storm", // 21
                                     "Egg Emperor", // 22
                                     "Metal Madness", // 23
                                     "Metal Overlord", // 24
                                     "Sea Gate", // 25
                                     "Bobsled Race: Seaside Course", // 26
                                     "Bobsled Race: City Course", // 27
                                     "Bobsled Race: Casino Course", // 28
                                     "Special Stage Act 1 Bonus Challenge", // 29
                                     "Special Stage Act 2 Bonus Challenge", // 30
                                     "Special Stage Act 3 Bonus Challenge", // 31
                                     "Special Stage Act 4 Bonus Challenge", // 32
                                     "Special Stage Act 5 Bonus Challenge", // 33
                                     "Special Stage Act 6 Bonus Challenge", // 34
                                     "Special Stage Act 7 Bonus Challenge", // 35
                                     "Rail Canyon", // 36
                                     "Action Race: Seaside Hill", // 37
                                     "Action Race: Grand Metropolis", // 38
                                     "Action Race: BINGO Highway", // 39
                                     "Battle: City Top", // 40
                                     "Battle: Casino Ring", // 41
                                     "Battle: Turtle Shell", // 42
                                     "Ring Race: Egg Treat", // 43
                                     "Ring Race: Pinball Match", // 44
                                     "Ring Race: Hot Elevator", // 45
                                     "Quick Race: Road Rock", // 46
                                     "Quick Race: Mad Express", // 47
                                     "Quick Race: Terror Hall", // 48
                                     "Advanced Race: Rail Canyon", // 49
                                     "Advanced Race: Frog Forest", // 50
                                     "Advanced Race: Egg Fleet", // 51
                                     "Special Stage Act 1 Emerald Challenge", // 52
                                     "Special Stage Act 2 Emerald Challenge", // 53
                                     "Special Stage Act 3 Emerald Challenge", // 54
                                     "Special Stage Act 4 Emerald Challenge", // 55
                                     "Special Stage Act 5 Emerald Challenge", // 56
                                     "Special Stage Act 6 Emerald Challenge", // 57
                                     "Special Stage Act 7 Emerald Challenge", // 58
                                     "2P Special Stage 1", // 59
                                     "2P Special Stage 2", // 60
                                     "2P Special Stage 3" // 61
                                 };
        static string[] shchars = { "Team Sonic", "Team Dark", "Team Rose", "Team Chaotix" };
        static internal string Game()
        {
            string functionReturnValue = "Now playing: ";
            if (Process.GetProcessesByName("sonic").Length > 0)
            {
                Process proc = Process.GetProcessesByName("sonic")[0];
                functionReturnValue += "Sonic Adventure DX: ";
                functionReturnValue += sadxlvls[proc.ReadByte(0x3b22dcc)];
                functionReturnValue += " part ";
                functionReturnValue += proc.ReadByte(0x3b22dec) + 1;
                functionReturnValue += " as ";
                if (proc.ReadBoolean(0x3b18db5))
                    functionReturnValue += "Metal Sonic";
                else
                    functionReturnValue += sadxchars[proc.ReadByte(0x3b22dc0)];
                functionReturnValue += Environment.NewLine;
                functionReturnValue += "Time: " + proc.ReadByte(0x3b0ef48) + ":" + proc.ReadByte(0x3b0f128).ToString("00");
                functionReturnValue += " Rings: " + proc.ReadInt16(0x3b0f0e4);
                functionReturnValue += " Lives: " + proc.ReadSByte(0x3b0ef34);
                System.Collections.BitArray emblems = new System.Collections.BitArray(proc.ReadBytes(0x3B2B5E8, 0x11));
                int emblemcnt = 0;
                for (int i = 0; i < 131; i++)
                    if (emblems[i])
                        emblemcnt++;
                functionReturnValue += " Emblems: " + emblemcnt;
                return functionReturnValue;
            }
            if (Process.GetProcessesByName("sorr").Length > 0)
            {
                Process proc = Process.GetProcessesByName("sorr")[0];
                functionReturnValue += proc.MainWindowTitle;
                return functionReturnValue;
            }
            if (Process.GetProcessesByName("sonicr").Length > 0)
            {
                Process proc = Process.GetProcessesByName("sonicr")[0];
                functionReturnValue += "Sonic R";
                return functionReturnValue;
            }
            if (Process.GetProcessesByName("gens").Length > 0)
            {
                Process proc = Process.GetProcessesByName("gens")[0];
                functionReturnValue += proc.MainWindowTitle;
                return functionReturnValue;
            }
            if (Process.GetProcessesByName("sonic3k").Length > 0)
            {
                Process proc = Process.GetProcessesByName("sonic3k")[0];
                functionReturnValue += "Sonic & Knuckles Collection: ";
                functionReturnValue += skcgames[proc.ReadByte(0x831188)] + ": ";
                functionReturnValue += skclvls[proc.ReadByte(0x8ffEE4F)][proc.ReadByte(0x8ffEE4E)];
                functionReturnValue += " As " + skcchars[proc.ReadByte(0x8ffff08)];
                functionReturnValue += Environment.NewLine;
                functionReturnValue += "Score: " + proc.ReadUInt32(0x8fffe26) + "0";
                functionReturnValue += " Time: " + proc.ReadByte(0x8fffe23) + ":" + proc.ReadByte(0x8fffe24).ToString("00");
                functionReturnValue += " Rings: " + proc.ReadUInt16(0x8fffe20);
                functionReturnValue += " Lives: " + proc.ReadByte(0x8fffe12);
                functionReturnValue += " Emeralds: " + (proc.ReadByte(0x8ffffb0) + proc.ReadByte(0x8ffffb1));
                return functionReturnValue;
            }
            if (Process.GetProcessesByName("Tsonic_win").Length > 0)
            {
                Process proc = Process.GetProcessesByName("Tsonic_win")[0];
                functionReturnValue += "Sonic Heroes: ";
                functionReturnValue += shlvls[proc.ReadByte(0x8D6710)];
                functionReturnValue += " As " + shchars[proc.ReadByte(0x8D6920)];
                functionReturnValue += Environment.NewLine;
                functionReturnValue += "Score: " + (proc.ReadUInt32(0x9DD6C0) + proc.ReadUInt32(0x9DD6C4) + proc.ReadUInt32(0x9DD6C8));
                functionReturnValue += " Time: " + proc.ReadByte(0x9DD70A) + ":" + proc.ReadByte(0x9DD709).ToString("00");
                functionReturnValue += " Rings: " + proc.ReadInt16(0x9DD70C);
                functionReturnValue += " Lives: " + proc.ReadSByte(0x9DD74C);
                return functionReturnValue;
            }
            return functionReturnValue + "Nothing";
        }

        public static string smartsize(ulong bytes)
        {
            double sizek = bytes / 1024;
            double sizem = sizek / 1024;
            double sizeg = sizem / 1024;
            if (sizek > 0.8)
            {
                if (sizem > 0.8)
                {
                    if (sizeg > 0.8)
                    {
                        return Math.Round(sizeg, 2) + "GB";
                    }
                    else
                    {
                        return Math.Round(sizem, 2) + "MB";
                    }
                }
                else
                {
                    return Math.Round(sizek, 2) + "KB";
                }
            }
            else
            {
                return bytes + "B";
            }
        }

        internal enum ByteMultiples : ulong
        {
            B = 1,
            Bytes = B,
            KB = 1024,
            KByte = KB,
            Kilobytes = KB,
            MB = KB * 1024,
            MByte = MB,
            Megabytes = MB,
            GB = MB * 1024,
            GByte = GB,
            Gigabytes = GB,
            TB = GB * 1024,
            TByte = TB,
            Terabytes = TB,
            PB = TB * 1024,
            PByte = PB,
            Petabytes = PB,
            EB = PB * 1024,
            EByte = EB,
            Exabytes = EB,
            Bit = 0,
            KBit = 128,
            Kilobits = KBit,
            MBit = KBit * 1024,
            Megabits = MBit,
            GBit = MBit * 1024,
            Gigabits = GBit,
            TBit = GBit * 1024,
            Terabits = TBit,
            PBit = TBit * 1024,
            Petabits = PBit,
            EBit = PBit * 1024,
            Exabits = EBit,
        }

        internal static double ConvertSizeBytes(string size)
        {
            int numlen = size.Length;
            if (numlen == 0) throw new ArgumentException("Argument must contain a valid number.");
            double val = 0;
            while (!double.TryParse(size.Substring(0, numlen), out val))
            {
                numlen--;
                if (numlen == 0) throw new ArgumentException("Argument must contain a valid number.");
            }
            ByteMultiples enmult = ByteMultiples.B;
            if (!ByteMultiples.TryParse(size.Substring(numlen, size.Length - numlen), true, out enmult))
                throw new ArgumentException("Argument must contain a valid byte size.");
            double mult = (double)enmult;
            if (enmult == ByteMultiples.Bit)
                mult = 0.125;
            return val * mult;
        }

        internal static string ConvertBytesSize(double bytes, ByteMultiples size)
        {
            double mult = (double)size;
            if (size == ByteMultiples.Bit)
                mult = 0.125;
            return (bytes / mult).ToLongString() + size.ToString();
        }

        public static string BytesToString(byte[] bytes)
        {
            string functionReturnValue = string.Empty;
            for (int i = 0; i <= bytes.Length - 2; i++)
            {
                functionReturnValue += bytes[i].ToString("X").PadLeft(2, '0') + " ";
            }
            return functionReturnValue + bytes[bytes.Length - 1].ToString("X").PadLeft(2, '0');
        }

        static internal string BytesToStringB(byte[] bytes)
        {
            string functionReturnValue = string.Empty;
            for (int i = bytes.Length - 1; i >= 1; i += -1)
            {
                functionReturnValue += bytes[i].ToString("X").PadLeft(2, '0') + " ";
            }
            return functionReturnValue + bytes[0].ToString("X").PadLeft(2, '0');
        }

        static internal byte[] StringToBytes(string bytes)
        {
            string data = string.Empty;
            foreach (char item in bytes)
                if (!char.IsWhiteSpace(item))
                    data += item;
            if (data.Length % 2 == 1)
                data = "0" + data;
            List<byte> result = new List<byte>();
            for (int i = 0; i < data.Length / 2; i++)
                result.Add(byte.Parse(data.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber));
            return result.ToArray();
        }

        static internal string Recombine(string[] input, int start)
        {
            return string.Join(" ", input, start, input.Length - start);
        }

        static internal string Recombine(string[] input, int start, int length)
        {
            return string.Join(" ", input, start, length);
        }

        static internal void WriteOutput(string channel, string message, bool newtab)
        {
            myForm.Invoke(myForm.MessageReceiveDelegate, channel, message, newtab);
        }

        public static string ToStringCust(this TimeSpan a) { return a.ToStringCust(int.MaxValue); }

        public static string ToStringCust(this TimeSpan a, int numelements)
        {
            List<string> result = new List<string>();
            bool negative = false;
            if (a < TimeSpan.Zero)
            {
                a = -a;
                negative = true;
            }
            if (a.Days > 0)
            {
                int days = a.Days % 365;
                int years = a.Days / 365;
                int weeks = days / 7;
                days %= 7;
                if (years > 0)
                    result.Add(years + " year" + (years > 1 ? "s" : ""));
                if (weeks > 0)
                    result.Add(weeks + " week" + (weeks > 1 ? "s" : ""));
                if (days > 0)
                    result.Add(days + " day" + (days > 1 ? "s" : ""));
            }
            if (a.Hours > 0)
                result.Add(a.Hours + " hour" + (a.Hours > 1 ? "s" : ""));
            if (a.Minutes > 0)
                result.Add(a.Minutes + " minute" + (a.Minutes > 1 ? "s" : ""));
            if (a.Seconds > 0)
                result.Add(a.Seconds + " second" + (a.Seconds > 1 ? "s" : ""));
            if (result.Count == 0)
                return "0 seconds";
            if (result.Count > numelements)
                result.RemoveRange(numelements, result.Count - numelements);
            string resultstr = string.Join(", ", result);
            if (negative)
                resultstr = "-" + resultstr;
            return resultstr;
        }

        public static string ToStringCustShort(this TimeSpan a) { return a.ToStringCust(int.MaxValue); }

        public static string ToStringCustShort(this TimeSpan a, int numelements)
        {
            List<string> result = new List<string>();
            bool negative = false;
            if (a < TimeSpan.Zero)
            {
                a = -a;
                negative = true;
            }
            if (a.Days > 0)
            {
                int days = a.Days % 365;
                int years = a.Days / 365;
                int weeks = days / 7;
                days %= 7;
                if (years > 0)
                    result.Add(years + "y");
                if (weeks > 0)
                    result.Add(weeks + "w");
                if (days > 0)
                    result.Add(days + "d");
            }
            if (a.Hours > 0)
                result.Add(a.Hours + "h");
            if (a.Minutes > 0)
                result.Add(a.Minutes + "m");
            if (a.Seconds > 0)
                result.Add(a.Seconds + "s");
            if (result.Count == 0)
                return "0s";
            if (result.Count > numelements)
                result.RemoveRange(numelements, result.Count - numelements);
            string resultstr = string.Join("", result);
            if (negative)
                resultstr = "-" + resultstr;
            return resultstr;
        }

        public static string ToStringCustM(this TimeSpan a)
        {
            List<string> result = new List<string>();
            bool negative = false;
            if (a < TimeSpan.Zero)
            {
                a = -a;
                negative = true;
            }
            if (a.Days > 0)
            {
                int days = a.Days % 365;
                int years = a.Days / 365;
                int weeks = days / 7;
                days %= 7;
                if (years > 0)
                    result.Add(years + " year" + (years > 1 ? "s" : ""));
                if (weeks > 0)
                    result.Add(weeks + " week" + (weeks > 1 ? "s" : ""));
                if (days > 0)
                    result.Add(days + " day" + (days > 1 ? "s" : ""));
            }
            if (a.Hours > 0)
                result.Add(a.Hours + " hour" + (a.Hours > 1 ? "s" : ""));
            if (a.Minutes > 0)
                result.Add(a.Minutes + " minute" + (a.Minutes > 1 ? "s" : ""));
            if (a.Seconds > 0)
                result.Add(a.Seconds + " second" + (a.Seconds > 1 ? "s" : ""));
            if (a.Milliseconds > 0)
                result.Add(a.Milliseconds + " millisecond" + (a.Milliseconds > 1 ? "s" : ""));
            if (result.Count == 0)
                return "0 seconds";
            string resultstr = string.Join(", ", result);
            if (negative)
                resultstr = "-" + resultstr;
            return resultstr;
        }

        public static string[] SplitByString(this string a, string splitter)
        {
            string[] x = { splitter };
            return a.Split(x, StringSplitOptions.None);
        }

        public static string SafeSubstring(this string a, int startIndex)
        {
            return a.Substring(Math.Min(startIndex, a.Length));
        }

        public static string SafeSubstring(this string a, int startIndex, int length)
        {
            return a.Substring(Math.Min(startIndex, a.Length), Math.Min(length, a.Length - startIndex));
        }

        public static T[] GetRange<T>(this T[] a, int index, int length)
        {
            T[] x = new T[length];
            Array.Copy(a, index, x, 0, length);
            return x;
        }

        public static Regex multispace = new Regex(" +");
        public static string TrimExcessSpaces(this string a)
        {
            return multispace.Replace(a, " ");
        }

        internal class Reminder
        {
            public IRC Server;
            public string Name;
            public string Message;

            public System.DateTime Time;
            public Reminder(IRC s, string n, string m, DateTime t)
            {
                Server = s;
                Name = n;
                Message = m;
                Time = t;
            }
        }

        internal static void remindertimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            for (int i = 0; i <= reminders.Count - 1; i++)
            {
                if (DateTime.Now > reminders[i].Time)
                {
                    reminders[i].Server.WriteMessage("Reminder: " + reminders[i].Message, reminders[i].Name);
                    reminders.RemoveAt(i);
                }
            }
        }

        static int gcs = 0;
        static internal void savetimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Save();
            if (gcs == 4)
                GC.Collect();
            else
                GC.Collect(0);
            gcs = (gcs + 1) % 5;
        }

        public static string ToLongString(this double input)
        {
            string str = input.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
            // if string representation was collapsed from scientific notation, just return it: 
            if (!str.Contains("E") & !str.Contains("e"))
                return str;
            str = str.ToUpper();
            char decSeparator = '.';
            string[] exponentParts = str.Split('E');
            string[] decimalParts = exponentParts[0].Split(decSeparator);
            // fix missing decimal point: 
            if (decimalParts.Length == 1)
                decimalParts = new string[] {
				exponentParts[0],
				"0"
			};
            int exponentValue = int.Parse(exponentParts[1]);
            string newNumber = decimalParts[0] + decimalParts[1];
            string result = null;
            if (exponentValue > 0)
            {
                result = newNumber + GetZeros(exponentValue - decimalParts[1].Length);
            }
            else
            {
                // negative exponent 
                result = string.Empty;
                if (newNumber.StartsWith("-"))
                {
                    result = "-";
                    newNumber = newNumber.Substring(1);
                }
                result += "0" + decSeparator + GetZeros(exponentValue + decimalParts[0].Length) + newNumber;
                result = result.TrimEnd('0');
            }
            return result;
        }

        public static string ToLongString(this float input)
        {
            string str = input.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
            // if string representation was collapsed from scientific notation, just return it: 
            if (!str.Contains("E") & !str.Contains("e"))
                return str;
            str = str.ToUpper();
            char decSeparator = '.';
            string[] exponentParts = str.Split('E');
            string[] decimalParts = exponentParts[0].Split(decSeparator);
            // fix missing decimal point: 
            if (decimalParts.Length == 1)
                decimalParts = new string[] {
				exponentParts[0],
				"0"
			};
            int exponentValue = int.Parse(exponentParts[1]);
            string newNumber = decimalParts[0] + decimalParts[1];
            string result = null;
            if (exponentValue > 0)
            {
                result = newNumber + GetZeros(exponentValue - decimalParts[1].Length);
            }
            else
            {
                // negative exponent 
                result = string.Empty;
                if (newNumber.StartsWith("-"))
                {
                    result = "-";
                    newNumber = newNumber.Substring(1);
                }
                result += "0" + decSeparator + GetZeros(exponentValue + decimalParts[0].Length) + newNumber;
                result = result.TrimEnd('0');
            }
            return result;
        }

        public static string GetZeros(int zeroCount)
        {
            if (zeroCount < 0)
                zeroCount = Math.Abs(zeroCount);
            return new string('0', zeroCount);
        }

        public static bool CheckAccessLevel(UserModes level, IRCUser User)
        {
            return CheckAccessLevel(level, User, true);
        }

        public static bool CheckAccessLevel(UserModes level, IRCUser User, bool throwException)
        {
            bool result = false;
            if (User.name.Equals(OpName, StringComparison.CurrentCultureIgnoreCase))
                result = true;
            if (User.mode >= level)
                result = true;
            if (!result && throwException)
                throw new CommandAccessException("You have " + User.mode.ToString() + "-level access, but the command you tried to access requires at least " + level.ToString() + "-level access.");
            return result;
        }

        internal static void feedtimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (IRC IrcObject in IrcApp.IrcObjects)
            {
                if (!IrcObject.Connected) continue;
                foreach (IRCChannel chan in IrcObject.IrcChannels)
                {
                    if (!chan.Active) continue;
                    foreach (IRCChannel.FeedInfo feedinf in chan.feeds)
                    {
                        XmlReader r = null;
                        for (int trycnt = 0; trycnt < 3; trycnt++)
                        {
                            try
                            {
                                r = XmlReader.Create(feedinf.url);
                                break;
                            }
                            catch { }
                        }
                        if (r == null) continue;
                        XmlSerializer xs = new XmlSerializer(typeof(RssFeed));
                        try
                        {
                            if (!xs.CanDeserialize(r))
                            {
                                xs = new XmlSerializer(typeof(AtomFeed));
                                AtomFeed feed = (AtomFeed)xs.Deserialize(r);
                                r.Close();
                                feedinf.title = feed.Title.Text;
                                Array.Sort(feed.Entries);
                                AtomFeedEntry lastentry = feed.Entries[feed.Entries.Length - 1];
                                for (int i = 0; i < feed.Entries.Length; i++)
                                {
                                    AtomFeedEntry entry = feed.Entries[i];
                                    if ((entry.GetPostTimestamp() ?? DateTime.MinValue) <= feedinf.lastupdate) continue;
                                    string author = entry.Author != null ? entry.Author.Name : "";
                                    string ititle = System.Web.HttpUtility.HtmlDecode(entry.Title.Text).Replace("\r", " ").Replace("\n", " ").TrimExcessSpaces().Trim();
                                    Uri url = new Uri(new Uri(feedinf.url), entry.Link.URL);
                                    IrcObject.WriteMessage(Module1.UnderChar + feed.Title.Text + Module1.UnderChar + ": " + Module1.UnderChar + ititle + Module1.UnderChar + (string.IsNullOrEmpty(author) ? "" : " by " + Module1.UnderChar + author + Module1.UnderChar) + ": " + url.ToString(), chan.Name);
                                }
                                feedinf.lastupdate = lastentry.Created ?? lastentry.Published ?? lastentry.Updated ?? DateTime.Now;
                            }
                            else
                            {
                                RssFeed feed = (RssFeed)xs.Deserialize(r);
                                r.Close();
                                feedinf.title = feed.Channel.Title;
                                Array.Sort(feed.Channel.Entries);
                                RssFeedEntry lastentry = feed.Channel.Entries[feed.Channel.Entries.Length - 1];
                                feedinf.lastupdate = lastentry.PubDate ?? DateTime.Now;
                                for (int i = 0; i < feed.Channel.Entries.Length; i++)
                                {
                                    RssFeedEntry entry = feed.Channel.Entries[i];
                                    if ((entry.GetPostTimestamp() ?? DateTime.MinValue) <= feedinf.lastupdate) continue;
                                    string author = entry.Author != null ? entry.Author : (entry.Creator != null ? entry.Creator : "");
                                    string ititle = System.Web.HttpUtility.HtmlDecode(entry.Title).Replace("\r", " ").Replace("\n", " ").TrimExcessSpaces().Trim();
                                    Uri url = new Uri(new Uri(feedinf.url), entry.Link);
                                    IrcObject.WriteMessage(Module1.UnderChar + feed.Channel.Title + Module1.UnderChar + ": " + Module1.UnderChar + ititle + Module1.UnderChar + (string.IsNullOrEmpty(author) ? "" : " by " + Module1.UnderChar + author + Module1.UnderChar) + ": " + url.ToString(), chan.Name);
                                }
                            }
                        }
                        catch
                        { continue; }
                        System.Threading.Thread.Sleep(5000);
                    }
                }
            }
        }

        static public string Choose(int index, params string[] choices)
        {
            return choices[index - 1];
        }

        public static IRC GetNetworkByName(string name)
        {
            foreach (IRC item in IrcApp.IrcObjects)
            {
                if (item.name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Compares the string against a given pattern.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="wildcard">The wildcard, where "*" means any sequence of characters, and "?" means any single character.</param>
        /// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
        public static bool Like(this string str, string wildcard)
        {
            return new System.Text.RegularExpressions.Regex(
                "^" + System.Text.RegularExpressions.Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline
                ).IsMatch(str);
        }

        /// <summary>
        /// Strips formatting codes from a string.
        /// </summary>
        public static string Strip(this string message)
        {
            System.Text.StringBuilder output = new System.Text.StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                switch (message[i])
                {
                    case ColorChar:
                        if (i + 1 < message.Length && char.IsNumber(message, i + 1))
                        {
                            if (i + 2 < message.Length && char.IsNumber(message, i + 2))
                                i += 2;
                            else
                                i += 1;
                            if (i + 1 < message.Length && message[i + 1] == ',')
                            {
                                i += 1;
                                if (i + 1 < message.Length && char.IsNumber(message, i + 1))
                                {
                                    if (i + 2 < message.Length && char.IsNumber(message, i + 2))
                                        i += 2;
                                    else
                                        i += 1;
                                }
                            }
                        }
                        break;
                    case RevChar:
                    case StopChar:
                    case BoldChar:
                    case UnderChar:
                    case ItalicChar:
                        break;
                    default:
                        output.Append(message[i]);
                        break;
                }
            }
            return output.ToString();
        }

        /// <summary>
        /// Strips color codes from a string.
        /// </summary>
        public static string StripColor(this string message)
        {
            System.Text.StringBuilder output = new System.Text.StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                switch (message[i])
                {
                    case ColorChar:
                        if (i + 1 < message.Length && char.IsNumber(message, i + 1))
                        {
                            if (i + 2 < message.Length && char.IsNumber(message, i + 2))
                                i += 2;
                            else
                                i += 1;
                            if (i + 1 < message.Length && message[i + 1] == ',')
                            {
                                i += 1;
                                if (i + 1 < message.Length && char.IsNumber(message, i + 1))
                                {
                                    if (i + 2 < message.Length && char.IsNumber(message, i + 2))
                                        i += 2;
                                    else
                                        i += 1;
                                }
                            }
                        }
                        break;
                    default:
                        output.Append(message[i]);
                        break;
                }
            }
            return output.ToString();
        }

        public static char[] Digits;
        public static string BaseConv(string value, int baseIn, int baseOut)
        {
            if (Digits == null)
            {
                List<char> digits = new List<char>();
                for (int i = 0x30; i < 0x3A; i++)
                    digits.Add((char)i);
                for (int i = 0x41; i < 0x5B; i++)
                    digits.Add((char)i);
                Digits = digits.ToArray();
            }
            if (baseIn < 2 | baseIn > 36) throw new ArgumentOutOfRangeException("baseIn", baseIn, "Base must be between 2 and 36.");
            if (baseOut < 2 | baseOut > 36) throw new ArgumentOutOfRangeException("baseOut", baseOut, "Base must be between 2 and 36.");
            if (baseIn == baseOut) return value; // shortcut
            bool negative = false;
            if (value.StartsWith("-"))
            {
                value = value.Substring(1);
                negative = true;
            }
            value = value.ToUpper();
            if (value == "0")
                return "0";
            System.Numerics.BigInteger valueD = 0;
            int digit;
            int chari = value.Length - 1;
            for (int i = 0; i < value.Length; i++)
            {
                digit = Array.IndexOf(Digits, value[chari]);
                if (digit >= baseIn | digit == -1) throw new FormatException("Input string was not in a correct format.");
                valueD = valueD + (digit * System.Numerics.BigInteger.Pow(baseIn, i));
                chari--;
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (valueD > 0)
            {
                digit = (int)(valueD % baseOut);
                sb.Insert(0, Digits[digit]);
                valueD /= baseOut;
            }
            return (negative ? "-" : "") + sb.ToString().TrimStart('0');
        }

        public static string ToBase(this byte value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                case 16:
                    return value.ToString("X");
                default:
                    return BaseConv(value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo), 10, @base);
            }
        }

        public static string ToBase(this sbyte value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                case 16:
                    return value.ToString("X");
                default:
                    return unchecked((byte)value).ToBase(@base);
            }
        }

        public static string ToBase(this ushort value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                case 16:
                    return value.ToString("X");
                default:
                    return BaseConv(value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo), 10, @base);
            }
        }

        public static string ToBase(this short value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                case 16:
                    return value.ToString("X");
                default:
                    return unchecked((ushort)value).ToBase(@base);
            }
        }

        public static string ToBase(this uint value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                case 16:
                    return value.ToString("X");
                default:
                    return BaseConv(value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo), 10, @base);
            }
        }

        public static string ToBase(this int value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                case 16:
                    return value.ToString("X");
                default:
                    return unchecked((uint)value).ToBase(@base);
            }
        }

        public static string ToBase(this ulong value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                case 16:
                    return value.ToString("X");
                default:
                    return BaseConv(value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo), 10, @base);
            }
        }

        public static string ToBase(this long value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                case 16:
                    return value.ToString("X");
                default:
                    return unchecked((ulong)value).ToBase(@base);
            }
        }

        public static string ToBase(this float value, int @base)
        {
            if (float.IsInfinity(value) | float.IsNaN(value)) return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
            switch (@base)
            {
                case 10:
                    return value.ToLongString();
                default:
                    return new System.Numerics.BigInteger(value).ToBase(@base);
            }
        }

        public static string ToBase(this double value, int @base)
        {
            if (double.IsInfinity(value) | double.IsNaN(value)) return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
            switch (@base)
            {
                case 10:
                    return value.ToLongString();
                default:
                    return new System.Numerics.BigInteger(value).ToBase(@base);
            }
        }

        public static string ToBase(this decimal value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                default:
                    return new System.Numerics.BigInteger(value).ToBase(@base);
            }
        }

        public static string ToBase(this System.Numerics.BigInteger value, int @base)
        {
            switch (@base)
            {
                case 10:
                    return value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                case 16:
                    return value.ToString("X");
                default:
                    return BaseConv(value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo), 10, @base);
            }
        }

        static List<Func<LinkCheckParams, bool>> linkHandlers = new List<Func<LinkCheckParams, bool>>();

        public static void AddLinkHandler(Func<LinkCheckParams, bool> handler)
        {
            linkHandlers.Add(handler);
        }

        public static void LinkCheck(object obj)
        {
            LinkCheckParams param = (LinkCheckParams)obj;
            foreach (Func<LinkCheckParams, bool> item in linkHandlers)
                if (item(param))
                    return;
            IRC IrcObject = param.IrcObject;
            string Channel = param.Channel;
            string url = param.Url;
            bool fullinfo = param.FullInfo;
            try
            {
                HttpWebRequest s = (HttpWebRequest)HttpWebRequest.Create(url);
                s.Credentials = CredentialCache.DefaultCredentials;
                s.UserAgent = useragent;
                s.Timeout = 10000;
                s.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                HttpWebResponse response = (HttpWebResponse)s.GetResponse();
                string msg = "Type: " + response.ContentType;
                if (response.ContentLength >= 0)
                    msg += " Size: " + Module1.smartsize((ulong)response.ContentLength);
                TimeSpan updated = DateTime.Now - response.LastModified;
                if (updated > TimeSpan.Zero)
                    msg += " Updated: " + updated.ToStringCust() + " ago";
                if (response.ContentType.StartsWith("text") | response.ContentType.StartsWith("application/xhtml+xml"))
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();
                    reader.Close();
                    if (response.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) | response.ContentType.StartsWith("application/xhtml+xml"))
                    {
                        int start = responseFromServer.IndexOf("<title", StringComparison.CurrentCultureIgnoreCase);
                        if (start > -1)
                            start = responseFromServer.IndexOf('>', start) + 1;
                        int length = responseFromServer.IndexOf("</title>", StringComparison.CurrentCultureIgnoreCase) - start;
                        if (start == -1 || length < 0)
                            msg += " Sample: " + responseFromServer.Substring(0, Math.Min(50, responseFromServer.Length)).Replace("\r", " ").Replace("\n", " ") + "...";
                        else
                        {
                            if (fullinfo)
                                msg += " Title: " + System.Web.HttpUtility.HtmlDecode(responseFromServer.Substring(start, length).Replace("\r", " ").Replace("\n", " ")).TrimExcessSpaces().Trim();
                            else
                                msg = "Title: " + System.Web.HttpUtility.HtmlDecode(responseFromServer.Substring(start, length).Replace("\r", " ").Replace("\n", " ")).TrimExcessSpaces().Trim();
                        }
                    }
                    else
                        msg += " Sample: " + responseFromServer.Substring(0, Math.Min(50, responseFromServer.Length)).Replace("\r", " ").Replace("\n", " ") + "...";
                }
                IrcObject.WriteMessage(msg, Channel);
            }
            catch (UriFormatException) { }
            catch (Exception ex) { IrcObject.WriteMessage("Link error: " + ex.Message, Channel); }
        }

        static readonly Regex REGEX_PUNCTUATION = new Regex("[',.!?\"]");
        private static bool tc2_line = false;
        static readonly string[] rainbowcolors = { "04", "07", "08", "09", "11", "02", "06" };
        public static string Translate(this string message)
        {
            string outline = message;
            Random rand = new Random();
            foreach (string tran in translate.Split(','))
            {
                string trans = tran;
                if (trans == "random")
                    trans = Module1.Choose(rand.Next(translatetypes.Length - 1) + 1, translatetypes);
                if (trans == "randtroll")
                    trans = Choose(rand.Next(trolls.Length - 1) + 1, trolls);
                switch (trans)
                {
                    case "off":
                        break;
                    case "allcaps":
                        outline = outline.ToUpper();
                        break;
                    case "nocaps":
                        outline = outline.ToLower();
                        break;
                    case "invcaps":
                        char[] secline = new char[outline.Length];
                        for (int i = 0; i <= outline.Length - 1; i++)
                            if (char.IsLower(outline[i]))
                                secline[i] = char.ToUpper(outline[i]);
                            else if (char.IsUpper(outline[i]))
                                secline[i] = char.ToLower(outline[i]);
                            else
                                secline[i] = outline[i];
                        outline = new string(secline);
                        break;
                    case "altcaps":
                        secline = new char[outline.Length];
                        for (int i = 0; i <= outline.Length - 1; i++)
                            if (i % 2 == 0)
                                secline[i] = char.ToUpper(outline[i]);
                            else
                                secline[i] = char.ToLower(outline[i]);
                        outline = new string(secline);
                        break;
                    case "rainbow":
                        {
                            outline = outline.StripColor();
                            StringBuilder result = new StringBuilder(outline.Length);
                            int i = 0;
                            foreach (char item in outline)
                            {
                                if (!char.IsWhiteSpace(item))
                                    result.Append(Module1.ColorChar + rainbowcolors[i++]);
                                result.Append(item);
                                i %= 7;
                            }
                            result.Append(Module1.ColorChar);
                            outline = result.ToString();
                        }
                        break;
                    case "reverse":
                        string seclinec = string.Empty;
                        for (int i = outline.Length - 1; i >= 0; i += -1)
                            seclinec += outline[i];
                        outline = seclinec;
                        break;
                    case "underline":
                        outline = Module1.UnderChar + outline + Module1.UnderChar;
                        break;
                    case "bold":
                        outline = Module1.BoldChar + outline + Module1.BoldChar;
                        break;
                    case "invert":
                        outline = Module1.RevChar + outline + Module1.RevChar;
                        break;
                    case "swap":
                        string[] command = outline.Split(' ');
                        bool[] nums = new bool[command.Length];
                        string[] secline2 = new string[command.Length];
                        int b = 0;
                        for (int i = 0; i <= command.Length - 1; i++)
                        {
                        lolno:
                            b = rand.Next(command.Length);
                            if (!nums[b])
                            {
                                secline2[i] = command[b];
                                nums[b] = true;
                            }
                            else
                                goto lolno;
                        }
                        outline = Module1.Recombine(secline2, 0);
                        break;
                    case "scramble":
                        bool[] nums2 = new bool[outline.Length];
                        secline = new char[outline.Length];
                        int b2 = 0;
                        for (int i = 0; i <= outline.Length - 1; i++)
                        {
                        lolno2:
                            b2 = rand.Next(outline.Length);
                            if (!nums2[b2])
                            {
                                secline[i] = outline[b2];
                                nums2[b2] = true;
                            }
                            else
                                goto lolno2;
                        }
                        outline = new string(secline);
                        break;
                    case "hs-aa":
                        string[] split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = REGEX_PUNCTUATION.Replace(split[i].ToLower(), string.Empty).Replace('o', '0');
                        outline = ColorChar + "05" + string.Join(" ", split);
                        break;
                    case "hs-at":
                        split = outline.Split(' ');
                        bool cap = true;
                        for (int i = 0; i < split.Length; i++)
                            if (split[i].Length > 0 && !split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            {
                                split[i] = REGEX_PUNCTUATION.Replace(split[i].ToUpper(), ",");
                                if (cap)
                                    for (int j = 0; j < message.Length; j++)
                                    {
                                        bool found = false;
                                        switch (split[i][j])
                                        {
                                            case ColorChar:
                                                if (j + 1 < split[i].Length && char.IsNumber(split[i], j + 1))
                                                {
                                                    if (j + 2 < split[i].Length && char.IsNumber(split[i], j + 2))
                                                        j += 2;
                                                    else
                                                        j += 1;
                                                    if (j + 1 < split[i].Length && split[i][j + 1] == ',')
                                                    {
                                                        j += 1;
                                                        if (j + 1 < split[i].Length && char.IsNumber(split[i], j + 1))
                                                        {
                                                            if (j + 2 < split[i].Length && char.IsNumber(split[i], j + 2))
                                                                j += 2;
                                                            else
                                                                j += 1;
                                                        }
                                                    }
                                                }
                                                break;
                                            case RevChar:
                                            case StopChar:
                                            case BoldChar:
                                            case UnderChar:
                                            case ItalicChar:
                                                break;
                                            default:
                                                split[i] = (j > 0 ? split[i].Substring(0, j) : string.Empty) + char.ToLower(split[i][j]) + (split[i].Length > j + 1 ? split[i].Substring(j + 1) : string.Empty);
                                                found = true;
                                                break;
                                        }
                                        if (found)
                                            break;
                                    }
                                cap = split[i].EndsWith(",");
                            }
                        outline = ColorChar + "05" + string.Join(" ", split);
                        break;
                    case "hs-ta":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = Regex.Replace(split[i].ToLower().Replace('s', '2').Replace("i", "ii"), @"too?\b", "two");
                        outline = ColorChar + "07" + string.Join(" ", split);
                        break;
                    case "hs-cg":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = split[i].ToUpper();
                        outline = ColorChar + "14" + string.Join(" ", split);
                        if (!outline.EndsWith(".") & !outline.EndsWith("!") & !outline.EndsWith("?"))
                            outline += ".";
                        if (rand.Next(1, 100) < 25)
                        {
                            string[] insults = { "FUCKASS", "NOOKSNIFFER" };
                            outline = outline.Insert(outline.Length - 1, ", " + insults[rand.Next(insults.Length)]);
                        }
                        break;
                    case "hs-ac":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = Regex.Replace(split[i].ToLower().Replace("per", "purr").Replace("pau", "paw").Replace("pon", "pawn").Replace("cause", "claws").Replace("ee", "33"), ":([!-~])", ":$1$1", RegexOptions.IgnoreCase);
                        outline = ColorChar + "03:33 < " + string.Join(" ", split);
                        break;
                    case "hs-ga":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (split[i].Length > 0 && !split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            {
                                split[i] = REGEX_PUNCTUATION.Replace(split[i].ToLower(), string.Empty);
                                for (int j = 0; j < message.Length; j++)
                                {
                                    bool found = false;
                                    switch (split[i][j])
                                    {
                                        case ColorChar:
                                            if (j + 1 < split[i].Length && char.IsNumber(split[i], j + 1))
                                            {
                                                if (j + 2 < split[i].Length && char.IsNumber(split[i], j + 2))
                                                    j += 2;
                                                else
                                                    j += 1;
                                                if (j + 1 < split[i].Length && split[i][j + 1] == ',')
                                                {
                                                    j += 1;
                                                    if (j + 1 < split[i].Length && char.IsNumber(split[i], j + 1))
                                                    {
                                                        if (j + 2 < split[i].Length && char.IsNumber(split[i], j + 2))
                                                            j += 2;
                                                        else
                                                            j += 1;
                                                    }
                                                }
                                            }
                                            break;
                                        case RevChar:
                                        case StopChar:
                                        case BoldChar:
                                        case UnderChar:
                                        case ItalicChar:
                                            break;
                                        default:
                                            split[i] = (j > 0 ? split[i].Substring(0, j) : string.Empty) + char.ToUpper(split[i][j]) + (split[i].Length > j + 1 ? split[i].Substring(j + 1) : string.Empty);
                                            found = true;
                                            break;
                                    }
                                    if (found)
                                        break;
                                }
                            }
                        outline = ColorChar + "03" + string.Join(" ", split);
                        break;
                    case "hs-gc":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = REGEX_PUNCTUATION.Replace(split[i].ToUpper(), string.Empty).Replace('A', '4').Replace('I', '1').Replace('E', '3');
                        outline = ColorChar + "10" + string.Join(" ", split);
                        break;
                    case "hs-ag":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = Regex.Replace(Regex.Replace(Regex.Replace(split[i].Replace('B', '8').Replace('b', '8').Replace("ate", "8").Replace("8reak", "8r8k").Replace("aint", "8nt").Replace("ation", "8tion"), @"([a-z])\1{2,}", "$1$1$1$1$1$1$1$1", RegexOptions.IgnoreCase), @"([:;]-?[\(\[\)\]D])", ":::$1"), "!+", "!!!!!!!!");
                        outline = ColorChar + "10" + string.Join(" ", split);
                        break;
                    case "hs-ct":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = Regex.Replace(Regex.Replace(split[i].Replace('x', '%').Replace('X', '%').Replace("cross", "%"), @"\b[.!\?]", string.Empty), "(loo|ool)", new MatchEvaluator(delegate(Match match) { return match.Captures[0].Value.ToLower().Replace('l', '1').Replace('o', '0'); }), RegexOptions.IgnoreCase);
                        outline = ColorChar + "02D --> " + string.Join(" ", split);
                        break;
                    case "hs-tc":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            {
                                System.Text.StringBuilder ns = new System.Text.StringBuilder(outline.Length);
                                bool flip = true;
                                for (var j = 0; j < split[i].Length; j++)
                                {
                                    ns.Append(flip ? Char.ToUpper(split[i][j]) : char.ToLower(split[i][j]));
                                    flip = !flip;
                                }
                                split[i] = ns.ToString();
                            }
                        outline = ColorChar + "02" + string.Join(" ", split);
                        break;
                    case "hs-tc2":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = (tc2_line ? split[i].ToLower() : split[i].ToUpper());
                        outline = ColorChar + "02" + string.Join(" ", split);
                        tc2_line = !tc2_line;
                        break;
                    case "hs-ca":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = REGEX_PUNCTUATION.Replace(Regex.Replace(split[i].ToLower().Replace("w", "ww").Replace("v", "vv"), @"ing\b", "in"), string.Empty);
                        outline = ColorChar + "06" + string.Join(" ", split);
                        break;
                    case "hs-cc":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = split[i].Replace("h", ")(").Replace("H", ")(").Replace("E", "-E");
                        outline = ColorChar + "05" + string.Join(" ", split);
                        break;
                    case "hs-uu":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = split[i].ToLower().Replace('u', 'U');
                        outline = ColorChar + "15" + string.Join(" ", split);
                        break;
                    case "hs-uu2":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                split[i] = split[i].ToUpper().Replace('U', 'u').Replace(',', '.');
                        outline = ColorChar + "01" + string.Join(" ", split);
                        break;
                    case "hs-ds":
                        outline = ColorChar + "00" + outline;
                        break;
                    case "sbahj":
                        split = outline.Split(' ');
                        for (int i = 0; i < split.Length; i++)
                            if (!split[i].Strip().StartsWith("http://", StringComparison.OrdinalIgnoreCase) & !split[i].Strip().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            {
                                split[i] = split[i].ToLower();
                                if (Random.Next(1, 100) < 50)
                                    split[i] = mispeller(split[i]);
                                if (rand.Next(1, 100) < 10)
                                    split[i] = split[i].ToUpper();
                            }
                        outline = string.Join(" ", split);
                        break;
                }
            }
            return outline;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue @default)
        {
            TValue output;
            if (dict.TryGetValue(key, out output))
                return output;
            return @default;
        }

        static char[][] kbloc = { "1234567890-=".ToCharArray(),
         "qwertyuiop[]".ToCharArray(),
         "asdfghjkl:;'".ToCharArray(),
         "zxcvbnm,.>/?".ToCharArray()};

        static Dictionary<char, int[]> kbdict;

        static Dictionary<char, string> sounddict = new Dictionary<char, string>() { { 'a', "e" }, { 'b', "d" }, { 'c', "k" }, { 'd', "g" }, { 'e', "eh" },
             { 'f', "ph" }, { 'g', "j" }, { 'h', "h" }, { 'i', "ai" }, { 'j', "ge" },
             { 'k',  "c" }, { 'l', "ll" }, { 'm', "n" }, { 'n', "m" }, { 'o', "oa" },
             { 'p', "b" }, { 'q', "kw" }, { 'r', "ar" }, { 's', "ss" }, { 't', "d" },
             { 'u', "you" }, { 'v', "w" }, { 'w', "wn" }, { 'x', "cks" }, { 'y', "uy" }, { 'z', "s" } };

        static Func<string, int, string>[] funcs = { mistype, transpose, randomletter, randomreplace, soundalike };

        public static readonly Random Random = new Random();

        static string mispeller(string word)
        {
            if (kbdict == null)
            {
                kbdict = new Dictionary<char, int[]>();
                for (int i = 0; i < kbloc.Length; i++)
                    for (int j = 0; j < kbloc[i].Length; j++)
                        kbdict.Add(kbloc[i][j], new int[] { i, j });
            }
            int num;
            if (word.Length <= 6)
                num = 1;
            else
                num = Random.Next(1, 3);
            List<int> wordseq = new List<int>(num);
            int b2 = Random.Next(word.Length);
            for (int i = 0; i < num; i++)
            {
                while (wordseq.Contains(b2))
                    b2 = Random.Next(word.Length);
                word = funcs[Random.Next(funcs.Length)](word, b2);
                wordseq.Add(b2);
            }
            return word;
        }

        static string mistype(string @string, int i)
        {
            char l = @string[i];
            if (!kbdict.ContainsKey(l))
                return @string;
            int[] lpos = kbdict[l];
            int[] newpos = lpos;
            while (newpos[0] == lpos[0] & newpos[1] == lpos[1])
                newpos = new int[] {ModNeg(lpos[0] + Random.Next(-1, 2), kbloc.Length),
                      ModNeg(lpos[1] + Random.Next(-1,2), kbloc[0].Length)};
            @string = @string.Substring(0, i) + kbloc[newpos[0]][newpos[1]] + @string.Substring(i + 1);
            return @string;
        }

        static string transpose(string @string, int i)
        {
            int j = ModNeg(i + (Random.Next(2) == 0 ? -1 : 1), @string.Length - 1);
            char[] l = @string.ToCharArray();
            l[i] = @string[j];
            l[j] = @string[i];
            return new string(l);
        }

        static string randomletter(string @string, int i)
        {
            @string = @string.Substring(0, i + 1) + "abcdefghijklmnopqrstuvwxyz"[Random.Next(0, 26)] + @string.Substring(i + 1);
            return @string;
        }

        static string randomreplace(string @string, int i)
        {
            @string = @string.Substring(0, i) + "abcdefghijklmnopqrstuvwxyz"[Random.Next(0, 26)] + @string.Substring(i + 1);
            return @string;
        }

        static string soundalike(string @string, int i)
        {
            string c;
            try
            {
                c = sounddict[@string[i]];
            }
            catch
            {
                return @string;
            }
            @string = @string.Substring(0, i) + c + @string.Substring(i + 1);
            return @string;
        }

        static int ModNeg(int value, int max)
        {
            int result = value;
            if (result > max)
                result = max + (max - result);
            if (result < 0)
                result = -result;
            return result;
        }

        public static byte[] DataToBytes(string type, string data)
        {
            switch (type.ToLower())
            {
                case "bytes":
                    return StringToBytes(data);
                case "byte":
                    return new byte[] { Convert.ToByte(data) };
                case "sbyte":
                    return new byte[] { (byte)Convert.ToSByte(data) };
                case "short":
                case "int16":
                case "sword":
                    return BitConverter.GetBytes(Convert.ToInt16(data));
                case "ushort":
                case "uint16":
                case "word":
                    return BitConverter.GetBytes(Convert.ToUInt16(data));
                case "int":
                case "int32":
                case "integer":
                case "sdword":
                    return BitConverter.GetBytes(Convert.ToInt32(data));
                case "uint":
                case "uint32":
                case "uinteger":
                case "dword":
                    return BitConverter.GetBytes(Convert.ToUInt32(data));
                case "long":
                case "int64":
                case "sqword":
                    return BitConverter.GetBytes(Convert.ToInt64(data));
                case "ulong":
                case "uint64":
                case "qword":
                    return BitConverter.GetBytes(Convert.ToUInt64(data));
                case "single":
                case "float":
                    return BitConverter.GetBytes(Convert.ToSingle(data));
                case "double":
                    return BitConverter.GetBytes(Convert.ToDouble(data));
                case "bigint":
                case "biginteger":
                case "number":
                    return System.Numerics.BigInteger.Parse(data).ToByteArray();
                case "datetime":
                case "date":
                case "time":
                    return BitConverter.GetBytes(GetDate(data).Value.ToBinary());
                case "timespan":
                    return BitConverter.GetBytes(GetTimeSpan(data).Value.Ticks);
                case "base64":
                    return Convert.FromBase64String(data);
                case "ascii":
                    return System.Text.Encoding.ASCII.GetBytes(data);
                case "utf8":
                case "text":
                case "string":
                    return System.Text.Encoding.UTF8.GetBytes(data);
                case "unicode":
                case "utf16":
                    return System.Text.Encoding.Unicode.GetBytes(data);
                default:
                    int cdpg = 0;
                    if (int.TryParse(type, out cdpg))
                        return System.Text.Encoding.GetEncoding(cdpg).GetBytes(data);
                    else
                        return System.Text.Encoding.GetEncoding(type).GetBytes(data);
            }
        }

        public static dynamic BytesToData(string type, byte[] bytes)
        {
            switch (type)
            {
                case "bytes":
                    return bytes;
                case "byte":
                    return bytes[0];
                case "sbyte":
                    return (sbyte)bytes[0];
                case "short":
                case "int16":
                case "sword":
                    return BitConverter.ToInt16(bytes, 0);
                case "ushort":
                case "uint16":
                case "word":
                    return BitConverter.ToUInt16(bytes, 0);
                case "int":
                case "int32":
                case "integer":
                case "sdword":
                    return BitConverter.ToInt32(bytes, 0);
                case "uint":
                case "uint32":
                case "uinteger":
                case "dword":
                    return BitConverter.ToUInt32(bytes, 0);
                case "long":
                case "int64":
                case "sqword":
                    return BitConverter.ToInt64(bytes, 0);
                case "ulong":
                case "uint64":
                case "qword":
                    return BitConverter.ToUInt64(bytes, 0);
                case "single":
                case "float":
                    return BitConverter.ToSingle(bytes, 0);
                case "double":
                    return BitConverter.ToDouble(bytes, 0);
                case "number":
                case "bigint":
                case "biginteger":
                    return new System.Numerics.BigInteger(bytes);
                case "datetime":
                case "date":
                case "time":
                    return DateTime.FromBinary(BitConverter.ToInt64(bytes, 0));
                case "timespan":
                    return TimeSpan.FromTicks(BitConverter.ToInt64(bytes, 0));
                case "base64":
                    return Convert.ToBase64String(bytes);
                case "ascii":
                    return Encoding.ASCII.GetString(bytes);
                case "utf8":
                case "text":
                case "string":
                    return Encoding.UTF8.GetString(bytes);
                case "unicode":
                case "utf16":
                    return Encoding.Unicode.GetString(bytes);
                default:
                    int cdpg = 0;
                    if (int.TryParse(type, out cdpg))
                        return Encoding.GetEncoding(cdpg).GetString(bytes);
                    else
                        return Encoding.GetEncoding(type).GetString(bytes);
            }
        }

        public static string[] ParseCommandLine(string commandLine)
        {
            List<string> args2 = new List<string>();
            string curcmd = string.Empty;
            bool quotes = false;
            foreach (char item in commandLine)
            {
                switch (item)
                {
                    case ' ':
                        if (!quotes)
                        {
                            if (!string.IsNullOrEmpty(curcmd))
                                args2.Add(curcmd);
                            curcmd = string.Empty;
                        }
                        else
                            goto default;
                        break;
                    case '"':
                        if (quotes)
                        {
                            args2.Add(curcmd);
                            curcmd = string.Empty;
                            quotes = false;
                        }
                        else
                            quotes = true;
                        break;
                    default:
                        curcmd += item;
                        break;
                }
            }
            if (!string.IsNullOrEmpty(curcmd))
                args2.Add(curcmd);
            return args2.ToArray();
        }

        internal static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dict, IDictionary<TKey, TValue> other)
        {
            foreach (KeyValuePair<TKey, TValue> item in other)
                dict.Add(item.Key, item.Value);
        }

        internal static string ConvertToString(this object @object)
        {
            if (@object is string) return (string)@object;
            if (@object == null) return null;
            System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(@object);
            if (converter != null && !(converter is System.ComponentModel.ComponentConverter) && converter.GetType() != typeof(System.ComponentModel.TypeConverter))
            {
                if (converter.CanConvertTo(typeof(string)))
                    return converter.ConvertToInvariantString(@object);
            }
            else if (@object is Type)
                return ((Type)@object).AssemblyQualifiedName;
            return null;
        }

        private static bool IsComplexType(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(type);
                    if (converter != null && !(converter is System.ComponentModel.ComponentConverter) && converter.GetType() != typeof(System.ComponentModel.TypeConverter))
                        if (converter.CanConvertTo(typeof(string)) & converter.CanConvertFrom(typeof(string)))
                            return false;
                    if (type.GetType() == typeof(Type))
                        return false;
                    return true;
                default:
                    return false;
            }
        }

        public static Dictionary<string, string> ToStringDictionary(this object obj)
        {
            Dictionary<string, dynamic> dictionary = obj.ToDictionary();
            Dictionary<string, string> result = new Dictionary<string, string>(dictionary.Count);
            foreach (KeyValuePair<string,dynamic> item in dictionary)
                result.Add(item.Key, ConvertToString(item.Value));
            return result;
        }

        public static Dictionary<string, dynamic> ToDictionary(this object obj)
        {
            Dictionary<string, dynamic> dictionary = new Dictionary<string, dynamic>();
            ToDictionaryInternal(obj, dictionary, string.Empty);
            return dictionary;
        }

        private static void ToDictionaryInternal(object value, Dictionary<string, dynamic> dictionary, string prefix)
        {
            if (value == null | value == DBNull.Value)
            {
                dictionary[prefix] = value;
                return;
            }
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Object:
                    if (value is IList)
                    {
                        int i = 0;
                        foreach (object item in (IList)value)
                            ToDictionaryInternal(item, dictionary, prefix + "[" + i++ + "]");
                        return;
                    }
                    if (value is IDictionary)
                    {
                        foreach (DictionaryEntry item in (IDictionary)value)
                            if (!item.Key.GetType().IsComplexType())
                                ToDictionaryInternal(item, dictionary, prefix + "[" + item.Key.ConvertToString() + "]");
                        return;
                    }
                    string newpfx = prefix;
                    if (!string.IsNullOrEmpty(newpfx))
                        newpfx += '.';
                    foreach (MemberInfo member in value.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance))
                    {
                        object item;
                        switch (member.MemberType)
                        {
                            case MemberTypes.Field:
                                FieldInfo field = (FieldInfo)member;
                                item = field.GetValue(value);
                                break;
                            case MemberTypes.Property:
                                PropertyInfo property = (PropertyInfo)member;
                                if (property.GetIndexParameters().Length > 0) continue;
                                MethodInfo getmethod = property.GetGetMethod();
                                if (getmethod == null) continue;
                                item = getmethod.Invoke(value, null);
                                break;
                            default:
                                continue;
                        }
                        ToDictionaryInternal(item, dictionary, newpfx + member.Name);
                    }
                    break;
                default:
                    dictionary[prefix] = value;
                    break;
            }
        }
    }

    public class LinkCheckParams
    {
        public IRC IrcObject { get; private set; }
        public string Channel { get; private set; }
        public string Url { get; private set; }
        public bool FullInfo { get; private set; }

        public LinkCheckParams(IRC ircobject, string channel, string url, bool full)
        {
            IrcObject = ircobject;
            Channel = channel;
            Url = url;
            FullInfo = full;
        }
    }

    [Serializable()]
    public class CodeRunner : MarshalByRefObject
    {
        public Func<object> CustomCodeDelegate;
        public object CustomCodeResult;
        public void CustomCodeRunner()
        {
            CustomCodeResult = CustomCodeDelegate();
        }
    }

    public class ProcessInfo
    {
        public Process proc;
        public string chan;
        public string name = string.Empty;

        public IRC IrcObject;
        public ProcessInfo(string commandLine, IRC IrcObject, string chan)
        {
            try
            {
                this.chan = chan;
                this.IrcObject = IrcObject;
                string prg = null;
                string arg = "";
                if (!commandLine.StartsWith("\""))
                {
                    string[] command = commandLine.Split(' ');
                    prg = command[0];
                    if (command.Length > 1)
                        arg = Module1.Recombine(command, 1);
                }
                else
                {
                    prg = commandLine.Substring(0, commandLine.IndexOf('"', 1) + 1);
                    arg = commandLine.SafeSubstring(commandLine.IndexOf('"', 1) + 1);
                }
                proc = new Process();
                proc.StartInfo.FileName = prg;
                proc.StartInfo.Arguments = arg;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.EnableRaisingEvents = true;
                proc.Exited += proc_Exited;
                proc.OutputDataReceived += proc_DataReceived;
                proc.ErrorDataReceived += proc_DataReceived;
                proc.Start();
                name = proc.ProcessName;
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Module1.IrcApp.stacktrace = ex.StackTrace;
                IrcObject.WriteMessage(ex.GetType().Name + " in " + ex.Source + ": " + ex.Message, chan);
            }
        }

        private void proc_Exited(object sender, System.EventArgs e)
        {
            try
            {
                IrcObject.WriteMessage("Process " + name + " exited with code " + proc.ExitCode, chan);
                proc.Dispose();
            }
            catch (Exception ex)
            {
                Module1.IrcApp.stacktrace = ex.StackTrace;
                IrcObject.WriteMessage(ex.GetType().Name + " in " + ex.Source + ": " + ex.Message, chan);
            }
            Module1.proclist.Remove(this);
        }

        private void proc_DataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                    IrcObject.WriteMessage(name + ": " + e.Data, chan);
            }
            catch (Exception ex)
            {
                Module1.IrcApp.stacktrace = ex.StackTrace;
                IrcObject.WriteMessage(ex.GetType().Name + " in " + ex.Source + ": " + ex.Message, chan);
            }
        }
    }

    public class GUIChanInfo
    {
        public System.Windows.Forms.RichTextBox textbox = new System.Windows.Forms.RichTextBox();

        public int newlines;
        public GUIChanInfo()
        {
            textbox.BackColor = System.Drawing.SystemColors.Window;
            textbox.Dock = System.Windows.Forms.DockStyle.Fill;
            //textbox.Font = new System.Drawing.Font("Fixedsys Excelsior 3.01", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            textbox.Margin = new System.Windows.Forms.Padding(0);
            textbox.Name = "RichTextBox1";
            textbox.ReadOnly = true;
            textbox.TabIndex = 3;
            textbox.TabStop = false;
            textbox.Text = "";
            textbox.LinkClicked += Module1.myForm.RichTextBox1_LinkClicked;
        }
    }

    public class ServerInfo
    {
        public static List<ServerInfo> Load(string inifile)
        {
            Dictionary<string, ServerInfo> deserialized = IniSerializer.Deserialize<Dictionary<string, ServerInfo>>(inifile);
            List<ServerInfo> result = new List<ServerInfo>(deserialized.Count);
            foreach (KeyValuePair<string, ServerInfo> item in deserialized)
            {
                item.Value.name = item.Key;
                result.Add(item.Value);
            }
            return result;
        }

        [IniIgnore]
        public string name;
        public string servers;
        public bool usessl;
        public bool autoconnect;
        [IniName("favchans")]
        public string channels;

        public ServerInfo()
        {
        }
    }

    public class GlobalSettings
    {
        [IniName("banlist")]
        [IniCollection(IniCollectionMode.SingleLine, Format = " ")]
        public List<string> BanList { get; set; }
        [IniName("ignorelist")]
        [IniCollection(IniCollectionMode.SingleLine, Format = " ")]
        public List<string> IgnoreList { get; set; }
        [IniName("opname")]
        public string OpName { get; set; }
        [IniName("password")]
        public string Password { get; set; }

        public static GlobalSettings Load(string filename)
        {
            return IniSerializer.Deserialize<GlobalSettings>(filename);
        }

        public void Save(string filename)
        {
            IniSerializer.Serialize(this, filename);
        }
    }

    public class TimerInfo
    {
        public System.Timers.Timer timer;
        public string action;
        public string channel;
        public IRC IrcObject;

        public int repeats;
        public TimerInfo(double interval, string action, int repeat, IRC IrcObject, string channel)
        {
            this.action = action;
            repeats = repeat;
            this.channel = channel;
            this.IrcObject = IrcObject;
            timer = new System.Timers.Timer(interval * 1000)
            {
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += timer_Elapsed;
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Module1.IrcApp.IrcCommand(IrcObject, channel, action);
            if (repeats > 0)
            {
                repeats -= 1;
                if (repeats == 0)
                {
                    Module1.timers.Remove(this);
                    timer.Stop();
                }
            }
        }
    }

    [Serializable()]
    public class CommandAccessException : Exception
    {
        public CommandAccessException() { }
        public CommandAccessException(string message) : base(message) { }
        public CommandAccessException(string message, Exception inner) : base(message, inner) { }
        protected CommandAccessException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
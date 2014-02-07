using System;
using System.Collections.Generic;
using MMBot;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.IO;
using System.Web;

namespace MMBotInternetpulse
{
    public class InternetpulseModule : BotModule
    {
        public InternetpulseModule() { }

        public override void Shutdown() { }

        Regex var = new Regex(@"\s*(?:var )?(.+?) = new Array\((.+?)\)\;", RegexOptions.Singleline);
        void InternetpulseCommand(IRC IrcObject, string channel, string user, string command)
        {
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                string str = wc.DownloadString("http://internetpulse.net");
                int start = str.IndexOf("aUnit = new Array(");
                int end = str.IndexOf(");", str.IndexOf("var aThresh = new Array(")) + 2;
                string data = str.Substring(start, end - start);
                string json = "{" + var.Replace(data, "\"$1\": [$2],") + "}";
                JsonSerializer js = new JsonSerializer();
                NetData d;
                using (StringReader strr = new StringReader(json))
                using (JsonReader jr = new JsonTextReader(strr))
                    d = js.Deserialize<NetData>(jr);
                List<string> results = new List<string>();
                for (int i = 0; i < d.aData.Length; i++)
                {
                    string[] items = d.aData[i].Split('|');
                    List<string> warnings = new List<string>();
                    for (int j = 0; j < items.Length; j++)
                    {
                        string[] info = items[j].Split(';');
                        if (info[0] == "y")
                            warnings.Add(Module1.ColorChar + "08" + HttpUtility.HtmlDecode(d.aNameZ[j]) + " " + HttpUtility.HtmlDecode(info[1]) + HttpUtility.HtmlDecode(d.aUnit[j]) + Module1.ColorChar);
                        else if (info[0] == "r")
                            warnings.Add(Module1.ColorChar + "04" + HttpUtility.HtmlDecode(d.aNameZ[j]) + " " + HttpUtility.HtmlDecode(info[1]) + HttpUtility.HtmlDecode(d.aUnit[j]) + Module1.ColorChar);
                    }
                    if (warnings.Count > 0)
                        results.Add(Module1.UnderChar + HttpUtility.HtmlDecode(d.aNameX[i / d.aNameX.Length]) + " -> " + HttpUtility.HtmlDecode(d.aNameY[i % d.aNameX.Length]) + ": " + string.Join(", ", warnings) + Module1.UnderChar);
                }
                string report = "No warnings found!";
                if (results.Count > 0)
                    report = string.Join("; ", results);
                IrcObject.WriteMessage("Internet Health Report: " + report, channel);
            }
        }
    }

    public class NetData
    {
        public string[] aUnit { get; set; }
        public string[] aNameX { get; set; }
        public string[] aNameY { get; set; }
        public string[] aNameZ { get; set; }
        public string[] aData { get; set; }
        public string[] aThresh { get; set; }
    }
}
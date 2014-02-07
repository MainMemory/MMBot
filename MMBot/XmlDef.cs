using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MMBot.XML
{
    [XmlRoot(Namespace = "http://localhost")]
    public class BotModule
    {
        [XmlAttribute]
        public string name { get; set; }
        [XmlAttribute]
        public string className { get; set; }
        [XmlArray("CommandList")]
        public BotCommand[] CommandList { get; set; }
    }

    [XmlRoot(Namespace = "http://localhost")]
    public class CommandList
    {
        [XmlElement("BotCommand")]
        public BotCommand[] Commands { get; set; }
    }

    public class BotCommand
    {
        [XmlAttribute]
        public string name { get; set; }
        [XmlAttribute]
        public UserModes accessLevel { get; set; }
        [XmlAttribute]
        public string functionName;
        [XmlAttribute]
        public int cmdMinLength { get; set; }
        [XmlAttribute]
        public bool separateThread { get; set; }
        [XmlElement]
        public string HelpText { get; set; }
        [XmlIgnore]
        public bool HelpTextSpecified { get; set; }
        [XmlElement("BotCommand")]
        public BotCommand[] SubCommands;
    }
}
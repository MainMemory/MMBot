using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using MMBot;

namespace MMBotUnicode
{
    public class UnicodeModule : BotModule
    {
        public UnicodeModule()
        {
			ChangeDirectory();
			if (File.Exists("ucd.all.flat.xml"))
				ucd = UnicodeCharacterDatabase.Load("ucd.all.flat.xml");
			else
				LoadFailed = true;
			RestoreDirectory();
        }

        public override void Shutdown() { }

        UnicodeCharacterDatabase ucd;

        void CharinfoCommand(IRC IrcObject, string channel, string user, string command)
        {
            int c;
			if (command.Length == 0)
			{
				do
				{
					c = Module1.Random.Next(0x10F800);
					if (c >= 0xD800) c += 0x800;
				} while (!ucd.Repertoire.Any((item) => item.FirstCodePoint <= c && item.LastCodePoint >= c)
					|| !ucd.Blocks.Any((item) => item.FirstCodePoint <= c && item.LastCodePoint >= c));
				PrintInfo(IrcObject, channel, c);
			}
			else if (command.Length == 1 || (command.Length == 2 && char.IsHighSurrogate(command, 0) && char.IsLowSurrogate(command, 1)))
				PrintInfo(IrcObject, channel, char.ConvertToUtf32(command, 0));
			else if (command.StartsWith("U+", StringComparison.OrdinalIgnoreCase))
				PrintInfo(IrcObject, channel, int.Parse(command.Substring(2), NumberStyles.HexNumber));
			else if (int.TryParse(command, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out c))
				PrintInfo(IrcObject, channel, c);
			else
			{
				IEnumerable<CodePointInfo> cp = ucd.Repertoire.Where((a) =>
					command.Equals(a.ToString(), StringComparison.OrdinalIgnoreCase));
				if (cp.Any())
					foreach (CodePointInfo inf in cp)
						PrintInfo(IrcObject, channel, inf.FirstCodePoint);
				else
				{
					int[] matches = new int[ucd.Repertoire.Length];
					string[] line = command.ToUpperInvariant().Split(' ').Distinct().ToArray();
					for (int i = 0; i < ucd.Repertoire.Length; i++)
					{
						string[] cs = ucd.Repertoire[i].ToString().Split(' ');
						for (int j = 0; j < line.Length; j++)
							if (cs.Contains(line[j], StringComparer.OrdinalIgnoreCase))
								matches[i]++;
					}
					int highestnum = matches.Max();
					if (highestnum == 0)
						IrcObject.WriteMessage("No matches found.", channel);
					else
					{
						List<int> bestmatches = new List<int>();
						for (int i = 0; i < matches.Length; i++)
							if (matches[i] == highestnum)
								bestmatches.Add(i);
						if (bestmatches.Count > 5)
							IrcObject.WriteMessage(string.Format("Found {0} matches. Please clarify your search.", bestmatches.Count), channel);
						else
							foreach (int i in bestmatches)
								PrintInfo(IrcObject, channel, ucd.Repertoire[i].FirstCodePoint);
					}
				}
			}
        }

        void PrintInfo(IRC IrcObject, string channel, int character)
        {
            string c = char.ConvertFromUtf32(character);
            CodePointInfo cp = ucd.Repertoire.Single((item) =>
                item.FirstCodePoint <= character && item.LastCodePoint >= character);
            string blk = ucd.Blocks.Single((item) =>
                item.FirstCodePoint <= character && item.LastCodePoint >= character).Name;
            StringBuilder result = new StringBuilder();
            result.AppendFormat("Code Point: U+{0:X4} '{1}' Age: {2} Name: \"{3}\" ", character, c,
                cp.Age, cp.ToString());
            if (cp.NameAliases != null && cp.NameAliases.Length > 0)
                result.AppendFormat("Aliases: {0} ", string.Join(", ", cp.NameAliases.Select((a) =>
                    '"' + a.ToString() + '"').ToArray()));
            result.AppendFormat("Block: \"{0}\" Category: \"{1}\"", blk, GetCategoryName(cp));
            if (c.Length == 1)
            {
                char k = char.ToLowerInvariant(c[0]);
                if (k != c[0])
                    result.AppendFormat(" Lowercase: U+{0:X4} '{1}'", (int)k, k);
                k = char.ToUpperInvariant(c[0]);
                if (k != c[0])
                    result.AppendFormat(" Uppercase: U+{0:X4} '{1}'", (int)k, k);
            }
            IrcObject.WriteMessage(result.ToString(), channel);
        }

        string GetCategoryName(CodePointInfo cp)
        {
            StringBuilder result = new StringBuilder(((UnicodeCategory)cp.GeneralCategory).ToString());
            for (int i = 1; i < result.Length; i++)
                if (char.IsLower(result[i - 1]) && char.IsUpper(result[i]))
                {
                    result.Insert(i, ' ');
                    i++;
                }
            return result.ToString();
        }
    }

    [XmlRoot("ucd", Namespace = "http://www.unicode.org/ns/2003/ucd/1.0")]
    public class UnicodeCharacterDatabase
    {
        private static readonly XmlSerializer ucdserializer = new XmlSerializer(typeof(UnicodeCharacterDatabase));

        [XmlElement("description")]
        public string Description { get; set; }
        [XmlArray("repertoire")]
        [XmlArrayItem("reserved", typeof(Reserved))]
        [XmlArrayItem("noncharacter", typeof(NonCharacter))]
        [XmlArrayItem("surrogate", typeof(Surrogate))]
        [XmlArrayItem("char", typeof(Character))]
        public CodePointInfo[] Repertoire { get; set; }
        [XmlArray("blocks")]
        [XmlArrayItem("block")]
        public Block[] Blocks { get; set; }

        public static UnicodeCharacterDatabase Load(string filename)
        {
            using (FileStream fs = File.OpenRead(filename))
                return (UnicodeCharacterDatabase)ucdserializer.Deserialize(fs);
        }
    }

    public abstract class CodePointInfo
    {
        [XmlIgnore]
        public int FirstCodePoint { get; set; }
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("first-cp")]
        public string FirstCodePointString
        {
            get { return FirstCodePoint.ToString("X4"); }
            set { FirstCodePoint = int.Parse(value, NumberStyles.HexNumber); }
        }
        [XmlIgnore]
        public int LastCodePoint { get; set; }
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("last-cp")]
        public string LastCodePointString
        {
            get { return LastCodePoint.ToString("X4"); }
            set { LastCodePoint = int.Parse(value, NumberStyles.HexNumber); }
        }
        [XmlIgnore]
        public int? CodePoint
        {
            get { return FirstCodePoint == LastCodePoint ? (int?)FirstCodePoint : null; }
            set { if (value.HasValue) FirstCodePoint = LastCodePoint = value.Value; }
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("cp")]
        public string CodePointString
        {
            get { return CodePoint.HasValue ? CodePoint.Value.ToString("X4") : null; }
            set { if (value != null) CodePoint = int.Parse(value, NumberStyles.HexNumber); }
        }
        [XmlAttribute("age")]
        public string Age { get; set; }
        [XmlAttribute("na")]
        public string Name { get; set; }
        [XmlAttribute("na1")]
        public string Name1 { get; set; }
        [XmlElement("name-alias")]
        public NameAlias[] NameAliases { get; set; }
        [XmlAttribute("gc")]
        public Categories GeneralCategory { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
                return Name.Replace("#", FirstCodePointString);
            else if (!string.IsNullOrEmpty(Name1))
                return Name1.Replace("#", FirstCodePointString);
            else
                return "Unnamed";
        }
    }

    public enum Categories
    {
        Lu,
        Ll,
        Lt,
        Lm,
        Lo,
        Mn,
        Mc,
        Me,
        Nd,
        Nl,
        No,
        Zs,
        Zl,
        Zp,
        Cc,
        Cf,
        Cs,
        Co,
        Pc,
        Pd,
        Ps,
        Pe,
        Pi,
        Pf,
        Po,
        Sm,
        Sc,
        Sk,
        So,
        Cn
    }

    public class NameAlias
    {
        [XmlAttribute("alias")]
        public string Alias { get; set; }
        [XmlAttribute("type")]
        public AliasType Type { get; set; }

        public override string ToString()
        {
            return Alias;
        }
    }

    public enum AliasType
    {
        abbreviation,
        alternate,
        control,
        correction,
        figment
    }

    public class Reserved : CodePointInfo { }
    public class NonCharacter : CodePointInfo { }
    public class Surrogate : CodePointInfo { }
    public class Character : CodePointInfo { }

    public class Block
    {
        [XmlIgnore]
        public int FirstCodePoint { get; set; }
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("first-cp")]
        public string FirstCodePointString
        {
            get { return FirstCodePoint.ToString("X4"); }
            set { FirstCodePoint = int.Parse(value, NumberStyles.HexNumber); }
        }
        [XmlIgnore]
        public int LastCodePoint { get; set; }
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("last-cp")]
        public string LastCodePointString
        {
            get { return LastCodePoint.ToString("X4"); }
            set { LastCodePoint = int.Parse(value, NumberStyles.HexNumber); }
        }
        [XmlAttribute("name")]
        public string Name { get; set; }

        public override string ToString()
        {
            return FirstCodePointString + "-" + LastCodePointString + " " + Name;
        }
    }
}
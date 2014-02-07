using System;
using System.Xml;
using System.Xml.Serialization;

namespace MMBot
{
    [XmlRoot("rss")]
    public class RssFeed
    {
        [XmlElement("channel")]
        public RssChannel Channel { get; set; }
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }
        [XmlAnyAttribute]
        public XmlAttribute[] Attributes { get; set; }
    }

    public class RssChannel
    {
        [XmlElement("title")]
        public string Title { get; set; }
        [XmlElement("link")]
        public string Link { get; set; }
        [XmlElement("item")]
        public RssFeedEntry[] Entries { get; set; }
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }
        [XmlAnyAttribute]
        public XmlAttribute[] Attributes { get; set; }
    }

    public class RssFeedEntry : IComparable<RssFeedEntry>
    {
        [XmlElement("link")]
        public string Link { get; set; }
        [XmlElement("title")]
        public string Title { get; set; }
        [XmlElement("creator", Namespace = "http://purl.org/dc/elements/1.1/")]
        public string Creator { get; set; }
        [XmlElement("author")]
        public string Author { get; set; }
        [XmlIgnore]
        public DateTime? PubDate { get; set; }
        [XmlElement("pubDate")]
        public string PubDateString
        {
            get { return PubDate.HasValue ? PubDate.Value.ToString("ddd, d MMM yyyy HH:mm:ss") : null; }
            set { PubDate = DateTime.ParseExact(value.Remove(value.LastIndexOf(' ')), "ddd, d MMM yyyy HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo); }
        }
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }
        [XmlAnyAttribute]
        public XmlAttribute[] Attributes { get; set; }

        public DateTime? GetPostTimestamp()
        {
            if (PubDate.HasValue)
                return PubDate.Value;
            else
                return null;
        }

        int IComparable<RssFeedEntry>.CompareTo(RssFeedEntry other)
        {
            return (GetPostTimestamp() ?? DateTime.MinValue).CompareTo(other.GetPostTimestamp() ?? DateTime.MinValue);
        }
    }

    [XmlRoot("feed", Namespace = "http://www.w3.org/2005/Atom")]
    public class AtomFeed
    {
        [XmlElement("title")]
        public AtomFeedTitle Title { get; set; }
        [XmlElement("id")]
        public string ID { get; set; }
        [XmlElement("entry")]
        public AtomFeedEntry[] Entries { get; set; }
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }
        [XmlAnyAttribute]
        public XmlAttribute[] Attributes { get; set; }
    }

    public class AtomFeedTitle
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        [XmlText]
        public string Text { get; set; }
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }
        [XmlAnyAttribute]
        public XmlAttribute[] Attributes { get; set; }
    }

    public class AtomFeedEntry : IComparable<AtomFeedEntry>
    {
        [XmlElement("title")]
        public AtomFeedTitle Title { get; set; }
        [XmlElement("author")]
        public AtomFeedAuthor Author { get; set; }
        [XmlElement("link")]
        public AtomFeedLink Link { get; set; }
        [XmlIgnore]
        public DateTime? Created { get; set; }
        [XmlElement("created")]
        public string CreatedString
        {
            get { return Created.HasValue ? Created.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : null; }
            set { Created = DateTime.Parse(value); }
        }
        [XmlIgnore]
        public DateTime? Published { get; set; }
        [XmlElement("published")]
        public string PublishedString
        {
            get { return Published.HasValue ? Published.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : null; }
            set { Published = DateTime.Parse(value); }
        }
        [XmlIgnore]
        public DateTime? Updated { get; set; }
        [XmlElement("updated")]
        public string UpdatedString
        {
            get { return Updated.HasValue ? Updated.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : null; }
            set { Updated = DateTime.Parse(value); }
        }
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }
        [XmlAnyAttribute]
        public XmlAttribute[] Attributes { get; set; }

        public DateTime? GetPostTimestamp()
        {
            if (Created.HasValue)
                return Created.Value;
            else if (Published.HasValue)
                return Published.Value;
            else if (Updated.HasValue)
                return Updated.Value;
            else
                return null;
        }

        int IComparable<AtomFeedEntry>.CompareTo(AtomFeedEntry other)
        {
            return (GetPostTimestamp() ?? DateTime.MinValue).CompareTo(other.GetPostTimestamp() ?? DateTime.MinValue);
        }
    }

    public class AtomFeedAuthor
    {
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }
        [XmlAnyAttribute]
        public XmlAttribute[] Attributes { get; set; }
    }

    public class AtomFeedLink
    {
        [XmlAttribute("type")]
        public string Type { get; set; }
        [XmlAttribute("href")]
        public string URL { get; set; }
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }
        [XmlAnyAttribute]
        public XmlAttribute[] Attributes { get; set; }
    }
}
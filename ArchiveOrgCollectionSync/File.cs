namespace ArchiveOrgCollectionSync
{
    using System.Xml.Serialization;

    public class File
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("size")]
        public long Size { get; set; }

        [XmlElement("md5")]
        public string Md5 { get; set; }
    }
}
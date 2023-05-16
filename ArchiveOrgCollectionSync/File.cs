namespace ArchiveOrgCollectionSync
{
    using System.Xml.Serialization;

    public class File
    {
        public File()
        {
            this.Name = string.Empty;
            this.Md5 = string.Empty;
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("size")]
        public long Size { get; set; }

        [XmlElement("md5")]
        public string Md5 { get; set; }
    }
}
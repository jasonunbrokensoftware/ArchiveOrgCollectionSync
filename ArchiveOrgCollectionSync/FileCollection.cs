namespace ArchiveOrgCollectionSync
{
    using System.Xml.Serialization;

    [XmlRoot("files")]
    public class FileCollection
    {
        [XmlElement("file")]
        public File[] Files { get; set; }
    }
}
namespace ArchiveOrgCollectionSync
{
    using System.Xml.Serialization;

    [XmlRootAttribute("files")]
    public class FileCollection
    {
        [XmlElement("file")]
        public File[] Files { get; set; }
    }
}
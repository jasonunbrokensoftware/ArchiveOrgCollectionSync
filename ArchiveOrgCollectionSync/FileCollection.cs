namespace ArchiveOrgCollectionSync
{
    using System;
    using System.Xml.Serialization;

    [XmlRoot("files")]
    public class FileCollection
    {
        public FileCollection()
        {
            this.Files = Array.Empty<File>();
        }

        [XmlElement("file")]
        public File[] Files { get; set; }
    }
}
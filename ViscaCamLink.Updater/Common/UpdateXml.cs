namespace ViscaCamLink.Updater.Common;

using System;
using System.Xml.Serialization;

[XmlRoot("item")]
public class UpdateXml
{
    [XmlElement("url")]
    public String? DownloadUrl { get; set; }

    [XmlElement("changelog")]
    public String? ChangelogUrl { get; set; }

    [XmlElement("version")]
    public String? Version { get; set; }

    [XmlElement("mandatory")]
    public MandatoryXml? Mandatory { get; set; }

    [XmlElement("executable")]
    public String? ExecutablePath { get; set; }

    [XmlElement("args")]
    public String? InstallerArgs { get; set; }
}

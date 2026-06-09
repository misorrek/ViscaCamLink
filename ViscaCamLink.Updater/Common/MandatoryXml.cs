namespace ViscaCamLink.Updater.Common;

using System;
using System.Xml.Serialization;

public class MandatoryXml
{
    [XmlText]
    public bool Value { get; set; }

    [XmlAttribute("minVersion")]
    public String? MinimumVersion { get; set; }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FirstKey.D365.Plug_Ins
{
    [XmlRoot("ProjectTemplateSettings")]
    public class ProjectTemplateSettings
    {
        [XmlArray("Mappings")]
        public List<Mapping> Mappings;
    }

    public class Mapping
    {
        public string Name;
        public string Key;
}
}

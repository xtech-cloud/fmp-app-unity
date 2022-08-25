using System.IO;
using System.Collections;
using System.Xml.Serialization;
using UnityEngine;
using System.Text;

public class DependencyConfig
{
    public class Field
    {
        [XmlAttribute("attribute")]
        public string attribute { get; set; } = "";

        [XmlAttribute("values")]
        public string values { get; set; } = "";
    }


    public class Options
    {
        [XmlAttribute("environment")]
        public string environment { get; set; } = "develop";
        [XmlAttribute("repository")]
        public string repository { get; set; } = "";
    }

    public class Reference
    {
        [XmlAttribute("org")]
        public string org { get; set; } = "";
        [XmlAttribute("module")]
        public string module { get; set; } = "";
        [XmlAttribute("version")]
        public string version { get; set; } = "";
    }

    public class Plugin
    {
        [XmlAttribute("name")]
        public string name { get; set; } = "";
        [XmlAttribute("version")]
        public string version { get; set; } = "";
    }

    public class Body
    {
        [XmlElement("Options")]
        public Options options = new Options();
        [XmlArray("References"), XmlArrayItem("Reference")]
        public Reference[] references { get; set; } = new Reference[0];
        [XmlArray("Plugins"), XmlArrayItem("Plugin")]
        public Plugin[] plugins { get; set; } = new Plugin[0];
    }

    public class Schema
    {
        [XmlElement("Body")]
        public Body body { get; set; } = new Body();

        [XmlArray("Header"), XmlArrayItem("Field")]
        public Field[] fields{ get; set; } = new Field[]
        {
            new Field 
            {
                attribute = "LogLevel.environment",
                values = "环境，可选值为：develop,product。使用develop时，所有依赖的version字段被强制替换为develop",
            }
        };
    }


    public static DependencyConfig Singleton { get; private set; } = new DependencyConfig();

    public Body body
    {
        get
        {
            return schema_.body;
        }
    }

    private Schema schema_;

    public IEnumerator Load()
    {
        UnityLogger.Singleton.Info("ready to load Dependency.xml ...");
        var storage = new XmlStorage<Schema>();
        yield return storage.Load(VendorManager.Singleton.active, "Dependency.xml");
        schema_ = storage.xml as Schema;
    }
}

using System.IO;
using System.Collections;
using System.Xml.Serialization;
using UnityEngine;
using System.Text;

public class AppConfig
{

    public class Field
    {
        [XmlAttribute("attribute")]
        public string attribute { get; set; } = "";

        [XmlAttribute("values")]
        public string values { get; set; } = "";
    }

    public class VendorSelector
    {
        [XmlAttribute("active")]
        public string active { get; set; } = "default";
    }

    public class Logger
    {
        [XmlAttribute("level")]
        public int level { get; set; } = 4; //info
    }

    public class Security
    {
        [XmlAttribute("sngen")]
        public int sngen { get; set; } = 1; // 0：使用自有算法生成sn, 1：使用Unity的算法

    }

    public class Body
    {
        [XmlElement("Logger")]
        public Logger logger { get; set; } = new Logger();

        [XmlElement("Security")]
        public Security security = new Security();

        [XmlElement("VendorSelector")]
        public VendorSelector vendorSelector = new VendorSelector();
    }

    public class Schema
    {
        [XmlElement("Body")]
        public Body body { get; set; } = new Body();

        [XmlArray("Header"), XmlArrayItem("Field")]
        public Field[] fields { get; set; } = new Field[]
        {
            new Field
            {
                attribute = "LogLevel.level",
                values = "日志等级，可选值为：0(NONE), 1(EXCEPTION), 2(ERROR), 3(WARNING), 4(INFO), 5(DEBUG)5, 6(TRACE), 7(ALL)",
            },
            new Field
            {
                attribute = "Vendor.Selector.active",
                values = "激活的虚拟环境的目录，如果没有激活的虚拟环境，会显示虚拟环境选择界面",
            }
        };
    }

    public Body body
    {
        get
        {
            return schema_.body;
        }
    }

    private static AppConfig singleton_ = null;
    private Schema schema_ = new Schema();

    public static AppConfig Singleton
    {
        get
        {
            if (null == singleton_)
                singleton_ = new AppConfig();
            return singleton_;
        }
    }

    public IEnumerator Load()
    {
        UnityLogger.Singleton.Info("ready to load AppConfig.xml ...");
        var storage = new XmlStorage();
        yield return storage.LoadFromRoot<Schema>("AppConfig.xml");
        schema_ = storage.xml as Schema;
    }
}

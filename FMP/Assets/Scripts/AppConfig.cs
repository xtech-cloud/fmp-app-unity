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

        [XmlArray("Vendors"), XmlArrayItem("Vendor")]

        public Vendor[] vendors = new Vendor[]
        {
            new Vendor()
            {
                scope = "default",
                graphics = new Graphics(),
            }
        };
    }

    public class Vendor
    {
        [XmlAttribute("scope")]
        public string scope { get; set; } = "";

        [XmlAttribute("display")]
        public string display { get; set; } = "Default";

        [XmlElement("Skin")]
        public Skin skin = new Skin();

        [XmlElement("Graphics")]
        public Graphics graphics { get; set; } = new Graphics();

    }

    public class ReferenceResolution
    {
        [XmlAttribute("width")]
        public int width = 1920;
        [XmlAttribute("height")]
        public int height = 1080;
        [XmlAttribute("match")]
        public float match = 1.0f;
    }

    public class Logger
    {
        [XmlAttribute("level")]
        public int level { get; set; } = 4; //info
    }

    public class Graphics
    {
        [XmlAttribute("fps")]
        public int fps { get; set; } = 60;

        [XmlAttribute("quality")]
        public int quality { get; set; } = 3; //High

        [XmlAttribute("pixelResolution")]
        public string pixelResolution { get; set; } = "auto";

        [XmlElement("ReferenceResolution")]
        public ReferenceResolution referenceResolution = new ReferenceResolution();
    }

    public class Skin
    {
        public class Splash
        {
            [XmlAttribute("background")]
            public string background { get; set; } = "";
            [XmlAttribute("slogan")]
            public string slogan { get; set; } = "";
        }

        public class Bootloader
        {
            [XmlAttribute("color")]
            public string color { get; set; } = "#FF7A00FF";
        }

        [XmlElement("Splash")]
        public Splash splash { get; set; } = new Splash();
    }

    public class Body
    {
        [XmlElement("Logger")]
        public Logger logger { get; set; } = new Logger();

        [XmlElement("VendorSelector")]
        public VendorSelector vendorSelector = new VendorSelector();
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
                attribute = "LogLevel.level",
                values = "日志等级，可选值为：0(NONE), 1(EXCEPTION), 2(ERROR), 3(WARNING), 4(INFO), 5(DEBUG)5, 6(TRACE), 7(ALL)",
            },
            new Field
            {
                attribute = "Vendor.Selector.active",
                values = "激活的虚拟环境的目录，如果没有激活的虚拟环境，会显示虚拟环境选择界面",
            },
            new Field
            {
                attribute = "Vendor.Graphices.pixelResolution",
                values = "像素分辨率，可选值为 auto（自动匹配ReferenceResolution）, 宽度x高度",
            },
            new Field
            {
                attribute = "Vendor.Graphics.ReferenceResolution.match",
                values = "参考分辨率的适配权重，可选值为：0(匹配宽度), 1(匹配高度)",
            },
            new Field
            {
                attribute = "Vendor.Graphics.ReferenceResolution.width",
                values = "参考分辨率的宽度，影响UI缩放",
            },
            new Field
            {
                attribute = "Vendor.Graphics.ReferenceResolution.height",
                values = "参考分辨率的高度，影响UI缩放",
            },
            new Field
            {
                attribute = "Vendor.Graphics.qulity",
                values = "画质等级：0(VeryLow), 1(Low), 2(Medium), 3(High), 4(VeryHigh) 5(Ultra)",
            },
            new Field
            {
                attribute = "Vendor.Skin.Splash.background",
                values = "过场界面的背景图片文件名，文件存放在vendor目录下",
            },
            new Field
            {
                attribute = "Vendor.Skin.Splash.slogan",
                values = "过场界面的标语图片文件名，文件存放在vendor目录下",
            },
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
        var storage = new XmlStorage<Schema>();
        yield return storage.Load(null, "AppConfig.xml");
        schema_ = storage.xml as Schema;
    }
}

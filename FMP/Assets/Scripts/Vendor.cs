using ConfigEntity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ConfigEntity
{
    public class FileSubEntity
    {
        public string path { get; set; }
        public string hash { get; set; }
        public ulong size { get; set; }
        public string url { get; set; }

    }

    public class FileSubEntityS
    {
        public FileSubEntity[] entityS { get; set; } = new FileSubEntity[0];
    }

    public class DependencyConfig
    {
        public class Field
        {
            [XmlAttribute("attribute")]
            public string attribute { get; set; } = "";

            [XmlAttribute("values")]
            public string values { get; set; } = "";
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
            public Field[] fields { get; set; } = new Field[]
            {
                    new Field {
                        attribute = "LogLevel.environment",
                        values = "环境，可选值为：develop,product。使用develop时，所有依赖的version字段被强制替换为develop",
                    }
            };
        }

        [XmlElement("Schema")]
        public Schema schema { get; set; } = new Schema();
    }

    public class BootloaderConfig
    {
        public class BootStep
        {
            [XmlAttribute("length")]
            public int length = 1;
            [XmlAttribute("org")]
            public string org = "";
            [XmlAttribute("module")]
            public string module = "";
        }

        public class Schema
        {
            [XmlArray("Steps"), XmlArrayItem("Step")]
            public BootStep[] steps { get; set; } = new BootStep[0];
        }

        [XmlElement("Schema")]
        public Schema schema { get; set; } = new Schema();
    }

    public class UpdateConfig
    {
        public class Schema
        {
            public class Field
            {
                [XmlAttribute("attribute")]
                public string attribute { get; set; } = "";

                [XmlAttribute("values")]
                public string values { get; set; } = "";
            }
            public class FrameworkUpdate
            {
                [XmlAttribute("strategy")]
                public string strategy { get; set; } = "skip";
                [XmlAttribute("environment")]
                public string environment { get; set; } = "develop";
                [XmlAttribute("repository")]
                public string repository { get; set; } = "";
            }

            public class AssetSyndication
            {
                [XmlAttribute("strategy")]
                public string strategy { get; set; } = "skip";
                [XmlAttribute("storage")]
                public string storage { get; set; } = "";
            }

            public class Body
            {
                [XmlElement("FrameworkUpdate")]
                public FrameworkUpdate frameworkUpdate { get; set; } = new FrameworkUpdate();
                [XmlElement("AssetSyndication")]
                public AssetSyndication assetSyndication { get; set; } = new AssetSyndication();
            }

            [XmlElement("Body")]
            public Body body { get; set; } = new Body();

            [XmlArray("Header"), XmlArrayItem("Field")]
            public Field[] fields { get; set; } = new Field[] {
                    new Field {
                        attribute = "Update.strategy",
                        values = "升级策略，可选值为：skip, auto, manual",
                    },
                };
        }

        [XmlElement("Schema")]
        public Schema schema { get; set; } = new Schema();
    }

    public class CatalogConfig
    {
        public class Section
        {
            /// <summary>
            /// 内容列表
            /// </summary>
            /// <remarks>
            /// 支持正则表达式
            /// </remarks>
            public string[] contentS { get; set; } = new string[0];
        }

        public Section[] sectionS { get; set; } = new Section[0];
    }

    public class VendorSchema
    {
        public string Uuid { get; set; } = "";
        public string Name { get; set; } = "";
        public string Display { get; set; } = "";
        public string SkinSplashBackground { get; set; } = "";
        public string SkinSplashSlogan { get; set; } = "";
        public int GraphicsFPS { get; set; }
        public int GraphicsQuality { get; set; }
        public string GraphicsPixelResolution { get; set; } = "";
        public int GraphicsReferenceResolutionWidth { get; set; }
        public int GraphicsReferenceResolutionHeight { get; set; }
        public float GraphicsReferenceResolutionMatch { get; set; }
        public string Application { get; set; } = "";
        public string DependencyConfig { get; set; } = "";
        public string BootloaderConfig { get; set; } = "";
        public string UpdateConfig { get; set; } = "";
        public Dictionary<string, string> ModuleConfigS = new Dictionary<string, string>();
        public Dictionary<string, string> ModuleCatalogS = new Dictionary<string, string>();
        public Dictionary<string, FileSubEntityS> ModuleThemeS = new Dictionary<string, FileSubEntityS>();
    }
}


public class Vendor
{
    public static Vendor Parse(byte[] _bytes)
    {
        UnityLogger.Singleton.Info("parse Vendor/meta.json ...");
        Vendor vendor = null;
        try
        {
            vendor = new Vendor();
            vendor.schema = JsonConvert.DeserializeObject<ConfigEntity.VendorSchema>(System.Text.Encoding.UTF8.GetString(_bytes));
            UnityLogger.Singleton.Info("parse BootloaderConfig ...");
            vendor.bootloaderConfig = vendor.parseXML<BootloaderConfig>(vendor.schema.BootloaderConfig);
            UnityLogger.Singleton.Info("parse DependencyConfig ...");
            vendor.dependencyConfig = vendor.parseXML<DependencyConfig>(vendor.schema.DependencyConfig);
            UnityLogger.Singleton.Info("parse UpgradeConfig ...");
            vendor.updateConfig = vendor.parseXML<UpdateConfig>(vendor.schema.UpdateConfig);
        }
        catch (System.Exception ex)
        {
            UnityLogger.Singleton.Error(ex.Message);
            UnityLogger.Singleton.Exception(ex);
        }
        return vendor;
    }



    public ConfigEntity.VendorSchema schema { get; private set; }
    public ConfigEntity.BootloaderConfig bootloaderConfig { get; private set; }
    public ConfigEntity.DependencyConfig dependencyConfig { get; private set; }
    public ConfigEntity.UpdateConfig updateConfig { get; private set; }

    private T parseXML<T>(string _base64) where T : class, new()
    {
        var xs = new XmlSerializer(typeof(T));
        byte[] bytes = Convert.FromBase64String(_base64);
        UnityLogger.Singleton.Info("the content is {0}", System.Text.Encoding.UTF8.GetString(bytes));
        T xml = null;
        using (MemoryStream reader = new MemoryStream(bytes))
        {
            xml = xs.Deserialize(reader) as T;
        }
        return xml;
    }
}

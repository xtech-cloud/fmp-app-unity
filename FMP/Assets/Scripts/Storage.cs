using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking;

public class Storage
{
    public enum Mode
    {
        Client,
        Browser,
    }

    public static Mode mode = Mode.Client;

    /// <summary>
    /// 存储根目录（绝对路径）
    /// </summary>
    public static string RootPath
    {
        get
        {
            if (Mode.Browser == mode)
                return BusinessBranch.Security.StorageAddress;
            else
                return Application.persistentDataPath;
        }
    }

    /// <summary>
    /// 虚拟环境路径（相对路径）
    /// </summary>
    public static string VendorDir
    {
        get
        {
            if (Mode.Browser == mode)
                return Path.Combine(BusinessBranch.Security.StorageVendorRootDir, VendorManager.Singleton.activeUuid);
            else
                return VendorManager.Singleton.activeUuid;
        }
    }

    /// <summary>
    /// 主题路径（绝对路径）
    /// </summary>
    public static string ThemesPath
    {
        get
        {
            string path = Path.Combine(RootPath, VendorDir);
            path = Path.Combine(path, "themes");
            return path;
        }
    }

    /// <summary>
    /// 资源路径（绝对路径）
    /// </summary>
    public static string AssetsPath
    {
        get
        {
            if (Mode.Browser == mode)
            {
                return Path.Combine(RootPath, BusinessBranch.Security.StorageAssloudRootDir);
            }
            else
            {
                string path = Path.Combine(RootPath, VendorDir);
                path = Path.Combine(path, "assets");
                return path;
            }
        }
    }

    public static string UpgradeCachePath
    {
        get
        {
            string path = Path.Combine(RootPath, VendorDir);
            return Path.Combine(path, ".upgrade");
        }
    }

    public long statusCode { get; protected set; } = 200;
    public string error { get; protected set; } = "";
    public byte[] bytes { get; protected set; } = null;

    public IEnumerator ReadBytesFromRoot(string _file)
    {
        string file = Path.Combine(RootPath, _file);
        yield return readBytes(file);
    }

    public IEnumerator ReadBytesFromVendor(string _file)
    {
        string vendorPath = Path.Combine(RootPath, VendorDir);
        string file = Path.Combine(vendorPath, _file);
        yield return readBytes(file);
    }

    /// <summary>
    /// 从存储根节点读取数据
    /// </summary>
    /// <param name="_file"></param>
    protected IEnumerator readBytes(string _file)
    {
        string file = _file.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        statusCode = 0;
        bytes = null;
        error = "";
        using (UnityWebRequest uwr = UnityWebRequest.Get(new Uri(file)))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                statusCode = uwr.responseCode;
                error = uwr.error;
                yield break;
            }
            statusCode = 200;
            bytes = uwr.downloadHandler.data;
        }
    }

    /// <summary>
    /// 从存储根节点读取数据
    /// </summary>
    /// <param name="_file"></param>
    protected IEnumerator writeBytes(string _file, byte[] _content)
    {
        string file = _file.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (file.StartsWith("http://") || file.StartsWith("https://"))
            yield break;
        File.WriteAllBytes(file, _content);
    }
}

public class XmlStorage : Storage
{
    public object xml { get; private set; } = null;

    public IEnumerator LoadFromRoot<T>(string _file) where T : new()
    {
        string file = Path.Combine(RootPath, _file);
        yield return load<T>(file);
    }
    public IEnumerator LoadFromVendor<T>(string _file) where T : new()
    {
        string vendorPath = Path.Combine(RootPath, VendorDir);
        string file = Path.Combine(vendorPath, _file);
        yield return load<T>(file);
    }

    private IEnumerator load<T>(string _file) where T : new()
    {
        var xs = new XmlSerializer(typeof(T));

        yield return readBytes(_file);
        // 如果文件读取失败，则创建默认的配置文件
        if (200 != statusCode)
        {
            xml = new T();
            UnityLogger.Singleton.Info(string.Format("load {0} failure", Path.GetFileName(_file)));
            UnityLogger.Singleton.Info("status code is {0}", statusCode);
            if (404 != statusCode)
            {
                UnityLogger.Singleton.Error(error);
            }
            UnityLogger.Singleton.Info(string.Format("Use default {0}", typeof(T)));
            MemoryStream writer = new MemoryStream();
            xs.Serialize(writer, xml);
            yield return writeBytes(_file, writer.ToArray());
        }
        else
        {
            // 文件读取成功，反序列化
            try
            {
                using (MemoryStream reader = new MemoryStream(bytes))
                {
                    xml = xs.Deserialize(reader);
                }
                UnityLogger.Singleton.Info(string.Format("load {0} success", Path.GetFileName(_file)));
            }
            catch (System.Exception ex)
            {
                // 反序列化异常，使用默认
                UnityLogger.Singleton.Exception(ex);
                UnityLogger.Singleton.Info(string.Format("Use default {0}", typeof(T)));
                xml = new T();
            }
        }
    }
}


public class SpriteStorage : Storage
{
    public Sprite sprite { get; private set; } = null;

    public IEnumerator LoadFromVendor(string _file)
    {

        string vendorPath = Path.Combine(RootPath, VendorDir);
        string file = Path.Combine(vendorPath, _file).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        statusCode = 0;
        sprite = null;
        error = "";
        using (UnityWebRequest uwr = UnityWebRequest.Get(new Uri(file)))
        {
            DownloadHandlerTexture handler = new DownloadHandlerTexture(true);
            uwr.downloadHandler = handler;
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                statusCode = uwr.responseCode;
                error = uwr.error;
                yield break;
            }
            statusCode = 200;
            Texture2D texture = handler.texture;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }
    }
}

public class ModuleStorage : Storage
{
    public Assembly assembly { get; private set; } = null;
    public string config { get; private set; } = null;
    public string catalog { get; private set; } = null;
    public GameObject uab { get; private set; } = null;

    public IEnumerator LoadConfigFromVendor(string _org, string _module, string _version)
    {
        statusCode = 0;
        error = "";
        config = null;

        string vendorPath = Path.Combine(RootPath, VendorDir);
        string address = Path.Combine(vendorPath, "configs");
        string file = Path.Combine(address, string.Format("{0}_{1}.xml", _org, _module)).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        using (UnityWebRequest uwr = UnityWebRequest.Get(new Uri(file)))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                statusCode = uwr.responseCode;
                error = uwr.error;
                yield break;
            }
            statusCode = 200;
            var data = uwr.downloadHandler.data;
            config = Encoding.UTF8.GetString(data);
        }
    }

    public IEnumerator LoadCatalogFromVendor(string _org, string _module, string _version)
    {
        statusCode = 0;
        error = "";
        catalog = null;

        string vendorPath = Path.Combine(RootPath, VendorDir);
        string address = Path.Combine(vendorPath, "catalogs");
        string file = Path.Combine(address, string.Format("{0}_{1}.json", _org, _module)).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        using (UnityWebRequest uwr = UnityWebRequest.Get(new Uri(file)))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                statusCode = uwr.responseCode;
                error = uwr.error;
                yield break;
            }
            statusCode = 200;
            var data = uwr.downloadHandler.data;
            catalog = Encoding.UTF8.GetString(data);
        }
    }

    public IEnumerator LoadPlugin(string _name, string _file, string _version)
    {
        statusCode = 0;
        error = "";
        assembly = null;

        yield return new WaitForEndOfFrame();
        if (Mode.Browser == mode)
        {
            yield return loadPluginFromWeb(_name, _file, _version);
        }
        else
        {
            loadPluginFromFile(_name, _file, _version);
        }
    }

    public IEnumerator LoadReference(string _org, string _module, string _file, string _version)
    {
        statusCode = 0;
        error = "";
        assembly = null;

        yield return new WaitForEndOfFrame();
        if (Mode.Browser == mode)
        {
            yield return loadReferenceFromWeb(_org, _module, _file, _version);
        }
        else
        {
            loadReferenceFromFile(_org, _module, _file, _version);
        }
    }

    public IEnumerator LoadUAB(string _org, string _module, string _version)
    {
        statusCode = 0;
        error = "";
        uab = null;

        string file = "";
        if (Storage.Mode.Browser == Storage.mode)
        {
            string version = _version;
            if (DependencyConfig.Singleton.body.options.environment.Equals("develop"))
                version = "develop";
            string address = DependencyConfig.Singleton.body.options.repository;
            address = Path.Combine(address, "modules");
            address = Path.Combine(address, _org);
            address = Path.Combine(address, string.Format("{0}@{1}", _module, version));
            file = Path.Combine(address, string.Format("{0}_{1}@{2}.uab", _org.ToLower(), _module.ToLower(), Constant.PlatformAlias))
                .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        else
        {
            string vendorPath = Path.Combine(RootPath, VendorDir);
            string address = Path.Combine(vendorPath, "uabs");
            file = Path.Combine(address, string.Format("{0}_{1}.uab", _org.ToLower(), _module.ToLower()))
                .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        using (UnityWebRequest uwr = UnityWebRequest.Get(new Uri(file)))
        {
            uwr.downloadHandler = new DownloadHandlerAssetBundle(file, 0);
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                statusCode = uwr.responseCode;
                error = uwr.error;
                yield break;
            }
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
            if (null == bundle)
            {
                statusCode = 500;
                error = "bundle is null";
                yield break;
            }

            // 从包中异步加载根对象
            var alr = bundle.LoadAssetAsync<GameObject>("[ExportRoot]");
            yield return alr;
            var go = alr.asset as GameObject;
            if (null == go)
            {
                statusCode = 500;
                error = "gameobject is null";
                yield break;
            }
            go.SetActive(false);

            // 实例化根对象
            uab = GameObject.Instantiate<GameObject>(go);
            statusCode = 200;

            // 清理资源
            bundle.Unload(false);
            Resources.UnloadUnusedAssets();
        }
    }


    private void loadPluginFromFile(string _name, string _file, string _version)
    {

        string vendorPath = Path.Combine(RootPath, VendorDir);
        string address = Path.Combine(vendorPath, "modules");
        string file = Path.Combine(address, _file);

        try
        {
            assembly = Assembly.LoadFile(file);
        }
        catch (Exception ex)
        {
            statusCode = 500;
            error = ex.Message;
        }
    }

    private IEnumerator loadPluginFromWeb(string _name, string _file, string _version)
    {
        string version = _version;
        if (DependencyConfig.Singleton.body.options.environment.Equals("develop"))
            version = "develop";
        string address = DependencyConfig.Singleton.body.options.repository;
        address = Path.Combine(address, "plugins");
        address = Path.Combine(address, string.Format("{0}@{1}", _name, version));
        string file = Path.Combine(address, _file).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        using (UnityWebRequest uwr = UnityWebRequest.Get(new Uri(file)))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                statusCode = uwr.responseCode;
                error = uwr.error;
                yield break;
            }
            statusCode = 200;
            var data = uwr.downloadHandler.data;
            assembly = Assembly.Load(data);
        }
    }

    private void loadReferenceFromFile(string _org, string _module, string _file, string _version)
    {

        string vendorPath = Path.Combine(RootPath, VendorDir);
        string address = Path.Combine(vendorPath, "modules");
        string file = Path.Combine(address, _file);

        try
        {
            assembly = Assembly.LoadFile(file);
        }
        catch (Exception ex)
        {
            statusCode = 500;
            error = ex.Message;
        }
    }

    private IEnumerator loadReferenceFromWeb(string _org, string _module, string _file, string _version)
    {
        string version = _version;
        if (DependencyConfig.Singleton.body.options.environment.Equals("develop"))
            version = "develop";
        string address = DependencyConfig.Singleton.body.options.repository;
        address = Path.Combine(address, "modules");
        address = Path.Combine(address, _org);
        address = Path.Combine(address, string.Format("{0}@{1}", _module, version));
        string file = Path.Combine(address, _file).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        using (UnityWebRequest uwr = UnityWebRequest.Get(new Uri(file)))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                statusCode = uwr.responseCode;
                error = uwr.error;
                yield break;
            }
            statusCode = 200;
            var data = uwr.downloadHandler.data;
            assembly = Assembly.Load(data);
        }

    }

}

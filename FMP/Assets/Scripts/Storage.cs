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

    public static string ScopePath
    {
        get
        {
            if (Mode.Browser == mode)
                return "http://localhost:9000/fmp.vendor/CD_3rdKindergarten";
            else
                return Application.persistentDataPath;
        }
    }

    public static string VendorPath
    {
        get
        {
            return Path.Combine(ScopePath, VendorManager.Singleton.active);
        }
    }

    public static string UpgradeCachePath
    {
        get
        {
            return Path.Combine(VendorPath, ".upgrade");
        }
    }

    public long statusCode { get; protected set; } = 200;
    public string error { get; protected set; } = "";
    public byte[] bytes { get; protected set; } = null;

    public IEnumerator ReadBytes(string _vendor, string _file)
    {
        string address = ScopePath;
        if (!string.IsNullOrEmpty(_vendor))
            address = Path.Combine(address, _vendor);
        string file = Path.Combine(address, _file);

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

    public IEnumerator WriteBytes(string _vendor, string _file, byte[] _content)
    {
        string address = ScopePath;
        if (!string.IsNullOrEmpty(_vendor))
            address = Path.Combine(address, _vendor);
        string file = Path.Combine(address, _file);

        if (file.StartsWith("http://") || file.StartsWith("https://"))
            yield break;
        File.WriteAllBytes(file, _content);
    }
}

public class XmlStorage<T> where T : new()
{
    public object xml { get; private set; } = null;

    public IEnumerator Load(string _vendor, string _file)
    {
        Storage storage = new Storage();
        var xs = new XmlSerializer(typeof(T));

        yield return storage.ReadBytes(_vendor, _file);
        // 如果文件读取失败，则创建默认的配置文件
        if (200 != storage.statusCode)
        {
            xml = new T();
            UnityLogger.Singleton.Info(string.Format("load {0} failure", Path.GetFileName(_file)));
            UnityLogger.Singleton.Info("status code is {0}", storage.statusCode);
            if (404 != storage.statusCode)
            {
                UnityLogger.Singleton.Error(storage.error);
            }
            UnityLogger.Singleton.Info(string.Format("Use default {0}", typeof(T)));
            MemoryStream writer = new MemoryStream();
            xs.Serialize(writer, xml);
            yield return storage.WriteBytes(_vendor, _file, writer.ToArray());
        }
        else
        {
            // 文件读取成功，反序列化
            try
            {
                using (MemoryStream reader = new MemoryStream(storage.bytes))
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

    public IEnumerator Load(string _vendor, string _file)
    {
        string address = ScopePath;
        if (!string.IsNullOrEmpty(_vendor))
            address = Path.Combine(address, _vendor);
        string file = Path.Combine(address, _file);

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

    public IEnumerator LoadConfig(string _org, string _module, string _version)
    {
        statusCode = 0;
        error = "";
        config = null;

        yield return new WaitForEndOfFrame();
        if (Mode.Browser == mode)
        {
            yield return loadConfigFromWeb(_org, _module, _version);
        }
        else
        {
            yield return loadConfigFromFile(_org, _module, _version);
        }
    }

    public IEnumerator LoadCatalog(string _org, string _module, string _version)
    {
        statusCode = 0;
        error = "";
        catalog = null;

        yield return new WaitForEndOfFrame();
        if (Mode.Browser == mode)
        {
            yield return loadCatalogFromWeb(_org, _module, _version);
        }
        else
        {
            yield return loadCatalogFromFile(_org, _module, _version);
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

        yield return new WaitForEndOfFrame();
        if (Mode.Browser == mode)
        {
            yield return loadUABFromWeb(_org, _module, _version);
        }
        else
        {
            yield return loadUABFromFile(_org, _module, _version);
        }
    }


    private IEnumerator loadConfigFromFile(string _org, string _module, string _version)
    {
        string address = Path.Combine(VendorPath, "configs");
        string file = Path.Combine(address, string.Format("{0}_{1}.xml", _org, _module));

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

    private IEnumerator loadConfigFromWeb(string _org, string _module, string _version)
    {
        string address = Path.Combine(VendorPath, "configs");
        string file = Path.Combine(address, string.Format("{0}_{1}.xml", _org, _module));

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
    private IEnumerator loadCatalogFromFile(string _org, string _module, string _version)
    {
        string address = Path.Combine(VendorPath, "catalogs");
        string file = Path.Combine(address, string.Format("{0}_{1}.json", _org, _module));

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

    private IEnumerator loadCatalogFromWeb(string _org, string _module, string _version)
    {
        string address = Path.Combine(VendorPath, "catalogs");
        string file = Path.Combine(address, string.Format("{0}_{1}.json", _org, _module));

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

    private void loadPluginFromFile(string _name, string _file, string _version)
    {
        string address = Path.Combine(VendorPath, "modules");
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
        string file = Path.Combine(address, _file);
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
        string address = Path.Combine(VendorPath, "modules");
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
        string file = Path.Combine(address, _file);
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

    private IEnumerator loadUABFromFile(string _org, string _module, string _version)
    {
        string address = Path.Combine(VendorPath, "uabs");
        string file = Path.Combine(address, string.Format("{0}_{1}.uab", _org.ToLower(), _module.ToLower()));

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

    private IEnumerator loadUABFromWeb(string _org, string _module, string _version)
    {
        string version = _version;
        if (DependencyConfig.Singleton.body.options.environment.Equals("develop"))
            version = "develop";
        string address = DependencyConfig.Singleton.body.options.repository;
        address = Path.Combine(address, "modules");
        address = Path.Combine(address, _org);
        address = Path.Combine(address, string.Format("{0}@{1}", _module, version));
        string file = Path.Combine(address, string.Format("{0}_{1}@{2}.uab", _org.ToLower(), _module.ToLower(), Constant.Platform));

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


}

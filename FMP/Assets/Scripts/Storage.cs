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
    public long statusCode { get; protected set; } = 200;
    public string error { get; protected set; } = "";
    public byte[] bytes { get; protected set; } = null;

    public IEnumerator ReadBytes(string _vendor, string _file)
    {
        string address = Constant.DataPath;
        if (!string.IsNullOrEmpty(_vendor))
            address = Path.Combine(address, _vendor);
        string file = Path.Combine(address, _file);

        statusCode = 0;
        bytes = null;
        error = "";
        using (UnityWebRequest uwr = UnityWebRequest.Get(file))
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
        string address = Constant.DataPath;
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
        string address = Constant.DataPath;
        if (!string.IsNullOrEmpty(_vendor))
            address = Path.Combine(address, _vendor);
        string file = Path.Combine(address, _file);

        statusCode = 0;
        sprite = null;
        error = "";
        using (UnityWebRequest uwr = UnityWebRequest.Get(file))
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

    public IEnumerator LoadConfig(string _vendor, string _org, string _module, string _version)
    {
        statusCode = 0;
        error = "";
        config = null;

        yield return new WaitForEndOfFrame();
        if (RuntimePlatform.WebGLPlayer == Application.platform)
        {
            throw new NotImplementedException();
            yield break;
        }

        string address = Constant.DataPath;
        if (!string.IsNullOrEmpty(_vendor))
            address = Path.Combine(address, _vendor);
        address = Path.Combine(address, "configs");
        string file = Path.Combine(address, string.Format("{0}_{1}.xml", _org, _module));

        using (UnityWebRequest uwr = UnityWebRequest.Get(file))
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

    public IEnumerator LoadAssembly(string _vendor, string _file, string _version)
    {
        statusCode = 0;
        error = "";
        assembly = null;

        yield return new WaitForEndOfFrame();
        if (RuntimePlatform.WebGLPlayer == Application.platform)
        {
            throw new NotImplementedException();
            yield break;
        }

        string address = Constant.DataPath;
        if (!string.IsNullOrEmpty(_vendor))
            address = Path.Combine(address, _vendor);
        address = Path.Combine(address, "modules");
        string file = Path.Combine(address, _file);

        try
        {
            assembly = Assembly.LoadFile(file);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
    }

}

using Newtonsoft.Json;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class VendorManager
{
    public static VendorManager Singleton
    {
        get
        {
            if (null == singleton_)
                singleton_ = new VendorManager();
            return singleton_;
        }
    }

    public IEnumerator Activate(string _vendorUuid)
    {
        if (string.IsNullOrEmpty(_vendorUuid))
            yield break;
        activeUuid = _vendorUuid;

        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            yield return readMetaUrlFile(_vendorUuid);
        }

        Storage storage = new Storage();
        UnityLogger.Singleton.Info("ready active vendor:{0}", _vendorUuid);
        yield return storage.ReadBytesFromVendor("meta.json");
        if (!string.IsNullOrEmpty(storage.error))
        {
            UnityLogger.Singleton.Error(storage.error);
            yield break;
        }
        try
        {
            active = Vendor.Parse(storage.bytes);
        }
        catch (Exception ex)
        {
            UnityLogger.Singleton.Exception(ex);
        }
    }

    public string activeUuid { get; private set; }
    public Vendor active { get; private set; }

    private static VendorManager singleton_;

    private IEnumerator readMetaUrlFile(string _vendorUuid)
    {
        Storage storage = new Storage();
        UnityLogger.Singleton.Info("read vendor({0})/url.txt", _vendorUuid);
        yield return storage.ReadBytesFromVendor("url.txt");
        if (!string.IsNullOrEmpty(storage.error))
        {
            UnityLogger.Singleton.Info("ignore read url.txt for there are some errors: {0}", storage.error);
            yield break;
        }
        string url = "";
        try
        {
            url = System.Text.Encoding.UTF8.GetString(storage.bytes).Trim();
        }
        catch (Exception ex)
        {
            UnityLogger.Singleton.Exception(ex);
        }

        UnityLogger.Singleton.Info("download meta.json from {0}", url);
        using (UnityWebRequest uwr = UnityWebRequest.Get(new Uri(url)))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                UnityLogger.Singleton.Error(uwr.error);
                yield break;
            }
            byte[] bytes = uwr.downloadHandler.data;
            storage.WriteBytesToVendor("meta.json", bytes);
        }

    }

}

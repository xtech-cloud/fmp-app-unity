using Newtonsoft.Json;
using System;
using System.Collections;

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
}

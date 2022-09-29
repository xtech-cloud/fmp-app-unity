using System.IO;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using MVCS = XTC.FMP.LIB.MVCS;
using Newtonsoft.Json;

public class StartupBehaviour : MonoBehaviour
{
    public class MVCSConfig : MVCS.Config
    {
        public MVCSConfig(Dictionary<string, string> _configs)
        {
            foreach (var pair in _configs)
            {

                fields_[pair.Key] = MVCS.Any.FromString(pair.Value);
            }
        }
    }

    public Transform mainCanvas;
    public GameObject bootloader;
    public Text textBootloaderTip;
    public Text textBootloaderUpgress;
    public Font mainFont;
    public TextAsset startupTip;

    private MVCS.Framework framework;
    private ModuleManager moduleManager;
    private bool isReady_ = false;
    private Dictionary<string, string> uiTip_;

    void Awake()
    {
        UnityLogger.Singleton.Info("########### Enter Startup Scene");
        uiTip_ = JsonConvert.DeserializeObject<Dictionary<string, string>>(startupTip.text);
        bootloader.SetActive(true);
        textBootloaderTip.text = "";
        textBootloaderUpgress.text = "";

        Vendor activeVendor = VendorManager.Singleton.active;

        var canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
        canvasScaler.referenceResolution = new Vector2(
            activeVendor.GraphicsReferenceResolutionWidth,
            activeVendor.GraphicsReferenceResolutionHeight
        );
        canvasScaler.matchWidthOrHeight = activeVendor.GraphicsReferenceResolutionMatch;

        moduleManager = new ModuleManager();
        moduleManager.OnTipChanged = (_category, _tip) => textBootloaderTip.text = string.Format(uiTip_[_category], _tip);
        moduleManager.OnProgressChanged = (_percentage) => textBootloaderUpgress.text = _percentage.ToString();
        moduleManager.OnBootFinish = () => bootloader.SetActive(false);
    }

    IEnumerator Start()
    {
        UnityLogger.Singleton.Info("---------------  Start ------------------------");

        // 加载模块
        yield return moduleManager.Load();
        if (!moduleManager.success)
            yield break;

        // 初始化MVCS框架
        MVCS.Config config = new MVCSConfig(moduleManager.configs);
        framework = new MVCS.Framework();
        framework.setLogger(UnityLogger.Singleton);
        framework.setConfig(config);
        framework.Initialize();

        // 处理Vendor路径
        string vendorDir = Storage.VendorDir;
        int pos = vendorDir.IndexOf(VendorManager.Singleton.activeUuid);
        if(pos >=0)
        {
            vendorDir = vendorDir.Substring(0, pos);
        }
        string vendorRootPath = Path.Combine(Storage.RootPath, vendorDir).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if(vendorRootPath.EndsWith("/"))
            vendorRootPath = vendorRootPath.Substring(0, vendorRootPath.Length - 1);
        // 加载模块
        Dictionary<string, MVCS.Any> settings = new Dictionary<string, MVCS.Any>();
        settings["vendor"] = MVCS.Any.FromString(VendorManager.Singleton.activeUuid);
        settings["datapath"] = MVCS.Any.FromString(vendorRootPath);
        settings["devicecode"] = MVCS.Any.FromString(Constant.DeviceCode);
        settings["platform"] = MVCS.Any.FromString(Constant.PlatformAlias);
        settings["canvas.main"] = MVCS.Any.FromObject(mainCanvas);
        settings["font.main"] = MVCS.Any.FromObject(mainFont);
        foreach (var pair in moduleManager.uabs)
        {
            settings[string.Format("uab.{0}", pair.Key)] = MVCS.Any.FromObject(pair.Value);
        }
        // 注册模块中的MVCS
        moduleManager.Inject(this, framework, UnityLogger.Singleton, config, settings);
        moduleManager.Register();

        // 装载已注册的部件
        framework.Setup();

        isReady_ = true;
        yield return new WaitForEndOfFrame();
        UnityLogger.Singleton.Info("++++++++++++++++++++++++++++++++++++++++++++++++");
        UnityLogger.Singleton.Info("+ Preload Modules                              +");
        UnityLogger.Singleton.Info("++++++++++++++++++++++++++++++++++++++++++++++++");
        moduleManager.Preload();
    }

    void Update()
    {
    }

    void OnDestroy()
    {
        UnityLogger.Singleton.Info("---------------  OnDestroy ------------------------");

        if (!isReady_)
            return;
        framework.Dismantle();
        // 拆卸模块中的MVCS
        moduleManager.Cancel();
        framework.Release();
        framework = null;

        moduleManager.Unload();
    }
}

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
    public Transform mainWorld;
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
            activeVendor.schema.GraphicsReferenceResolutionWidth,
            activeVendor.schema.GraphicsReferenceResolutionHeight
        );
        canvasScaler.matchWidthOrHeight = activeVendor.schema.GraphicsReferenceResolutionMatch;

        moduleManager = ModuleManager.Singleton;
        moduleManager.OnTipChanged = (_category, _tip) => textBootloaderTip.text = string.Format(uiTip_[_category], _tip);
        moduleManager.OnProgressChanged = (_percentage) => textBootloaderUpgress.text = _percentage.ToString();
        moduleManager.OnBootFinish = () => bootloader.SetActive(false);

        if(RuntimePlatform.WebGLPlayer == Constant.Platform)
        {
            var go = Resources.Load<GameObject>("__ModulesExport__");
            if(null != go)
            {
                GameObject.Instantiate(go);
            }
        }
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

        // 加载模块
        string serialnumber = string.IsNullOrEmpty(AppConfig.Singleton.body.security.serialnumber) ? Constant.DeviceCode : AppConfig.Singleton.body.security.serialnumber;
        UnityLogger.Singleton.Error(serialnumber);
        Dictionary<string, MVCS.Any> settings = new Dictionary<string, MVCS.Any>();
        settings["path.themes"] = MVCS.Any.FromString(Storage.ThemesPath);
        settings["path.assets"] = MVCS.Any.FromString(Storage.AssetsPath);
        settings["devicecode"] = MVCS.Any.FromString(Constant.DeviceCode);
        settings["serialnumber"] = MVCS.Any.FromString(serialnumber);
        settings["platform"] = MVCS.Any.FromString(Constant.PlatformAlias);
        settings["canvas.main"] = MVCS.Any.FromObject(mainCanvas);
        settings["world.main"] = MVCS.Any.FromObject(mainWorld);
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

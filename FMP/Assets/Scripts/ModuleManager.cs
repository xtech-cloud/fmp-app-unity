using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using MVCS = XTC.FMP.LIB.MVCS;

public class ModuleManager
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

    public class Bootloader
    {
        [XmlArray("Steps"), XmlArrayItem("Step")]
        public BootStep[] steps { get; set; } = new BootStep[0];
    }

    public class Module
    {
        public Assembly assembly { get; private set; }
        public object entryInstance { get; private set; }
        public Type entryClass { get; private set; }
        public string namespacePrefix { get; private set; }
        public Module(Assembly _assembly, object _entryInstance, Type _entryClass, string _namespacePrefix)
        {
            assembly = _assembly;
            entryInstance = _entryInstance;
            entryClass = _entryClass;
            namespacePrefix = _namespacePrefix;
        }
    }

    public Action<int> OnProgressChanged { get; set; }
    public Action<string, string> OnTipChanged { get; set; }
    public Action OnBootFinish { get; set; }

    public Dictionary<string, string> configs { get; private set; } = new Dictionary<string, string>();
    public Dictionary<string, GameObject> uabs { get; private set; } = new Dictionary<string, GameObject>();
    public bool success { get; private set; } = true;

    private Dictionary<string, Module> modules_ = new Dictionary<string, Module>();
    private Dictionary<string, Assembly> assemblies_ = new Dictionary<string, Assembly>();
    private Bootloader bootloader_ = new Bootloader();
    private int currentBootStep_ = 0;
    // 引导的总长度
    private int totalBootLength_ = 0;
    // 完成的引导步的长度
    private int finishedBootLength_ = 0;
    private Action<int> onBootStepProgress_;
    private Action<string> onBootStepFinish_;

    private static ModuleManager singleton_ = null;

    public static ModuleManager Singleton
    {
        get
        {
            if (null == singleton_)
                singleton_ = new ModuleManager();
            return singleton_;
        }
    }

    public ModuleManager()
    {
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(assemblyResolve);
        onBootStepProgress_ = this.handleBootStepProgress;
        onBootStepFinish_ = this.handleBootStepFinish;
    }

    /// <summary>
    /// 不能使用运行时加载程序集时，使用此方法注册工程中的程序集
    /// </summary>
    public void ExportModule(Assembly _assembly, string _org, string _module)
    {
        string namespacePrefix = string.Format("{0}.FMP.MOD.{1}", _org, _module);
        string assemblyFile = namespacePrefix + ".LIB.Unity.dll";
        string entryClassName = string.Format("{0}.LIB.Unity.MyEntry", namespacePrefix);
        UnityLogger.Singleton.Info("Create Instance of {0}", entryClassName);
        try
        {
            object instanceEntry = _assembly.CreateInstance(entryClassName);
            Type entryClass = _assembly.GetType(entryClassName);
            Module module = new Module(_assembly, instanceEntry, entryClass, namespacePrefix);
            modules_[assemblyFile] = module;
        }
        catch (Exception ex)
        {
            UnityLogger.Singleton.Exception(ex);
            success = false;
        }
    }

    public IEnumerator Load()
    {
        // 加载bootloader
        var storage = new XmlStorage();
        UnityLogger.Singleton.Info("load Bootloader.xml ...");
        yield return storage.LoadFromVendor<Bootloader>("Bootloader.xml");
        bootloader_ = storage.xml as Bootloader;

        totalBootLength_ = 0;
        finishedBootLength_ = 0;
        foreach (var dependency in DependencyConfig.Singleton.body.references)
        {
            // config, catalog, uab, plugin, reference 
            totalBootLength_ += 5;
        }
        foreach (var step in bootloader_.steps)
        {
            totalBootLength_ += step.length;
        }

        // 加载所有模块的配置文件
        // 配置文件在这里统一加载，而不由模块自己加载，是为了在模块的Unity工程中，能将配置文件放置于编译外的代码中灵活处理。
        yield return loadConfigs();
        if (!success)
            yield break;
        yield return loadCatalogs();
        if (!success)
            yield break;
        yield return loadAssetBundles();
        if (!success)
            yield break;
        yield return loadDependencies();
        if (!success)
            yield break;

        success = true;
    }

    public void Unload()
    {
        //TODO 加载的assembly无法卸载，需要重启Unity编辑器
        assemblies_.Clear();
        modules_.Clear();
        GC.Collect();
    }

    public void Inject(MonoBehaviour _mono, MVCS.Framework _framework, MVCS.Logger _logger, MVCS.Config _config, Dictionary<string, MVCS.Any> _settings)
    {
        foreach (var module in modules_.Values)
        {
            MethodInfo miNewOptions = module.entryClass.GetMethod("NewOptions");
            var options = miNewOptions.Invoke(module.entryInstance, new object[] { });
            MethodInfo miInject = module.entryClass.GetMethod("Inject");
            miInject.Invoke(module.entryInstance, new object[] { _framework, options });
            MethodInfo miUniInject = module.entryClass.GetMethod("UniInject");
            miUniInject.Invoke(module.entryInstance, new object[] { _mono, options, _logger, _config, _settings });
            MVCS.UserData entry = module.entryInstance as MVCS.UserData;
            _framework.setUserData(module.namespacePrefix + ".LIB.MVCS.Entry", entry);
        }
    }

    public void Register()
    {
        foreach (var module in modules_.Values)
        {
            MethodInfo miRegister = module.entryClass.GetMethod("RegisterDummy");
            miRegister.Invoke(module.entryInstance, null);
        }
    }

    public void Preload()
    {
        UnityLogger.Singleton.Info("read to boot {0} steps", bootloader_.steps.Length);
        executeNextBootStep();
    }

    public void Cancel()
    {
        foreach (var module in modules_.Values)
        {
            MethodInfo miCancel = module.entryClass.GetMethod("CancelDummy");
            miCancel.Invoke(module.entryInstance, null);
        }
    }

    private Assembly assemblyResolve(object sender, ResolveEventArgs args)
    {
        //Debug.Log(args.Name);
        return assemblies_[args.Name];
    }

    private IEnumerator loadConfigs()
    {
        var storage = new ModuleStorage();
        foreach (var reference in DependencyConfig.Singleton.body.references)
        {
            UnityLogger.Singleton.Info("load config of {0}_{1}", reference.org, reference.module);
            yield return storage.LoadConfigFromVendor(reference.org, reference.module, reference.version);
            if (200 != storage.statusCode)
            {
                UnityLogger.Singleton.Error(storage.error);
                success = false;
                yield break;
            }
            UnityLogger.Singleton.Trace("load config of {0}_{1} success", reference.org, reference.module);
            configs[string.Format("{0}_{1}.xml", reference.org, reference.module)] = storage.config;
            finishedBootLength_ += 1;
            updateProgress();
            OnTipChanged("config", string.Format("{0}_{1}", reference.org, reference.module));
        }
    }

    private IEnumerator loadCatalogs()
    {
        var storage = new ModuleStorage();
        foreach (var reference in DependencyConfig.Singleton.body.references)
        {
            UnityLogger.Singleton.Info("load catalog of {0}_{1}", reference.org, reference.module);
            yield return storage.LoadCatalogFromVendor(reference.org, reference.module, reference.version);
            if (200 != storage.statusCode)
            {
                UnityLogger.Singleton.Error(storage.error);
                success = false;
                yield break;
            }
            UnityLogger.Singleton.Trace("load catalog of {0}_{1} success", reference.org, reference.module);
            configs[string.Format("{0}_{1}.json", reference.org, reference.module)] = storage.catalog;
            finishedBootLength_ += 1;
            updateProgress();
            OnTipChanged("catalog", string.Format("{0}_{1}", reference.org, reference.module));
        }
    }


    private IEnumerator loadAssetBundles()
    {
        var storage = new ModuleStorage();
        foreach (var reference in DependencyConfig.Singleton.body.references)
        {
            UnityLogger.Singleton.Info("load uab of {0}_{1}", reference.org, reference.module);
            yield return storage.LoadUAB(reference.org, reference.module, reference.version);
            if (200 != storage.statusCode)
            {
                UnityLogger.Singleton.Error(storage.error);
                success = false;
                yield break;
            }
            UnityLogger.Singleton.Trace("load uab of {0}_{1} success", reference.org, reference.module);
            uabs[string.Format("{0}_{1}", reference.org, reference.module)] = storage.uab;
            finishedBootLength_ += 1;
            updateProgress();
            OnTipChanged("uab", string.Format("{0}_{1}", reference.org, reference.module));
        }
    }


    private IEnumerator loadDependencies()
    {
        // 先加载plugin
        foreach (var plugin in DependencyConfig.Singleton.body.plugins)
        {
            yield return loadPlugin(plugin.name, plugin.version);
            if (!success)
                yield break;

            finishedBootLength_ += 1;
            updateProgress();
            OnTipChanged("plugin", string.Format("{0}", plugin.name));
        }

        // 再加载reference
        foreach (var reference in DependencyConfig.Singleton.body.references)
        {
            yield return loadReference(reference.org, reference.module, reference.version);
            if (!success)
                yield break;

            finishedBootLength_ += 1;
            updateProgress();
            OnTipChanged("reference", string.Format("{0}_{1}", reference.org, reference.module));
        }

        UnityLogger.Singleton.Info("finally load {0} modules", modules_.Count);
    }

    private IEnumerator loadPlugin(string _name, string _version)
    {
        if (RuntimePlatform.WebGLPlayer == Constant.Platform)
        {
            //WASM 不支持运行时加载程序集
            UnityLogger.Singleton.Warning("WASM not support load assembly at runtime, so ignore this operation, make sure all assemblies included at build stage.");
            success = true;
            yield break;
        }

        string file = _name + ".dll";
        var storage = new ModuleStorage();
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadPlugin(_name, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }

        string filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;
    }

    private IEnumerator loadReference(string _org, string _module, string _version)
    {
        if (RuntimePlatform.WebGLPlayer == Constant.Platform)
        {
            //WASM 不支持运行时加载程序集
            UnityLogger.Singleton.Warning("WASM not support load assembly at runtime, so ignore this operation, make sure all assemblies included at build stage.");

            success = true;
            yield break;
        }

        var storage = new ModuleStorage();

        // proto.dll
        string file = string.Format("fmp-{0}-{1}-lib-proto.dll", _org.ToLower(), _module.ToLower());
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadReference(_org, _module, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }
        string filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;

        // bridge.dll
        file = string.Format("fmp-{0}-{1}-lib-bridge.dll", _org.ToLower(), _module.ToLower());
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadReference(_org, _module, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }
        filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;

        // mvcs.dll
        file = string.Format("fmp-{0}-{1}-lib-mvcs.dll", _org.ToLower(), _module.ToLower());
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadReference(_org, _module, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }
        filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;

        // Unity.dll
        file = string.Format("{0}.FMP.MOD.{1}.LIB.Unity.dll", _org, _module);
        UnityLogger.Singleton.Info("load {0}", file);
        yield return storage.LoadReference(_org, _module, file, _version);
        if (null == storage.assembly)
        {
            UnityLogger.Singleton.Error(storage.error);
            UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
            success = false;
            yield break;
        }
        filename = Path.GetFileName(file);
        assemblies_[filename] = storage.assembly;

        string namespacePrefix = string.Format("{0}.FMP.MOD.{1}", _org, _module);
        string entryClassName = string.Format("{0}.LIB.Unity.MyEntry", namespacePrefix);
        UnityLogger.Singleton.Info("Create Instance of {0}", entryClassName);
        try
        {
            object instanceEntry = storage.assembly.CreateInstance(entryClassName);
            Type entryClass = storage.assembly.GetType(entryClassName);
            Module module = new Module(storage.assembly, instanceEntry, entryClass, namespacePrefix);
            modules_[filename] = module;
        }
        catch (Exception ex)
        {
            UnityLogger.Singleton.Exception(ex);
            success = false;
        }
    }

    private void executeNextBootStep()
    {
        if (currentBootStep_ >= bootloader_.steps.Length)
        {
            UnityLogger.Singleton.Info("All steps are finished");
            OnBootFinish();
            return;
        }

        BootStep step = bootloader_.steps[currentBootStep_];
        string moduleFile = string.Format("{0}.FMP.MOD.{1}.LIB.Unity.dll", step.org, step.module);
        UnityLogger.Singleton.Info("Boot the step {0}_{1}", moduleFile, step.org, step.module);
        OnTipChanged("boot", string.Format("{0}_{1}", step.org, step.module));

        Module module;
        if (!modules_.TryGetValue(moduleFile, out module))
        {
            UnityLogger.Singleton.Error("Module {0} not found", moduleFile);
            return;
        }

        MethodInfo miPreload = module.entryClass.GetMethod("Preload");
        try
        {
            miPreload.Invoke(module.entryInstance, new object[] { onBootStepProgress_, onBootStepFinish_ });
        }
        catch (Exception ex)
        {
            UnityLogger.Singleton.Exception(ex);
        }
    }


    private void handleBootStepProgress(int _percentage)
    {
        //Debug.Log(_percentage);
    }

    private void handleBootStepFinish(string _module)
    {
        UnityLogger.Singleton.Info("Boot {0} finished", _module);
        BootStep step = bootloader_.steps[currentBootStep_];
        finishedBootLength_ += step.length;

        updateProgress();

        currentBootStep_ += 1;
        executeNextBootStep();
    }

    private void updateProgress()
    {
        OnProgressChanged(finishedBootLength_ * 100 / totalBootLength_);
    }
}

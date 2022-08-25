using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;
using MVCS = XTC.FMP.LIB.MVCS;

public class ModuleManager
{
    public class BootStep
    {
        [XmlAttribute("length")]
        public int length = 1;
        [XmlAttribute("tip")]
        public string tip = "";
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

    public Action<int> OnUpgressChanged { get; set; }
    public Action<string> OnTipChanged { get; set; }
    public Action OnBootFinish { get; set; }

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

    public ModuleManager()
    {
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(assemblyResolve);
        onBootStepProgress_ = this.handleBootStepProgress;
        onBootStepFinish_ = this.handleBootStepFinish;
    }

    public void Load(string _vendor, string _datapath)
    {
        prepareBootloader(_vendor, _datapath);

        string modulesDir = Path.Combine(_datapath, string.Format("{0}/modules", _vendor));
        UnityLogger.Singleton.Info("ready to load modules from {0}", modulesDir);
        if (!Directory.Exists(modulesDir))
        {
            UnityLogger.Singleton.Error("{0} not found", modulesDir);
            return;
        }

        Func<string, Assembly> loadAssembly = (_entry) =>
        {
            string file = Path.Combine(modulesDir, _entry);
            Assembly assembly = Assembly.LoadFile(file);
            if (null == assembly)
            {
                UnityLogger.Singleton.Error("load assembly from {0} failed!", file);
                return null;
            }

            string filename = Path.GetFileName(_entry);
            assemblies_[filename] = assembly;
            return assembly;
        };

        Action<string, Assembly> instantiateAssembly = (_entry, _assembly) =>
        {
            string filename = Path.GetFileNameWithoutExtension(_entry);
            string namespacePrefix = filename.Substring(0, filename.Length - ".LIB.Unity".Length);
            string entryClassName = namespacePrefix + ".LIB.Unity.MyEntry";
            object instanceEntry = _assembly.CreateInstance(entryClassName);
            Type entryClass = _assembly.GetType(entryClassName);
            Module module = new Module(_assembly, instanceEntry, entryClass, namespacePrefix);
            modules_[filename] = module;
            UnityLogger.Singleton.Info("load assembly {0} success", _entry);
        };


        Dictionary<string, Assembly> moduleFiles = new Dictionary<string, Assembly>();
        foreach (var file in Directory.GetFiles(modulesDir))
        {
            if (!file.EndsWith(".dll"))
                continue;
            UnityLogger.Singleton.Debug("found {0}", file);
            var assembly = loadAssembly(file);
            if (file.EndsWith(".LIB.Unity.dll") && file.Contains("FMP.MOD"))
            {
                foreach (var bootstep in bootloader_.steps)
                {
                    string filename = Path.GetFileNameWithoutExtension(file);
                    if (filename.Equals(bootstep.module))
                    {
                        moduleFiles[file] = assembly;
                        break;
                    }
                }
            }
        }

        // module需要在其他非模块程序集加载完后再加载
        foreach (var pair in moduleFiles)
        {
            instantiateAssembly(pair.Key, pair.Value);
        }

        UnityLogger.Singleton.Info("finally load {0} modules", modules_.Count);
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
        foreach (var bootstep in bootloader_.steps)
        {
            Module module;
            if (!modules_.TryGetValue(bootstep.module, out module))
            {
                UnityLogger.Singleton.Error("Module {0} not found", bootstep.module);
                continue;
            }
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
        foreach (var bootstep in bootloader_.steps)
        {
            Module module;
            if (!modules_.TryGetValue(bootstep.module, out module))
            {
                UnityLogger.Singleton.Error("Module {0} not found", bootstep.module);
                continue;
            }

            MethodInfo miRegister = module.entryClass.GetMethod("RegisterDummy");
            miRegister.Invoke(module.entryInstance, null);
        }
    }

    public void Preload()
    {
        totalBootLength_ = 0;
        foreach (var step in bootloader_.steps)
        {
            totalBootLength_ += step.length;
        }

        executeNextBootStep();
    }

    public void Cancel()
    {
        foreach (var bootstep in bootloader_.steps)
        {
            Module module;
            if (!modules_.TryGetValue(bootstep.module, out module))
            {
                UnityLogger.Singleton.Error("Module {0} not found", bootstep.module);
                continue;
            }

            MethodInfo miCancel = module.entryClass.GetMethod("CancelDummy");
            miCancel.Invoke(module.entryInstance, null);
        }
    }

    private Assembly assemblyResolve(object sender, ResolveEventArgs args)
    {
        //Debug.Log(args.Name);
        return assemblies_[args.Name];
    }

    private void prepareBootloader(string _vendor, string _datapath)
    {
        string config = Path.Combine(_datapath, string.Format("{0}/Bootloader.xml", _vendor));
        var xs = new XmlSerializer(typeof(Bootloader));
        // 如果文件不存在，则创建默认的配置文件
        if (!File.Exists(config))
        {
            bootloader_ = new Bootloader();
            using (FileStream writer = new FileStream(config, FileMode.CreateNew))
            {
                xs.Serialize(writer, bootloader_);
                writer.Close();
            }
        }

        try
        {
            using (FileStream reader = new FileStream(config, FileMode.Open))
            {
                bootloader_ = xs.Deserialize(reader) as Bootloader;
            }
        }
        catch (System.Exception ex)
        {
            UnityLogger.Singleton.Exception(ex);
            return;
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
        UnityLogger.Singleton.Info("Boot the step, module is {0}, tip is {1}", step.module, step.tip);
        OnTipChanged(step.tip);

        Module module;
        if (!modules_.TryGetValue(step.module, out module))
        {
            UnityLogger.Singleton.Error("Module {0} not found", step.module);
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

        OnUpgressChanged(finishedBootLength_ * 100 / totalBootLength_);

        currentBootStep_ += 1;
        executeNextBootStep();
    }
}

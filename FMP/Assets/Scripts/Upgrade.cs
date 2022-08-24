using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using XTC.FMP.LIB.MVCS;
using UnityEngine.Networking;
using System;
using static Upgrade.Schema;
using Newtonsoft.Json;
using static UpgradeBehaviour;

public class Upgrade
{
    public class Manifest
    {
        public class Entry
        {
            public string file = "";
            public ulong size = 0;
            public string hash = "";
        }
        public List<Entry> entries = new List<Entry>();
    }

    public class Schema
    {
        public class Option
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

        public class FMP
        {
            [XmlAttribute]
            public string environment { get; set; } = "product";
            [XmlAttribute]
            public string repository { get; set; } = "";
            [XmlArray("References"), XmlArrayItem("Reference")]
            public Reference[] refenrences { get; set; } = new Reference[0];

            [XmlArray("Plugins"), XmlArrayItem("Plugin")]
            public Plugin[] plugins { get; set; } = new Plugin[0];
        }

        public class Update
        {
            [XmlAttribute("strategy")]
            public string strategy { get; set; } = "skip";
            [XmlElement("FMP")]
            public FMP fmp { get; set; } = new FMP();
        }

        public class Body
        {

            [XmlElement("Update")]
            public Update update { get; set; } = new Update();
        }

        [XmlElement("Body")]
        public Body body { get; set; } = new Body();

        [XmlArray("Header"), XmlArrayItem("Option")]
        public Option[] options { get; set; } = new Option[] {
            new Option
            {
                attribute = "Update.strategy",
                values = "升级策略，可选值为：skip, auto, manual",
            },
        };
    }

    public class ManifestTask
    {
        public string url { get; set; }
        public bool finished { get; set; } = false;
    }

    public class FileTask
    {
        public string url { get; set; }
        public string saveAs { get; set; }
        public ulong size { get; set; }
        public string hash { get; set; }
        public bool finished { get; set; } = false;
    }


    public enum ErrorCode
    {
        OK,
        MANIFEST_NETWORK_ERROR,
        MANIFEST_PARSE_ERROR,
        ENTRY_NOTFOUNDINREPO,
        ENTRY_NETWORK_ERROR,
        ENTRY_SIZE_ERROR,
    }


    public ErrorCode errorCode { get; private set; }
    public ulong updateTotalSize { get; private set; } = 0;
    public ulong updateFinishedSize { get; private set; } = 0;
    public string updateEntryHash { get; private set; } = "";
    public float updateEntryProgress
    {
        get
        {
            if (null == fileWebRequest_)
                return 0;
            return fileWebRequest_.downloadProgress;
        }
    }

    private List<ManifestTask> manifestTasks_ = new List<ManifestTask>();
    private List<FileTask> fileTasks_ = new List<FileTask>();
    private UnityWebRequest fileWebRequest_ = null;

    public void ParseSchema(Schema _schema)
    {
        fileTasks_.Clear();
        foreach (var reference in _schema.body.update.fmp.refenrences)
        {
            string version = reference.version;
            if (_schema.body.update.fmp.environment.Equals("develop"))
                version = "develop";

            string address = string.Format("{0}/modules/{1}/{2}@{3}", _schema.body.update.fmp.repository, reference.org, reference.module, version);

            var manifestTask = new ManifestTask();
            manifestTask.url = string.Format("{0}/manifest.json", address);
            manifestTasks_.Add(manifestTask);

            var bridgeTask = new FileTask();
            bridgeTask.url = string.Format("{0}/fmp-{1}-{2}-lib-bridge.dll", address, reference.org.ToLower(), reference.module.ToLower());
            bridgeTask.saveAs = string.Format("modules/fmp-{0}-{1}-lib-bridge.dll", reference.org.ToLower(), reference.module.ToLower());
            fileTasks_.Add(bridgeTask);

            var protoTask = new FileTask();
            protoTask.url = string.Format("{0}/fmp-{1}-{2}-lib-proto.dll", address, reference.org.ToLower(), reference.module.ToLower());
            protoTask.saveAs = string.Format("modules/fmp-{0}-{1}-lib-proto.dll", reference.org.ToLower(), reference.module.ToLower());
            fileTasks_.Add(protoTask);

            var mvcsFileTask = new FileTask();
            mvcsFileTask.url = string.Format("{0}/fmp-{1}-{2}-lib-mvcs.dll", address, reference.org.ToLower(), reference.module.ToLower());
            mvcsFileTask.saveAs = string.Format("modules/fmp-{0}-{1}-lib-mvcs.dll", reference.org.ToLower(), reference.module.ToLower());
            fileTasks_.Add(mvcsFileTask);

            var unityFileTask = new FileTask();
            unityFileTask.url = string.Format("{0}/{1}.FMP.MOD.{2}.LIB.Unity.dll", address, reference.org, reference.module);
            unityFileTask.saveAs = string.Format("modules/{0}.FMP.MOD.{1}.LIB.Unity.dll", reference.org, reference.module);
            fileTasks_.Add(unityFileTask);

            var uabTask = new FileTask();
            uabTask.url = string.Format("{0}/{1}_{2}{3}.uab", address, reference.org.ToLower(), reference.module.ToLower(), getPlatformSuffix());
            uabTask.saveAs = string.Format("uabs/{0}_{1}.uab", reference.org.ToLower(), reference.module.ToLower());
            fileTasks_.Add(uabTask);

            var xmlTask = new FileTask();
            xmlTask.url = string.Format("{0}/{1}_{2}.xml", address, reference.org, reference.module);
            xmlTask.saveAs = string.Format("configs/_{0}_{1}.xml", reference.org, reference.module);
            fileTasks_.Add(xmlTask);
        }

        foreach (var plugin in _schema.body.update.fmp.plugins)
        {
            string version = plugin.version;
            if (_schema.body.update.fmp.environment.Equals("develop"))
                version = "develop";

            string address = string.Format("{0}/plugins/{1}@{2}", _schema.body.update.fmp.repository, plugin.name, version);

            var manifestTask = new ManifestTask();
            manifestTask.url = string.Format("{0}/manifest.json", address);
            manifestTasks_.Add(manifestTask);

            var dllTask = new FileTask();
            dllTask.url = string.Format("{0}/{1}.dll", address, plugin.name);
            dllTask.saveAs = string.Format("modules/{0}.dll", plugin.name);
            fileTasks_.Add(dllTask);
        }
    }

    public IEnumerator CheckDependencies(Schema _schema)
    {
        errorCode = ErrorCode.OK;
        updateTotalSize = 0;
        updateFinishedSize = 0;
        updateEntryHash = "";

        string cache_dir = Path.Combine(Constant.DataPath, VendorManager.Singleton.active);
        cache_dir = Path.Combine(cache_dir, ".upgrade");

        foreach (var manifestTask in manifestTasks_)
        {
            //下载过的清单不需要再次下载
            if (manifestTask.finished)
                continue;

            // 下载清单文件
            UnityLogger.Singleton.Debug("pull {0}", manifestTask.url);
            using (UnityWebRequest uwr = UnityWebRequest.Get(manifestTask.url))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    UnityLogger.Singleton.Error(uwr.error);
                    errorCode = ErrorCode.MANIFEST_NETWORK_ERROR;
                    yield break;
                }
                // 解析清单文件
                string text = uwr.downloadHandler.text;

                var manifest = new Manifest();
                try
                {
                    manifest = JsonConvert.DeserializeObject<Manifest>(text);
                }
                catch (Exception ex)
                {
                    UnityLogger.Singleton.Exception(ex);
                    errorCode = ErrorCode.MANIFEST_PARSE_ERROR;
                    yield break;
                }

                if (null == manifest)
                {
                    UnityLogger.Singleton.Error("manifest is null, maybe convert from json is failed");
                    errorCode = ErrorCode.MANIFEST_PARSE_ERROR;
                    yield break;
                }

                foreach (var entry in manifest.entries)
                {
                    var file = fileTasks_.Find((_item) =>
                    {
                        return Path.GetFileName(_item.url).Equals(entry.file);
                    });
                    if (null == file)
                        continue;
                    file.size = entry.size;
                    file.hash = entry.hash;
                    // 如果文件已经下载
                    string hashFile = Path.Combine(cache_dir, file.saveAs) + ".hash";
                    if (File.Exists(hashFile))
                    {
                        if (File.ReadAllText(hashFile).Equals(file.hash))
                        {
                            file.finished = true;
                        }
                    }
                }
            }
        }

        // 重新计算需要下载总量和已下载量
        foreach (var fileTask in fileTasks_)
        {
            // 任何一个需要更新的文件，如果在仓库中找不到，都报错
            if (string.IsNullOrEmpty(fileTask.hash))
            {
                errorCode = ErrorCode.ENTRY_NOTFOUNDINREPO;
                yield break;
            }

            updateTotalSize += fileTask.size;
            if (fileTask.finished)
                updateFinishedSize += fileTask.size;
        }
    }

    public IEnumerator DownloadDependencies(Schema _schema)
    {
        errorCode = ErrorCode.OK;
        yield return downloadDependencies(_schema);
    }

    public IEnumerator OverwriteDependencies(Schema _schema)
    {
        yield return new UnityEngine.WaitForEndOfFrame();
        string vendor_dir = Path.Combine(Constant.DataPath, VendorManager.Singleton.active);
        string cache_dir = Path.Combine(vendor_dir, ".upgrade");
        foreach (var task in fileTasks_)
        {
            if (!File.Exists(Path.Combine(cache_dir, task.saveAs)))
                continue;
            UnityLogger.Singleton.Info("Overwrite {0}", task.saveAs);
            File.Copy(Path.Combine(cache_dir, task.saveAs), Path.Combine(vendor_dir, task.saveAs), true);
            File.Delete(Path.Combine(cache_dir, task.saveAs));
        }
    }

    private IEnumerator downloadDependencies(Schema _schema)
    {
        // 任何错误都不继续下载
        if (errorCode != ErrorCode.OK)
        {
            yield break;
        }

        // 取一个未完成的任务
        var task = fileTasks_.Find((_item) =>
        {
            return !_item.finished;
        });

        // 已经完成全部的下载
        if (null == task)
        {
            yield break;
        }

        // 开始下载文件
        updateEntryHash = task.hash;
        fileWebRequest_ = UnityWebRequest.Get(task.url);
        // 下载到缓存目录中
        UnityLogger.Singleton.Trace("download {0}", task.saveAs);
        string cache_dir = Path.Combine(Constant.DataPath, VendorManager.Singleton.active);
        cache_dir = Path.Combine(cache_dir, ".upgrade");
        string saveAsPath = Path.Combine(cache_dir, task.saveAs);
        fileWebRequest_.downloadHandler = new DownloadHandlerFile(saveAsPath);
        yield return fileWebRequest_.SendWebRequest();
        if (fileWebRequest_.result != UnityWebRequest.Result.Success)
        {
            UnityLogger.Singleton.Error(fileWebRequest_.error);
            errorCode = ErrorCode.ENTRY_NETWORK_ERROR;
            yield break;
        }
        if (task.size != fileWebRequest_.downloadedBytes)
        {
            errorCode = ErrorCode.ENTRY_SIZE_ERROR;
            yield break;
        }
        // 保存文件的hash值
        File.WriteAllText(saveAsPath + ".hash", task.hash);
        updateFinishedSize += fileWebRequest_.downloadedBytes;
        fileWebRequest_.Dispose();
        fileWebRequest_ = null;
        task.finished = true;
        yield return downloadDependencies(_schema);
    }

    private string getPlatformSuffix()
    {
        string suffix = "";
        switch (UnityEngine.Application.platform)
        {
            case UnityEngine.RuntimePlatform.WindowsPlayer:
                suffix = "@windows";
                break;
            case UnityEngine.RuntimePlatform.WindowsEditor:
                suffix = "@windows";
                break;
            case UnityEngine.RuntimePlatform.Android:
                suffix = "@android";
                break;
            case UnityEngine.RuntimePlatform.WebGLPlayer:
                suffix = "@webgl";
                break;
            case UnityEngine.RuntimePlatform.LinuxEditor:
                suffix = "@linux";
                break;
            case UnityEngine.RuntimePlatform.LinuxPlayer:
                suffix = "@linux";
                break;
            case UnityEngine.RuntimePlatform.OSXEditor:
                suffix = "@osx";
                break;
            case UnityEngine.RuntimePlatform.OSXPlayer:
                suffix = "@osx";
                break;
            case UnityEngine.RuntimePlatform.IPhonePlayer:
                suffix = "@ios";
                break;
            default:
                suffix = "";
                break;
        }
        return suffix;
    }
}

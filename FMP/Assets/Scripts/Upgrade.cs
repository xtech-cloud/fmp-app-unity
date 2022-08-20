using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using XTC.FMP.LIB.MVCS;
using UnityEngine.Networking;
using System;
using static Upgrade.Schema;
using Newtonsoft.Json;

public class Upgrade
{
    public class MD5Json
    {
        public class Entry
        {
            public string file = "";
            public long size = 0;
            public string hash = "";
        }
        public List<Entry> entries = new List<Entry>();
    }

    public class Schema
    {
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
            public string environment { get; set; } = "";
            [XmlAttribute]
            public string repository { get; set; } = "";
            [XmlArray("References"), XmlArrayItem("Reference")]
            public Reference[] refenrences { get; set; } = new Reference[0];

            [XmlArray("Plugins"), XmlArrayItem("Plugin")]
            public Plugin[] plugins { get; set; } = new Plugin[0];
        }

        public class Update
        {
            [XmlElement("FMP")]
            public FMP fmp { get; set; } = new FMP();
        }

        [XmlElement("Update")]
        public Update update { get; set; } = new Update();
    }

    public class FileTask
    {
        public string url { get; set; }
        public long size { get; set; }
        public string hash { get; set; }
        public string saveAs { get; set; }
    }

    public class ReferenceTask
    {
        public string org { get; set; }
        public string module { get; set; }
        public string version { get; set; }
        public string md5json_url { get; set; }
        public List<FileTask> files = new List<FileTask>();
        public bool parsed { get; set; } = false;
    }

    public bool DependenciesHasUpdate { get; private set; }

    public enum ErrorCode
    {
        OK,
        NETWORK_ERROR,
        PARSE_ERROR,
        NOTFOUND_ERROR,
    }

    public class Result
    {
        public long totalSize { get; set; }
    }


    private Queue<ReferenceTask> referenceTaskQueue_ = new Queue<ReferenceTask>();

    public ErrorCode updateDependenciesError { get; private set; }
    public Result result { get; private set; }

    public void ParseSchema(Schema _schema)
    {
        referenceTaskQueue_.Clear();
        foreach (var reference in _schema.update.fmp.refenrences)
        {
            ReferenceTask task = new ReferenceTask();
            string version = reference.version;
            if (_schema.update.fmp.environment.Equals("develop"))
                version = "develop";

            string address = string.Format("{0}/{1}/{2}@{3}", _schema.update.fmp.repository, reference.org, reference.module, version);
            task.org = reference.org;
            task.module = reference.module;
            task.version = version;
            task.md5json_url = string.Format("{0}/md5.json", address);
            FileTask bridgeFileTask = new FileTask();
            bridgeFileTask.url = string.Format("{0}/fmp-{1}-{2}-lib-bridge.dll", address, reference.org.ToLower(), reference.module.ToLower());
            bridgeFileTask.saveAs = string.Format("modules/fmp-{1}-{2}-lib-bridge.dll", address, reference.org.ToLower(), reference.module.ToLower());
            task.files.Add(bridgeFileTask);
            FileTask protoFileTask = new FileTask();
            protoFileTask.url = string.Format("{0}/fmp-{1}-{2}-lib-proto.dll", address, reference.org.ToLower(), reference.module.ToLower());
            protoFileTask.saveAs = string.Format("modules/fmp-{1}-{2}-lib-proto.dll", address, reference.org.ToLower(), reference.module.ToLower());
            task.files.Add(protoFileTask);
            FileTask mvcsFileTask = new FileTask();
            mvcsFileTask.url = string.Format("{0}/fmp-{1}-{2}-lib-mvcs.dll", address, reference.org.ToLower(), reference.module.ToLower());
            mvcsFileTask.saveAs = string.Format("modules/fmp-{1}-{2}-lib-mvcs.dll", address, reference.org.ToLower(), reference.module.ToLower());
            task.files.Add(mvcsFileTask);
            FileTask unityFileTask = new FileTask();
            unityFileTask.url = string.Format("{0}/{1}.FMP.MOD.{2}.LIB.Unity.dll", address, reference.org, reference.module);
            unityFileTask.saveAs = string.Format("modules/{0}.FMP.MOD.{1}.LIB.Unity.dll", address, reference.org, reference.module);
            task.files.Add(unityFileTask);
            FileTask uabFileTask = new FileTask();
            uabFileTask.url = string.Format("{0}/{1}_{2}@win32.uab", address, reference.org.ToLower(), reference.module.ToLower());
            uabFileTask.saveAs = string.Format("uabs/{0}_{1}@win32.uab", address, reference.org.ToLower(), reference.module.ToLower());
            task.files.Add(uabFileTask);
            referenceTaskQueue_.Enqueue(task);
        }
    }

    public IEnumerator CheckDependencies(Schema _schema)
    {
        updateDependenciesError = ErrorCode.OK;
        result = new Result();
        foreach (var task in referenceTaskQueue_)
        {
            if (task.parsed)
                continue;

            // 下载MD5
            UnityLogger.Singleton.Debug("pull {0}", task.md5json_url);
            using (UnityWebRequest uwr = UnityWebRequest.Get(task.md5json_url))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    UnityLogger.Singleton.Error(uwr.error);
                    updateDependenciesError = ErrorCode.NETWORK_ERROR;
                    yield break;
                }
                // 解析MD5
                string text = uwr.downloadHandler.text;
                MD5Json md5json = new MD5Json();
                try
                {
                    md5json = JsonConvert.DeserializeObject<MD5Json>(text);
                }
                catch (Exception ex)
                {
                    UnityLogger.Singleton.Error(ex.Message);
                    UnityLogger.Singleton.Exception(ex);
                    updateDependenciesError = ErrorCode.PARSE_ERROR;
                    yield break;
                }

                foreach (var filetask in task.files)
                {
                    var entry = md5json.entries.Find((_item) =>
                    {
                        return _item.file.Equals(Path.GetFileName(filetask.url));
                    });
                    if (null == entry)
                    {
                        updateDependenciesError = ErrorCode.NOTFOUND_ERROR;
                        yield break;
                    }
                    filetask.size = entry.size;
                    filetask.hash = entry.hash;
                    result.totalSize += entry.size;
                }
                task.parsed = true;
            }
        }
    }
  }

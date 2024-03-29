﻿using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;

public class FrameworkUpdate
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
        ENTRY_COPY_ERROR,
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

    public void ParseSchema()
    {
        string environment = VendorManager.Singleton.active.updateConfig.schema.body.frameworkUpdate.environment;
        string repository = VendorManager.Singleton.active.updateConfig.schema.body.frameworkUpdate.repository;
        var dependencyConfig = VendorManager.Singleton.active.dependencyConfig;
        var themeS = VendorManager.Singleton.active.schema.ModuleThemeS;
        fileTasks_.Clear();
        foreach (var reference in dependencyConfig.schema.body.references)
        {
            string version = reference.version;
            if (environment.Equals("develop"))
                version = "develop";

            string address = string.Format("{0}/modules/{1}/{2}@{3}", repository, reference.org, reference.module, version);

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

            var jsonTask = new FileTask();
            jsonTask.url = string.Format("{0}/{1}_{2}.json", address, reference.org, reference.module);
            jsonTask.saveAs = string.Format("catalogs/_{0}_{1}.json", reference.org, reference.module);
            fileTasks_.Add(jsonTask);
        }

        foreach (var plugin in dependencyConfig.schema.body.plugins)
        {
            string version = plugin.version;
            if (environment.Equals("develop"))
                version = "develop";

            string address = string.Format("{0}/plugins/{1}@{2}", repository, plugin.name, version);

            var manifestTask = new ManifestTask();
            manifestTask.url = string.Format("{0}/manifest.json", address);
            manifestTasks_.Add(manifestTask);

            var dllTask = new FileTask();
            dllTask.url = string.Format("{0}/{1}.dll", address, plugin.name);
            dllTask.saveAs = string.Format("modules/{0}.dll", plugin.name);
            fileTasks_.Add(dllTask);
        }

        foreach (var pair in themeS)
        {
            foreach (var file in pair.Value.entityS)
            {
                var themeTask = new FileTask();
                themeTask.url = file.url;
                themeTask.hash = file.hash;
                themeTask.size = file.size;
                themeTask.saveAs = string.Format("themes/{0}/{1}", pair.Key, file.path);
                fileTasks_.Add(themeTask);
            }
        }
    }

    public IEnumerator CheckDependencies()
    {
        errorCode = ErrorCode.OK;
        updateTotalSize = 0;
        updateFinishedSize = 0;
        updateEntryHash = "";

        foreach (var manifestTask in manifestTasks_)
        {
            //下载过的清单不需要再次下载
            if (manifestTask.finished)
                continue;

            // 下载清单文件
            UnityLogger.Singleton.Debug("pull {0}", manifestTask.url);
            using (UnityWebRequest uwr = UnityWebRequest.Get(new Uri(manifestTask.url)))
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
                }
            }
        }

        // 比对已下载的哈希值记录
        foreach (var file in fileTasks_)
        { 
            // 如果文件已经下载
            string hashFile = Path.Combine(Storage.UpdateCachePath, file.saveAs) + ".hash";
            if (File.Exists(hashFile))
            {
                // 比对哈希值
                if (File.ReadAllText(hashFile).Equals(file.hash))
                {
                    file.finished = true;
                }
            }
        }

        // 重新计算需要下载总量和已下载量
        foreach (var fileTask in fileTasks_)
        {
            // 任何一个需要更新的文件，如果在仓库中找不到，都报错
            if (string.IsNullOrEmpty(fileTask.hash))
            {
                UnityLogger.Singleton.Error("{0} not found in repo", fileTask.url);
                errorCode = ErrorCode.ENTRY_NOTFOUNDINREPO;
                yield break;
            }

            updateTotalSize += fileTask.size;
            if (fileTask.finished)
                updateFinishedSize += fileTask.size;
        }
    }

    public IEnumerator DownloadDependencies()
    {
        errorCode = ErrorCode.OK;
        yield return downloadDependencies();
    }

    public IEnumerator OverwriteDependencies()
    {
        string vendorPath = Path.Combine(Storage.RootPath, Storage.VendorDir);
        yield return new UnityEngine.WaitForEndOfFrame();

        foreach (var task in fileTasks_)
        {
            if (!File.Exists(Path.Combine(Storage.UpdateCachePath, task.saveAs)))
                continue;
            UnityLogger.Singleton.Info("Overwrite {0}", task.saveAs);
            try
            {
                string dir = Path.GetDirectoryName(task.saveAs);
                if (!Directory.Exists(Path.Combine(vendorPath, dir)))
                    Directory.CreateDirectory(Path.Combine(vendorPath, dir));

                File.Copy(Path.Combine(Storage.UpdateCachePath, task.saveAs), Path.Combine(vendorPath, task.saveAs), true);
                File.Delete(Path.Combine(Storage.UpdateCachePath, task.saveAs));
            }
            catch (Exception ex)
            {
                UnityLogger.Singleton.Exception(ex);
                errorCode = ErrorCode.ENTRY_COPY_ERROR;
                yield break;
            }
        }
    }

    private IEnumerator downloadDependencies()
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
        fileWebRequest_ = UnityWebRequest.Get(new Uri(task.url));
        // 下载到缓存目录中
        UnityLogger.Singleton.Trace("download {0}", task.saveAs);
        string saveAsPath = Path.Combine(Storage.UpdateCachePath, task.saveAs);
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
        yield return downloadDependencies();
    }

    private string getPlatformSuffix()
    {
        string suffix = "";
        switch (Constant.Platform)
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

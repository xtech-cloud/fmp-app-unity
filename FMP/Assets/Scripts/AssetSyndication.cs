using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;

/// <summary>
/// 资源聚合
/// </summary>
public class AssetSyndication
{
    public class Manifest
    {
        public class Entry
        {
            public string file = "";
            public ulong size = 0;
            public string hash = "";
            public string url = "";
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

    private List<ManifestTask> manifestTaskS_ = new List<ManifestTask>();
    private List<FileTask> fileTasks_ = new List<FileTask>();
    private UnityWebRequest fileWebRequest_ = null;

    public void ParseSchema()
    {
        // 整理需要的资源包的Uuid
        foreach (var pair in VendorManager.Singleton.active.schema.ModuleCatalogS)
        {
            UnityLogger.Singleton.Info("parse catalog of {0}", pair.Key);
            string strCatalog = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(pair.Value));
            var catalogConfig = JsonConvert.DeserializeObject<ConfigEntity.CatalogConfig>(strCatalog);
            foreach (var section in catalogConfig.sectionS)
            {
                foreach (var contentUri in section.contentS)
                {
                    var vals = contentUri.Split("/");
                    if (2 != vals.Length)
                    {
                        UnityLogger.Singleton.Error("Found a invalid contentUri:{0} in {1}", contentUri, pair.Key);
                        continue;
                    }
                    var url = string.Format("{0}/{1}/manifest.json", VendorManager.Singleton.active.updateConfig.schema.body.assetSyndication.storage, vals[0]);
                    var task = manifestTaskS_.Find((_item) =>
                    {
                        return _item.url == url;
                    });
                    if (null != task)
                        continue;
                    task = new ManifestTask();
                    task.url = url;
                    manifestTaskS_.Add(task);
                }
            }
        }
        UnityLogger.Singleton.Info("found {0} bundles", manifestTaskS_.Count);
    }

    public IEnumerator CheckAssets()
    {
        errorCode = ErrorCode.OK;
        updateTotalSize = 0;
        updateFinishedSize = 0;
        updateEntryHash = "";
        string storageAddress = VendorManager.Singleton.active.updateConfig.schema.body.assetSyndication.storage;

        foreach (var manifestTask in manifestTaskS_)
        {
            //下载过的清单不需要再次下载
            if (manifestTask.finished)
                continue;

            //不下载忽略包
            string uuid = Path.GetFileName(Path.GetDirectoryName(manifestTask.url));
            string ignoreFile = Path.Combine(Storage.SyndicationCachePath, uuid + ".ignore");
            if (File.Exists(ignoreFile))
            {
                UnityLogger.Singleton.Warning("bundle {0} is ignored", uuid);
                manifestTask.finished = true;
                continue;
            }

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

                Manifest manifest = null;
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
                        return _item.url.Remove(0, storageAddress.Length + 1) == entry.file;
                    });
                    if (null != file)
                        continue;
                    file = new FileTask();
                    fileTasks_.Add(file);
                    if (string.IsNullOrEmpty(entry.url))
                        file.url = String.Format("{0}/{1}", storageAddress, entry.file);
                    else
                        file.url = entry.url;
                    file.saveAs = entry.file;
                    file.size = entry.size;
                    file.hash = entry.hash;
                    // 如果文件已经下载
                    string hashFile = Path.Combine(Storage.AssetsPath, file.saveAs) + ".hash";
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
            updateTotalSize += fileTask.size;
            if (fileTask.finished)
                updateFinishedSize += fileTask.size;
        }
    }

    public IEnumerator DownloadAssets()
    {
        errorCode = ErrorCode.OK;
        yield return downloadAssets();
    }

    private IEnumerator downloadAssets()
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
        string saveAsPath = Path.Combine(Storage.AssetsPath, task.saveAs);
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
        yield return downloadAssets();
    }
}

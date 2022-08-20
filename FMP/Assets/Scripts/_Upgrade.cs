/*
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;
using System.Security.Cryptography;

public class Upgrade
{
    public enum Strategy
    {
        Ignore,
        Auto,
        Manual,
    }

    [System.Serializable]
    public class Config
    {
        [System.Serializable]
        public class Patch
        {
            public string repo;
        }

        [System.Serializable]
        public class Update
        {
            public string tip;
            public string repo;
        }

        public Patch patch;
        public Update[] update;
    }

    public class Ui
    {
        public Transform root;
        public Transform panelPatchTip;
        public Transform panelUpdateTip;
        public Transform panelUpdate;
        public Transform panelUpdateResult;
        public Slider sliderSingle;
        public Slider sliderTotal;
        public Text txtUpdateTip;
        public Text txtUpdating;
        public Text txtTotalSize;
        public Text txtFinishSize;
        public Text txtHash;
    }


    [System.Serializable]
    public class UpdateRepository
    {
        [System.Serializable]
        public class UpdateEntry
        {
            public string path;
            public string hash;
            public ulong size;
            public string url;
        }
        public string strategy;
        public string host;
        public string key;
        public string saveAsTrim;
        public UpdateEntry[] entry;

        public string _md5;
        public string _filename;
    }


    public Transform canvas { get; set; }
    public MonoBehaviour mono { get; set; }
    public string vendor { get; set; }

    public Action onFinish;

    private Config config_ { get; set; }
    private Queue<Config.Update> updateTask_;
    private Dictionary<string, UpdateRepository> updateRepositoryMap_;
    private Queue<UpdateRepository.UpdateEntry> downloadEntryQueue_;
    private List<UpdateRepository.UpdateEntry> failedEntryList_;
    private Ui ui_;
    private string formatTipStr_ { get; set; }
    private string formatUpdateStr_ { get; set; }
    private int totalDownloadCount_;
    private int finishDownloadCount_;
    private ulong totalDownloadSize_;
    private ulong finishDownloadSize_;
    private string updateCachePath_ { get; set; }
    private string patchCachePath_ { get; set; }
    private UnityWebRequest uwrDownloader_ { get; set; }
    private int totalFailedCount_ { get; set; }

    public Upgrade()
    {
        updateTask_ = new Queue<Config.Update>();
        updateRepositoryMap_ = new Dictionary<string, UpdateRepository>();
        downloadEntryQueue_ = new Queue<UpdateRepository.UpdateEntry>();
        failedEntryList_ = new List<UpdateRepository.UpdateEntry>();
        totalFailedCount_ = 0;
    }

    public void ParseConfig(string _json)
    {
        config_ = JsonUtility.FromJson<Config>(_json);
        updateTask_.Clear();
        foreach (var update in config_.update)
        {
            updateTask_.Enqueue(update);
        }
    }

    public void Run()
    {
        Debug.Log("run upgrade");
        mono.StartCoroutine(refreshDownloadStatus());

        string cachePath = Path.Combine(Application.persistentDataPath, vendor);
        cachePath = Path.Combine(cachePath, ".upgrade");
        patchCachePath_ = Path.Combine(cachePath, "patch");
        updateCachePath_ = Path.Combine(cachePath, "update");
        if (!Directory.Exists(patchCachePath_))
            Directory.CreateDirectory(patchCachePath_);
        if (!Directory.Exists(updateCachePath_))
            Directory.CreateDirectory(updateCachePath_);

        ui_ = new Ui();
        ui_.root = canvas.Find("upgrade");
        ui_.panelPatchTip = ui_.root.Find("panelPatchTip");
        ui_.panelUpdateTip = ui_.root.Find("panelUpdateTip");
        ui_.panelUpdateResult = ui_.root.Find("panelUpdateResult");
        ui_.panelUpdate = ui_.root.Find("panelUpdate");
        ui_.sliderSingle = ui_.panelUpdate.Find("sSingle").GetComponent<Slider>();
        ui_.txtHash = ui_.panelUpdate.Find("sSingle/txtHash").GetComponent<Text>();
        ui_.sliderTotal = ui_.panelUpdate.Find("sTotal").GetComponent<Slider>();
        ui_.txtTotalSize = ui_.panelUpdate.Find("sTotal/txtTotalSize").GetComponent<Text>();
        ui_.txtFinishSize = ui_.panelUpdate.Find("sTotal/txtFinishSize").GetComponent<Text>();
        ui_.txtUpdateTip = ui_.root.Find("panelUpdateTip/txtTip").GetComponent<Text>();
        formatTipStr_ = ui_.txtUpdateTip.text;
        ui_.txtUpdating = ui_.root.Find("panelUpdate/txtTip").GetComponent<Text>();
        formatUpdateStr_ = ui_.txtUpdating.text;
        ui_.panelPatchTip.Find("btnYes").GetComponent<Button>().onClick.AddListener(runPatch);
        ui_.panelUpdateTip.Find("btnYes").GetComponent<Button>().onClick.AddListener(runUpdate);
        ui_.panelPatchTip.Find("btnNo").GetComponent<Button>().onClick.AddListener(() =>
        {
            Debug.Log("user ignore patch");
            ui_.panelPatchTip.gameObject.SetActive(false);
            //手动忽略升级后，检查更新
            checkUpdate();
        });
        ui_.panelUpdateTip.Find("btnNo").GetComponent<Button>().onClick.AddListener(() =>
        {
            Debug.Log("user ignore update");
            updateTask_.Dequeue();
            checkUpdate();
        });
        ui_.sliderTotal.value = 0;
        ui_.sliderSingle.value = 0;

        checkPatch();
    }

    private void pullPatchRepo(Config.Patch _patch, Action<Strategy> _onCallback)
    {
        Debug.Log("ready to pull repo of patch");
        if (null == config_ || string.IsNullOrEmpty(_patch.repo))
        {
            Debug.LogWarning("config or patch.repo is null or empty, ignore patch");
            _onCallback(Strategy.Ignore);
            return;
        }

        Debug.LogFormat("start download {0}", _patch.repo);

        mono.StartCoroutine(downloadText(config_.patch.repo, (_text) =>
        {
            //TODO compare version
        }, (_err) =>
         {
             Debug.LogError(_err);
             Debug.LogError("happen error, ignore patch");
             _onCallback(Strategy.Ignore);
         }));
    }

    private void checkPatch()
    {
        Debug.LogFormat("start check patch");
        Config.Patch patch = config_.patch;
        pullPatchRepo(patch, (_strategy) =>
        {
            Debug.LogFormat("patch's strategy is {0}", _strategy);
            // 自动升级
            if (Upgrade.Strategy.Auto == _strategy)
            {
                runPatch();
                return;
            }

            // 手动升级
            if (Upgrade.Strategy.Manual == _strategy)
            {
                ui_.root.gameObject.SetActive(true);
                ui_.panelPatchTip.gameObject.SetActive(true);
                return;
            }

            // 检查更新
            checkUpdate();
        });

    }

    private void checkUpdate()
    {
        Debug.Log("start check update");
        if (updateTask_.Count == 0)
        {
            Debug.Log("all update is finished");
            mono.StartCoroutine(delayFinish());
            return;
        }

        Config.Update update = updateTask_.Peek();
        pullUpdateRepo(update, (_strategy) =>
        {
            Debug.LogFormat("update's strategy is {0}", _strategy);
            // 自动升级
            if (Upgrade.Strategy.Auto == _strategy)
            {
                runUpdate();
                return;
            }

            // 手动升级
            if (Upgrade.Strategy.Manual == _strategy)
            {
                ui_.txtUpdateTip.text = string.Format(formatTipStr_, update.tip + string.Format("({0})", formatSize(totalDownloadSize_)));
                ui_.panelUpdateTip.gameObject.SetActive(true);
                ui_.root.gameObject.SetActive(true);
                return;
            }

            updateTask_.Dequeue();
            checkUpdate();
        });
    }

    public void pullUpdateRepo(Config.Update _update, Action<Strategy> _onCallback)
    {
        Debug.Log("ready to pull repo of update");
        if (string.IsNullOrEmpty(_update.repo))
        {
            Debug.LogWarning("config or update.repo is null or empty, ignore update");
            _onCallback(Strategy.Ignore);
            return;
        }
        Debug.LogFormat("start download {0}", _update.repo);

        totalDownloadCount_ = 0;
        finishDownloadCount_ = 0;
        totalDownloadSize_ = 0;
        finishDownloadSize_ = 0;
        downloadEntryQueue_.Clear();
        failedEntryList_.Clear();

        mono.StartCoroutine(downloadText(_update.repo, (_text) =>
        {
            try
            {
                var repo = JsonUtility.FromJson<UpdateRepository>(_text);
                Debug.LogFormat("the strategy of update is {0} in remote", repo.strategy.ToString());
                updateRepositoryMap_[_update.repo] = repo;
                repo._filename = Path.GetFileName(_update.repo);
                repo._md5 = getMD5(_text);
                Strategy strategy = Strategy.Ignore;
                if (repo.strategy.Equals(Strategy.Auto.ToString()))
                    strategy = Strategy.Auto;
                else if (repo.strategy.Equals(Strategy.Manual.ToString()))
                    strategy = Strategy.Manual;

                // 如果repo存在md5文件并且值和远端的MD5一致，则说明已经成功更新过，则跳过此次更新
                string repo_md5file = Path.Combine(updateCachePath_, repo._filename + ".md5");
                if (File.Exists(repo_md5file))
                {
                    string md5 = File.ReadAllText(repo_md5file);
                    if (md5.Equals(repo._md5))
                    {
                        Debug.Log("the md5 is matched between cache and remote");
                        strategy = Strategy.Ignore;
                    }
                }

                if (Strategy.Ignore != strategy)
                {
                    foreach (var entry in repo.entry)
                    {

                        var localPath = entry.path;
                        if (!string.IsNullOrEmpty(repo.saveAsTrim) && localPath.StartsWith(repo.saveAsTrim))
                            localPath = localPath.Remove(0, repo.saveAsTrim.Length);
                        // 忽略已经更新过的文件
                        string md5file = Path.Combine(updateCachePath_, localPath + ".md5");
                        if (File.Exists(md5file))
                        {
                            try
                            {
                                string md5 = File.ReadAllText(md5file);
                                if (md5.Equals(entry.hash))
                                    continue;
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }
                        totalDownloadSize_ += entry.size;
                        downloadEntryQueue_.Enqueue(entry);
                    }
                }
                totalDownloadCount_ = downloadEntryQueue_.Count;

                _onCallback(strategy);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _onCallback(Strategy.Ignore);
            }
        }, (_err) =>
         {
             Debug.LogError(_err);
             _onCallback(Strategy.Ignore);
         }));
    }

    private void runPatch()
    {
        //TODO run Upgrade.exe
        Application.Quit();
    }

    private void runUpdate()
    {
        Config.Update update = updateTask_.Dequeue();

        ui_.txtUpdating.text = string.Format(formatUpdateStr_, update.tip);
        ui_.panelUpdateTip.gameObject.SetActive(false);
        ui_.panelUpdate.gameObject.SetActive(true);
        ui_.root.gameObject.SetActive(true);

        UpdateRepository repo;
        if (!updateRepositoryMap_.TryGetValue(update.repo, out repo))
            return;


        loopDownloadEntry(repo, () =>
        {
            checkUpdate();
        });
    }

    private void loopDownloadEntry(UpdateRepository _repo, Action _onFinish)
    {
        if (downloadEntryQueue_.Count == 0)
        {
            // 仅当所有文件下载完成后保存repo的md5值
            if (finishDownloadCount_ == totalDownloadCount_)
            {
                // 保存repo的md5
                string repo_md5file = Path.Combine(updateCachePath_, _repo._filename + ".md5");
                File.WriteAllText(repo_md5file, _repo._md5);
            }

            string repo_failedfile = Path.Combine(updateCachePath_, _repo._filename + ".failed.json");
            using (StreamWriter sw = new StreamWriter(repo_failedfile))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(failedEntryList_.GetType());
                serializer.Serialize(sw, failedEntryList_);
            }

            ui_.panelUpdate.gameObject.SetActive(false);
            _onFinish();
            return;
        }

        var entry = downloadEntryQueue_.Dequeue();
        string host = _repo.host;
        if (!host.EndsWith("/"))
            host += "/";
        string url = entry.url;
        if (string.IsNullOrEmpty(url))
            url = string.Format("{0}{1}", host, UnityWebRequest.EscapeURL(_repo.key.Equals("hash") ? entry.hash : entry.path));
        string vendorDir = Path.Combine(Application.persistentDataPath, vendor);
        string localPath = entry.path;
        if (!string.IsNullOrEmpty(_repo.saveAsTrim) && localPath.StartsWith(_repo.saveAsTrim))
            localPath = localPath.Remove(0, _repo.saveAsTrim.Length);

        string saveAs = Path.Combine(vendorDir, localPath);
        string md5file = Path.Combine(updateCachePath_, localPath + ".md5");
        mono.StartCoroutine(downloadFile(url, saveAs, md5file, entry.hash, () =>
        {
            loopDownloadEntry(_repo, _onFinish);
        }, (_err) =>
         {
             failedEntryList_.Add(entry);
             totalFailedCount_ += 1;
             Debug.LogError(_err);
             loopDownloadEntry(_repo, _onFinish);
         }));
    }

    private IEnumerator downloadText(string _url, Action<string> _onFinish, Action<string> _onError)
    {
        using (UnityWebRequest uwr = UnityWebRequest.Get(_url))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                _onError(uwr.error);
                yield break;
            }

            _onFinish(uwr.downloadHandler.text);
        }
    }

    private IEnumerator downloadFile(string _url, string _saveAs, string _md5file, string _md5, Action _onFinish, Action<string> _onError)
    {
        ui_.sliderSingle.value = 0.0f;
        ui_.txtHash.text = _md5;

        uwrDownloader_ = UnityWebRequest.Get(_url);
        uwrDownloader_.downloadHandler = new DownloadHandlerFile(_saveAs);
        yield return uwrDownloader_.SendWebRequest();

        if (uwrDownloader_.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError(_url);
            _onError(uwrDownloader_.error);
            yield break;
        }

        if (null != uwrDownloader_.error)
        {
            Debug.LogError(_url);
            _onError(uwrDownloader_.error);
            yield break;
        }

        finishDownloadCount_ += 1;
        finishDownloadSize_ += uwrDownloader_.downloadedBytes;
        ui_.sliderSingle.value = 1.0f;

        uwrDownloader_.Dispose();
        uwrDownloader_ = null;

        string dir = Path.GetDirectoryName(_md5file);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_md5file, _md5);
        _onFinish();
    }

    private string getMD5(string _str)
    {
        byte[] data = System.Text.Encoding.GetEncoding("utf-8").GetBytes(_str);
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] bytes = md5.ComputeHash(data);
        string str = "";
        for (int i = 0; i < bytes.Length; i++)
        {
            str += bytes[i].ToString("x2");
        }
        return str;
    }

    private IEnumerator refreshDownloadStatus()
    {
        float timer = 0;
        ulong singleDownloadedSize = 0;
        while (true)
        {
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;
            if (uwrDownloader_ == null)
            {
                ui_.sliderSingle.value = 0;
                singleDownloadedSize = 0;
            }
            else
            {
                if (uwrDownloader_.isDone)
                    ui_.sliderSingle.value = 1;
                else
                    ui_.sliderSingle.value = uwrDownloader_.downloadProgress;
                singleDownloadedSize = uwrDownloader_.downloadedBytes;
            }

            ui_.sliderTotal.value = totalDownloadSize_ == 0 ? 0 : (finishDownloadSize_ + singleDownloadedSize) * 1.0f / totalDownloadSize_;
            if (timer > 1)
            {
                ui_.txtTotalSize.text = formatSize(totalDownloadSize_);
                ui_.txtFinishSize.text = formatSize(finishDownloadSize_ + singleDownloadedSize);
                timer = 0;
            }
        }
    }

    private string formatSize(ulong _size)
    {
        if (_size < 1024)
            return string.Format("{0}B", _size);
        if (_size < 1024 * 1024)
            return string.Format("{0}K", _size / 1024);
        if (_size < 1024 * 1024 * 1024)
            return string.Format("{0}M", _size / 1024 / 1024);
        return string.Format("{0}G", _size / 1024 / 1024 / 1024);
    }

    private IEnumerator delayFinish()
    {
        if (totalFailedCount_ > 0)
        {
            ui_.panelUpdateTip.gameObject.SetActive(false);
            var text = ui_.panelUpdateResult.Find("txtTip").GetComponent<Text>();
            text.text = string.Format(text.text, totalFailedCount_);
            ui_.panelUpdateResult.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.5f);
        }
        onFinish();
    }
}
*/

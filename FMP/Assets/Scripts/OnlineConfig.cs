using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class OnlineConfig
{
    [System.Serializable]
    public class Entry
    {
        public string url = "";
        public string path = "";
        public string apikey = "";
        public string saveAs = "";
        public string encode = "";
    }

    [System.Serializable]
    public class ConfigSchema
    {
        public List<Entry> entry = new List<Entry>();
    }

    [System.Serializable]
    public class Request
    {
        public string path = "";
    }


    [System.Serializable]
    public class Reply
    {
        [System.Serializable]
        public class Status
        {
            public int code = 0;
            public string message = "";
        }

        [System.Serializable]
        public class Entity
        {
            public string content = "";
        }

        public Status status = new Status();
        public Entity entity = new Entity();
    }

    public IEnumerator Download()
    {
        // 获取ogm配置
        string configFile = Path.Combine(Application.streamingAssetsPath, "online-config.json");
        if (!File.Exists(configFile))
        {
            Debug.LogWarning("online-config.json not found, skip download config from remote");
            yield break;
        }

        ConfigSchema schema = new ConfigSchema();
        try
        {
            string content = File.ReadAllText(configFile);
            schema = JsonUtility.FromJson<ConfigSchema>(content);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            yield break;
        }

        foreach (var entry in schema.entry)
        {
            Request req = new Request();
            req.path = entry.path.Replace("$sn$", Constant.DeviceCode).Replace("$product$", Application.productName);

            string json = JsonUtility.ToJson(req);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            Debug.LogFormat("download {0} from {1} ...", req.path, entry.url);
            using (UnityWebRequest uwr = new UnityWebRequest(entry.url, UnityWebRequest.kHttpVerbPOST))
            {
                uwr.uploadHandler = new UploadHandlerRaw(data);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.SetRequestHeader("Content-Type", "application/json");
                uwr.SetRequestHeader("apikey", entry.apikey);
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogError(uwr.error);
                    continue;
                }

                try
                {
                    string rsp = uwr.downloadHandler.text;
                    var reply = JsonUtility.FromJson<Reply>(rsp);
                    if (reply.status.code != 0)
                    {
                        Debug.LogError(reply.status.message);
                        continue;
                    }
                    if (string.IsNullOrEmpty(reply.entity.content))
                    {
                        Debug.LogError("content is null or empty");
                        continue;
                    }

                    string saveAs = entry.saveAs;
                    saveAs = saveAs.Replace("$streamingAssetsPath$", Application.streamingAssetsPath);

                    File.WriteAllText(saveAs, reply.entity.content);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    continue;
                }

            }
        }
    }
}

using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Newtonsoft.Json;

public class SelectorBehaviour : MonoBehaviour
{
    public GameObject templateVendor;

    private IEnumerator Start()
    {
        UnityLogger.Singleton.Info("########### Enter Selector Scene");
        // WebGL跳过选择
        if (RuntimePlatform.WebGLPlayer == Constant.Platform)
        {
            SceneManager.LoadScene("Splash");
            yield break;
        }

        templateVendor.SetActive(false);

        UnityLogger.Singleton.Info("active vendor is {0}", VendorManager.Singleton.activeUuid);

        // 如果有激活的vendor，跳转到splash
        if (null != VendorManager.Singleton.active)
        {
            SceneManager.LoadScene("Splash");
            yield break;
        }

        // 如果命令参数和配置文件均没有指定vendor，显示vendor选择
        foreach (var vendorDir in Directory.GetDirectories(Storage.RootPath))
        {
            string vendorName = Path.GetFileName(vendorDir);
            UnityLogger.Singleton.Info("load meta.json of vendor:{0}", vendorName);
            Storage storage = new Storage();
            yield return storage.ReadBytesFromRoot(Path.Combine(vendorName, "meta.json"));
            if (!string.IsNullOrEmpty(storage.error))
            {
                UnityLogger.Singleton.Error(storage.error);
                continue;
            }
            var vendor = Vendor.Parse(storage.bytes);
            if (null == vendor)
                continue;
            var clone = GameObject.Instantiate(templateVendor, templateVendor.transform.parent);
            clone.name = vendor.schema.Name;
            clone.transform.Find("text").GetComponent<Text>().text = vendor.schema.Display;
            clone.gameObject.SetActive(true);
            clone.GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(activateVendor(clone.name));
            });
        }
    }

    private IEnumerator activateVendor(string _vendor)
    {

        yield return VendorManager.Singleton.Activate(_vendor);
        SceneManager.LoadScene("Splash");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectorBehaviour : MonoBehaviour
{
    public GameObject templateVendor;

    private void Awake()
    {
        UnityLogger.Singleton.Info("########### Enter Selector Scene");


        UnityLogger.Singleton.Info("active vendor is {0}", VendorManager.Singleton.active);

        // 如果有激活的vendor，跳转到splash
        if (!string.IsNullOrEmpty(VendorManager.Singleton.active))
        {
            SceneManager.LoadScene("splash");
            return;
        }

        // 如果有激活的vendor，跳转到splash
        if (!string.IsNullOrEmpty(VendorManager.Singleton.active))

        // 如果命令参数和配置文件均没有指定vendor，显示vendor选择
        foreach (var vendor in AppConfig.Singleton.body.vendorSelector.vendors)
        {
            var clone = GameObject.Instantiate(templateVendor, templateVendor.transform.parent);
            clone.name = vendor.scope;
            clone.transform.Find("text").GetComponent<Text>().text = vendor.display;
            clone.gameObject.SetActive(true);
            clone.GetComponent<Button>().onClick.AddListener(() =>
            {
                VendorManager.Singleton.active = clone.name;
                SceneManager.LoadScene("splash");
            });
        }
    }
}

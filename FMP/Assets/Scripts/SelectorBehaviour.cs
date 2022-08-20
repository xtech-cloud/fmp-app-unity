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

        templateVendor.gameObject.SetActive(false);
        foreach (var vendor in AppConfig.Singleton.body.vendorSelector.vendors)
        {
            var clone = GameObject.Instantiate(templateVendor, templateVendor.transform.parent);
            clone.name = vendor.directory;
            clone.transform.Find("text").GetComponent<Text>().text = vendor.name;
            clone.gameObject.SetActive(true);
            clone.GetComponent<Button>().onClick.AddListener(() =>
            {
                AppConfig.Singleton.body.vendorSelector.active = clone.name;
                SceneManager.LoadScene("splash");
            });
        }
    }
}

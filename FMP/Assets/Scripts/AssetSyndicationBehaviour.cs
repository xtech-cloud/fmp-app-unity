using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AssetSyndicationBehaviour : MonoBehaviour
{
    [Serializable]
    public class Ui
    {
        [Serializable]
        public class PanelUpdateTip
        {
            public Transform root;
            public Text tip;
            public Button btnYes;
            public Button btnNo;
        }

        [Serializable]
        public class PanelUpdating
        {
            public Transform root;
            public Slider sliderTotal;
            public Slider sliderSingle;
            public Text textHash;
            public Text textTotalSize;
            public Text textFinishSize;
        }

        [Serializable]
        public class PanelUpdateError
        {
            public Transform root;
            public Text tip;
            public Button btnRetry;
            public Button btnSkip;
        }

        [Serializable]
        public class PanelUpdateSuccess
        {
            public Transform root;
        }

        [Serializable]
        public class PanelUpdateFailure
        {
            public Transform root;
        }

        public Transform root;

        public PanelUpdateTip updateTipPanel;
        public PanelUpdating updatingPanel;
        public PanelUpdateError updateErrorPanel;
        public PanelUpdateSuccess updateSuccessPanel;
        public PanelUpdateFailure updateFailurePanel;
    }

    public class UiTip
    {
        public string update_parse_failure;
        public string assets_update_tip;
        public string assets_error;
        public string downloading_tip;
    }

    public Ui ui;
    public TextAsset updateTip;

    public enum Panel
    {
        NONE,
        TIP,
        UPDATING,
        ERROR,
        FAILURE,
        SUCCESS,
    }

    private AssetSyndication assetSyndication = new AssetSyndication();
    private UiTip uiTip_;
    private string updateStrategy_;

    private void Awake()
    {
        UnityLogger.Singleton.Info("########### Enter AssetSyndication Scene");
        uiTip_ = JsonUtility.FromJson<UiTip>(updateTip.text);

        ui.root.gameObject.SetActive(true);
        ui.updateErrorPanel.btnSkip.onClick.AddListener(() =>
        {
            switchPanel(Panel.FAILURE);
            enterStartup(3);
        });
        ui.updateErrorPanel.btnRetry.onClick.AddListener(() =>
        {
            StartCoroutine(updateAssets());
        });
        ui.updateTipPanel.btnYes.onClick.AddListener(() =>
        {
            ui.updateTipPanel.root.gameObject.SetActive(false);
            StartCoroutine(downloadAssets());
        });
        ui.updateTipPanel.btnNo.onClick.AddListener(() =>
        {
            enterStartup(0);
        });
        ui.updatingPanel.sliderTotal.value = 0;
        ui.updatingPanel.sliderSingle.value = 0;
        ui.updatingPanel.textHash.text = "";
        ui.updatingPanel.textTotalSize.text = "";
        ui.updatingPanel.textFinishSize.text = "";

        switchPanel(Panel.NONE);
    }


    private IEnumerator Start()
    {
        // WASM不需要下载资源
        if (RuntimePlatform.WebGLPlayer == Constant.Platform)
        {
            enterStartup(0);
            yield break;
        }

        updateStrategy_ = VendorManager.Singleton.active.updateConfig.schema.body.assetSyndication.strategy;
        UnityLogger.Singleton.Info("Strategy of AssetSyndication is {0}", updateStrategy_);

        if (updateStrategy_.Equals("skip"))
        {
            UnityLogger.Singleton.Warning("skip AssetSyndication");
            enterStartup(0);
            yield break;
        }

        assetSyndication.ParseSchema();
        yield return updateAssets();
    }

    private void Update()
    {
        ui.updatingPanel.textHash.text = assetSyndication.updateEntryHash;
        ui.updatingPanel.textFinishSize.text = formatSize(assetSyndication.updateFinishedSize);
        ui.updatingPanel.textTotalSize.text = formatSize(assetSyndication.updateTotalSize);
        ui.updatingPanel.sliderTotal.value = assetSyndication.updateTotalSize > 0 ? (assetSyndication.updateFinishedSize * 100 / assetSyndication.updateTotalSize) / 100f : 0;
        ui.updatingPanel.sliderTotal.value = assetSyndication.updateEntryProgress;
    }

    private void enterStartup(float _delay)
    {
        StartCoroutine(delayEnterStartup(_delay));
    }

    private IEnumerator delayEnterStartup(float _delay)
    {
        yield return new WaitForSeconds(_delay);
        SceneManager.LoadScene("Startup");
    }

    private IEnumerator updateAssets()
    {
        UnityLogger.Singleton.Info("check Assets ......");
        yield return assetSyndication.CheckAssets();
        if (AssetSyndication.ErrorCode.OK != assetSyndication.errorCode)
        {
            //检查阶段有错误
            UnityLogger.Singleton.Error("check assets has error: {0}", assetSyndication.errorCode.ToString());
            if (updateStrategy_.Equals("manual"))
            {
                // 手动模式弹出错误提示
                switchPanel(Panel.ERROR);
                ui.updateErrorPanel.tip.text = string.Format(uiTip_.assets_error, assetSyndication.errorCode.ToString());
            }
            else
            {
                switchPanel(Panel.FAILURE);
                // 非手动模式进入startup场景
                enterStartup(3);
            }
            yield break;
        }

        // 检查阶段没有错误
        if (updateStrategy_.Equals("manual"))
        {
            // 没有数据需要更新
            UnityLogger.Singleton.Info("ready to download assets, totalSize is {0}, finishedSize is {1}", assetSyndication.updateTotalSize, assetSyndication.updateFinishedSize);
            if (assetSyndication.updateFinishedSize >= assetSyndication.updateTotalSize)
            {
                enterStartup(0);
                yield break;
            }
            // 手动模式弹出更新提示
            switchPanel(Panel.TIP);
            ui.updateTipPanel.tip.text = string.Format(uiTip_.assets_update_tip, formatSize(assetSyndication.updateTotalSize - assetSyndication.updateFinishedSize));
            yield break;
        }

        // 自动模式开始下载
        yield return downloadAssets();
    }

    private IEnumerator downloadAssets()
    {
        UnityLogger.Singleton.Info("ready to download assets, totalSize is {0}, finishedSize is {1}", assetSyndication.updateTotalSize, assetSyndication.updateFinishedSize);
        // 没有数据需要更新
        if (assetSyndication.updateFinishedSize >= assetSyndication.updateTotalSize)
        {
            enterStartup(0);
            yield break;
        }

        switchPanel(Panel.UPDATING);
        yield return assetSyndication.DownloadAssets();
        // 有错误
        if (AssetSyndication.ErrorCode.OK != assetSyndication.errorCode)
        {
            UnityLogger.Singleton.Error("download has error: {0}", assetSyndication.errorCode.ToString());
            if (updateStrategy_.Equals("manual"))
            {
                // 手动模式弹出错误提示
                switchPanel(Panel.ERROR);
                ui.updateErrorPanel.tip.text = string.Format(uiTip_.assets_error, assetSyndication.errorCode.ToString());
            }
            else
            {
                switchPanel(Panel.FAILURE);
                // 非手动模式进入startup场景
                enterStartup(3);
            }
            yield break;
        }

        UnityLogger.Singleton.Info("all assets download success");
        switchPanel(Panel.SUCCESS);
        yield return new WaitForEndOfFrame();
        enterStartup(1);
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

    private void switchPanel(Panel _panel)
    {
        ui.updateTipPanel.root.gameObject.SetActive(Panel.TIP == _panel);
        ui.updatingPanel.root.gameObject.SetActive(Panel.UPDATING == _panel);
        ui.updateSuccessPanel.root.gameObject.SetActive(Panel.SUCCESS == _panel);
        ui.updateFailurePanel.root.gameObject.SetActive(Panel.FAILURE == _panel);
        ui.updateErrorPanel.root.gameObject.SetActive(Panel.ERROR == _panel);
    }
}

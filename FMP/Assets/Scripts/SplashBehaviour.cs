using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ZXing;
using ZXing.QrCode;
using XTC.OGM.SDK;
using System.Collections.Generic;

public class UiTip
{
    public string license_not_found;
    public string verify_code_1;
    public string verify_code_2;
    public string verify_code_3;
    public string verify_code_4;
    public string verify_code_5;
    public string verify_code_6;
    public string verify_code_7;
    public string verify_code_8;
    public string verify_code_14;
    public string expiry_left;
    public string vendor_active_none;
    public string vendor_directory_none;
}

public class SplashBehaviour : MonoBehaviour
{
    public Transform canvas;
    public Image imgBackground;
    public Image imgSlogan;
    public TextAsset tip;
    public Text txtError;
    public Text txtVersion;
    public Transform sn;

    private UiTip uiTip_;
    private Dictionary<string, string> verifyCodeMap = new Dictionary<string, string>();
    private Vendor activeVendor_;

    private int quitClikCount_ = 0;
    private float quitClikTimer_ = 0;

    // Start is called before the first frame update
    void Awake()
    {
        canvas.gameObject.SetActive(false);
        UnityLogger.Singleton.Info("########### Enter Splash Scene");
        activeVendor_ = VendorManager.Singleton.active;

        txtVersion.text = "ver " + Application.version + "." + BusinessBranch.Build.version;

        uiTip_ = JsonUtility.FromJson<UiTip>(tip.text);
        verifyCodeMap["verify_code_1"] = uiTip_.verify_code_1;
        verifyCodeMap["verify_code_2"] = uiTip_.verify_code_2;
        verifyCodeMap["verify_code_3"] = uiTip_.verify_code_3;
        verifyCodeMap["verify_code_4"] = uiTip_.verify_code_4;
        verifyCodeMap["verify_code_5"] = uiTip_.verify_code_5;
        verifyCodeMap["verify_code_6"] = uiTip_.verify_code_6;
        verifyCodeMap["verify_code_7"] = uiTip_.verify_code_7;
        verifyCodeMap["verify_code_8"] = uiTip_.verify_code_8;
        verifyCodeMap["verify_code_14"] = uiTip_.verify_code_14;

        sn.gameObject.SetActive(false);
    }

    IEnumerator Start()
    {
        // 调整画质
        yield return adjustGraphics();
        // 加载皮肤
        yield return applySkin();

        canvas.gameObject.SetActive(true);

        txtError.text = "";
        string deviceCode = Constant.DeviceCode;
        UnityLogger.Singleton.Info("deviceCode: {0}", deviceCode);
        applyDeviceCode(deviceCode);

        if (Constant.Platform == RuntimePlatform.WindowsEditor || Constant.Platform == RuntimePlatform.WebGLPlayer)
        {
            StartCoroutine(enterStartup(0));
            yield break;
        }

        Storage storage = new Storage();
        yield return storage.ReadBytesFromRoot("app.cer");
        if (200 != storage.statusCode)
        {
            txtError.text = uiTip_.license_not_found;
            UnityLogger.Singleton.Error(storage.error);
            yield break;
        }
        int expiry = 0;
        long timestamp = 0;
        int verifyCode = 0;
        bool pass = verifyLicense(storage.bytes, deviceCode, out verifyCode, out expiry, out timestamp);
        if (!pass)
        {
            txtError.text = verifyCodeMap[string.Format("verify_code_{0}", verifyCode)];
            yield break;
        }

        float delay = 2;
        if (expiry > 0)
        {
            System.DateTime created = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
            created = created.AddSeconds(timestamp);
            System.TimeSpan sp = System.DateTime.UtcNow.Subtract(created);
            int left = expiry - sp.Days;
            string expiryTip = uiTip_.expiry_left;
            txtError.text = expiryTip.Replace("??", left.ToString());
            delay = 6;
        }
        yield return enterStartup(delay);
    }

    private void Update()
    {
        if (Time.timeSinceLevelLoad - quitClikTimer_ > 1f)
        {
            quitClikCount_ = 0;
        }
    }

    private IEnumerator enterStartup(float _time)
    {
        yield return new WaitForSeconds(_time);

        if (null == activeVendor_)
        {
            txtError.text = uiTip_.vendor_active_none;
            yield break;
        }

        SceneManager.LoadScene("FrameworkUpdate");
    }

    private void applyDeviceCode(string _code)
    {
        Texture2D texture = new Texture2D(256, 256);
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = texture.height,
                Width = texture.width,
            }
        };
        Color32[] color32 = writer.Write(_code);
        texture.SetPixels32(color32);
        texture.Apply();

        //File.WriteAllText(Path.Combine(Constant.DataPath, "sn.out"), _code);

        UnityEngine.Events.UnityAction onTextCodeClick = () =>
        {
            if (quitClikCount_ > 15)
            {
                foreach (Transform child in sn.parent.transform)
                {
                    child.Find("txtCode").gameObject.SetActive(false);
                }
                return;
            }
            quitClikCount_ += 1;
            quitClikTimer_ = Time.timeSinceLevelLoad;
        };

        UnityEngine.Events.UnityAction onImageCodeClick = () =>
        {
            if (quitClikCount_ < 15)
                return;
            quitClikCount_ += 1;
            quitClikTimer_ = Time.timeSinceLevelLoad;
            if (quitClikCount_ > 30)
            {
                Debug.Log("QUIT");
                Application.Quit();
            }
        };

        int multiScreenColumn = null == activeVendor_ ? 1 : activeVendor_.schema.MultiScreenColumn;
        for (int i = 0; i < multiScreenColumn; ++i)
        {
            var clone = GameObject.Instantiate(sn, sn.parent);
            clone.gameObject.SetActive(true);
            var txtCode = clone.Find("txtCode");
            txtCode.GetComponent<Text>().text = _code;
            txtCode.GetComponent<Button>().onClick.AddListener(onTextCodeClick);
            var imgQRCode = clone.Find("imgQRCode");
            imgQRCode.GetComponent<RawImage>().texture = texture;
            imgQRCode.GetComponent<Button>().onClick.AddListener(onImageCodeClick);
        }
    }

    private bool verifyLicense(byte[] _bytes, string _deviceCode, out int _verifyCode, out int _expiry, out long _timestamp)
    {
        _expiry = 0;
        _timestamp = 0;
        _verifyCode = 1;

        if (null == _bytes)
        {
            return false;
        }

        List<string> lines = new List<string>();
        using (MemoryStream ms = new MemoryStream(_bytes))
        {
            using (StreamReader reader = new StreamReader(ms))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }
        }

        _verifyCode = License.Verify(lines.ToArray(), BusinessBranch.Security.AppKey, BusinessBranch.Security.AppSecret, _deviceCode);
        if (0 != _verifyCode)
        {
            return false;
        }

        _expiry = int.Parse(lines[7]);
        _timestamp = long.Parse(lines[5]);
        return true;
    }

    private IEnumerator adjustGraphics()
    {
        if (null == activeVendor_)
            yield break;

        // 设置画质
        Application.targetFrameRate = activeVendor_.schema.GraphicsFPS;
        QualitySettings.SetQualityLevel(activeVendor_.schema.GraphicsQuality);

        var resolution = Screen.currentResolution;
        int width = resolution.width;
        int height = resolution.height;
        // 获取最接近参考分辨率的分辨率
        if (activeVendor_.schema.GraphicsPixelResolution.Equals("auto"))
        {
            int min = int.MaxValue;
            foreach (var r in Screen.resolutions)
            {
                int d = 0;
                if (activeVendor_.schema.GraphicsReferenceResolutionMatch > 0.5)
                {
                    // 适配高度
                    d = Math.Abs(r.height - activeVendor_.schema.GraphicsReferenceResolutionHeight);
                }
                else
                {
                    // 适配宽度
                    d = Math.Abs(r.width - activeVendor_.schema.GraphicsReferenceResolutionWidth);
                }
                if (d <= min)
                {
                    min = d;
                    width = r.width;
                    height = r.height;
                }
            }
        }
        else if (activeVendor_.schema.GraphicsPixelResolution.Contains("x"))
        {
            string[] str = activeVendor_.schema.GraphicsPixelResolution.Split('x');
            if (!int.TryParse(str[0], out width))
                width = resolution.width;
            if (!int.TryParse(str[1], out height))
                height = resolution.height;
        }
        UnityLogger.Singleton.Info("adjust resolution to {0}x{1}", width, height);
        Screen.SetResolution(width, height, true);
    }

    private IEnumerator applySkin()
    {
        if (null == activeVendor_)
            yield break;

        SpriteStorage storage = new SpriteStorage();
        yield return storage.LoadFromVendor(activeVendor_.schema.SkinSplashBackground);
        if (null != storage.sprite)
        {
            imgBackground.sprite = storage.sprite;
        }
        yield return storage.LoadFromVendor(activeVendor_.schema.SkinSplashSlogan);
        if (null != storage.sprite)
        {
            imgSlogan.sprite = storage.sprite;
        }
    }
}

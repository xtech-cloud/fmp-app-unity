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
using UnityEngine.Networking;

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
}


public class Launcher : MonoBehaviour
{
    public Transform canvas;
    public Image imgBg;
    public Image imgSplash;
    public TextAsset tip;
    public Text txtDeviceCode;
    public Text txtError;
    public Text txtVersion;
    public RawImage imgQRCode;
    private Dictionary<string, string> tipMap = new Dictionary<string, string>();
    private Upgrade upgrade;
    private string vendor;

    // Start is called before the first frame update
    void Awake()
    {
        // 解析参数
        string[] commandLineArgs = System.Environment.GetCommandLineArgs();
        vendor = "data";
        foreach (string arg in commandLineArgs)
        {
            if (arg.StartsWith("-vendor="))
            {
                vendor = arg.Replace("-vendor=", "").Trim();
            }
            else if (arg.StartsWith("-resolution="))
            {
                string resolution = arg.Replace("-resolution=", "").Trim();
                adjustResolution(resolution);
            }
        }

        System.Action<string, Image> loadSprite = (_file, _image) =>
        {
            string path = Path.Combine(Application.persistentDataPath, vendor);
            path = Path.Combine(path, _file);
            if (!File.Exists(path))
                return;
            byte[] data = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(10, 10, TextureFormat.RGBA32, false);
            texture.LoadImage(data);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            _image.sprite = sprite;
        };
        loadSprite("bg.jpg", imgBg);
        loadSprite("splash.png", imgSplash);


        txtVersion.text = "ver " + Application.version;
        string file = Path.Combine(Application.persistentDataPath, "config.json");
        AppConfig.instance.MergeJson(file);
        Application.targetFrameRate = AppConfig.instance.schema.fps;
        QualitySettings.SetQualityLevel(AppConfig.instance.schema.quality);

        var uiTip = JsonUtility.FromJson<UiTip>(tip.text);
        tipMap["license_not_found"] = uiTip.license_not_found;
        tipMap["verify_code_1"] = uiTip.verify_code_1;
        tipMap["verify_code_2"] = uiTip.verify_code_2;
        tipMap["verify_code_3"] = uiTip.verify_code_3;
        tipMap["verify_code_4"] = uiTip.verify_code_4;
        tipMap["verify_code_5"] = uiTip.verify_code_5;
        tipMap["verify_code_6"] = uiTip.verify_code_6;
        tipMap["verify_code_7"] = uiTip.verify_code_7;
        tipMap["verify_code_8"] = uiTip.verify_code_8;
        tipMap["verify_code_14"] = uiTip.verify_code_14;
        tipMap["expiry_left"] = uiTip.expiry_left;
    }

    void Start()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            StartCoroutine(enterStartup(false));
            return;
        }

        txtError.text = "";
        string deviceCode = Constant.DeviceCode;
        Debug.LogFormat("deviceCode: {0}", deviceCode);
        applyDeviceCode(deviceCode);

        string licenseFile = Path.Combine(Application.persistentDataPath, "app.cer");
        if (File.Exists(licenseFile))
        {
            int expiry = 0;
            long timestamp = 0;
            int verifyCode = 0;
            bool pass = verifyLicense(licenseFile, deviceCode, out verifyCode, out expiry, out timestamp);
            if (!pass)
            {
                txtError.text = tipMap[string.Format("verify_code_{0}", verifyCode)];
                return;
            }

            bool delay = false;
            if (expiry > 0)
            {
                System.DateTime created = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
                created = created.AddSeconds(timestamp);
                System.TimeSpan sp = System.DateTime.UtcNow.Subtract(created);
                int left = expiry - sp.Days;
                string expiryTip = tipMap["expiry_left"];
                txtError.text = expiryTip.Replace("??", left.ToString());
                delay = true;
            }
            StartCoroutine(enterStartup(delay));
        }
        else
        {
            txtError.text = tipMap["license_not_found"];
        }
    }

    private IEnumerator enterStartup(bool _delay)
    {
        if (_delay)
        {
            yield return new WaitForSeconds(3.0f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        OnlineConfig onlineConfig = new OnlineConfig();
        yield return onlineConfig.Download();

        // 检查更新
        string upgradeFile = Path.Combine(Application.streamingAssetsPath, "upgrade.json");
        if (!File.Exists(upgradeFile))
        {
            SceneManager.LoadScene("startup");
            yield break;
        }

        upgrade = new Upgrade();
        upgrade.mono = this;
        upgrade.canvas = canvas;
        upgrade.vendor = vendor;
        upgrade.onFinish = () =>
        {
            SceneManager.LoadScene("startup");
        };
        string json = File.ReadAllText(upgradeFile);
        upgrade.ParseConfig(json);
        upgrade.Run();
    }

    private void applyDeviceCode(string _code)
    {
        txtDeviceCode.text = _code;
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
        imgQRCode.texture = texture;

        File.WriteAllText(Path.Combine(Application.persistentDataPath, "sn.out"), _code);
    }

    private bool verifyLicense(string _file, string _deviceCode, out int _verifyCode, out int _expiry, out long _timestamp)
    {
        _expiry = 0;
        _timestamp = 0;
        string[] lines = File.ReadAllLines(_file);
        _verifyCode = License.Verify(lines, BusinessBranch.Security.AppKey, BusinessBranch.Security.AppSecret, _deviceCode);
        if (0 != _verifyCode)
        {
            return false;
        }

        _expiry = int.Parse(lines[7]);
        _timestamp = long.Parse(lines[5]);
        return true;
    }

    private void adjustResolution(string _mode)
    {
        var resolution = Screen.currentResolution;
        int width = resolution.width;
        int height = resolution.height;
        // 获取最接近1080P的分辨率
        if (_mode.Equals("auto"))
        {
            int min = int.MaxValue;
            foreach (var r in Screen.resolutions)
            {
                // 适配高度
                int d = Math.Abs(r.height - 1080);
                if (d <= min)
                {
                    min = d;
                    width = r.width;
                    height = r.height;
                }
            }
        }
        else if (_mode.Contains("x"))
        {
            string[] str = _mode.Split('x');
            if (!int.TryParse(str[0], out width))
                width = resolution.width;
            if (!int.TryParse(str[1], out height))
                height = resolution.height;
        }
        Debug.LogFormat("adjust resolution to {0}x{1}", width, height);
        Screen.SetResolution(width, height, true);
    }

}

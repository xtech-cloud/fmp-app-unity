public class Vendor
{
    public string Uuid { get; set; } = "";
    public string Name { get; set; } = "";
    public string Display { get; set; } = "";
    public string SkinSplashBackground { get; set; } = "";
    public string SkinSplashSlogan { get; set; } = "";
    public int GraphicsFPS { get; set; }
    public int GraphicsQuality { get; set; }
    public string GraphicsPixelResolution { get; set; } = "";
    public int GraphicsReferenceResolutionWidth { get; set; }
    public int GraphicsReferenceResolutionHeight { get; set; }
    public float GraphicsReferenceResolutionMatch { get; set; }
    public string Application { get; set; } = "";
}

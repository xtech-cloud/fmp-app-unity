
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// 文件的结构
/// </summary>
public class FileSchema
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string path { get; set; }

    /// <summary>
    /// 文件哈希值
    /// </summary>
    public string hash { get; set; }

    /// <summary>
    /// 文件大小
    /// </summary>
    public long size { get; set; }

    /// <summary>
    /// 文件的可访问全路径
    /// </summary>
    public string url { get; set; }
}

/// <summary>
/// 资源包的meta结构
/// </summary>
public class BundleMetaSchema
{
    /// <summary>
    /// 路径是否匹配表达式
    /// </summary>
    /// <param name="_contentUri">内容的短路径</param>
    /// <param name="_pattern">正则表达式</param>
    /// <returns>是否匹配</returns>
    public static bool IsMatch(string _contentUri, string _pattern)
    {
        Regex regex = new Regex(_pattern);
        return regex.IsMatch(_contentUri);
    }

    /// <summary>
    /// 包的Uuid
    /// </summary>
    public string Uuid { get; set; } = "";

    /// <summary>
    /// 包的名称
    /// </summary>
    public string name { get; set; } = "";

    /// <summary>
    /// 包的简介
    /// </summary>
    public string summary { get; set; } = "";

    /// <summary>
    /// 简介的多国语言
    /// </summary>
    public Dictionary<string, string> summary_i18nS { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// 预设标签
    /// </summary>
    public string[] labelS { get; set; } = new string[0];

    /// <summary>
    /// 自定义标签
    /// </summary>
    public string[] tagS { get; set; } = new string[0];

    /// <summary>
    /// 资源的列表
    /// </summary>
    public FileSchema[] resourceS { get; set; } = new FileSchema[0];

    /// <summary>
    /// 内容的UUID的列表
    /// </summary>
    public string[] foreign_content_uuidS { get; set; } = new string[0];
} //class

/// <summary>
/// 资源内容的meta结构
/// </summary>
public class ContentMetaSchema
{
    /// <summary>
    /// 内容的uuid
    /// </summary>
    public string Uuid { get; set; } = "";

    /// <summary>
    /// 内容的名称
    /// </summary>
    public string name { get; set; } = "";

    /// <summary>
    /// 键值对
    /// </summary>
    public Dictionary<string, string> kvS { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// 别名
    /// </summary>
    public string alias { get; set; } = "";

    /// <summary>
    /// 主标题
    /// </summary>
    public string title { get; set; } = "";

    /// <summary>
    /// 副标题
    /// </summary>
    public string caption { get; set; } = "";

    /// <summary>
    /// 标签
    /// </summary>
    public string label { get; set; } = "";

    /// <summary>
    /// 标语
    /// </summary>
    public string topic { get; set; } = "";

    /// <summary>
    /// 说明描述
    /// </summary>
    public string description { get; set; } = "";

    /// <summary>
    /// 别名的多国语言
    /// </summary>
    public Dictionary<string, string> alias_i18nS = new Dictionary<string, string>();

    /// <summary>
    /// 主标题的多国语言
    /// </summary>
    public Dictionary<string, string> title_i18nS = new Dictionary<string, string>();

    /// <summary>
    /// 副标题的多国语言
    /// </summary>
    public Dictionary<string, string> caption_i18nS = new Dictionary<string, string>();

    /// <summary>
    /// 标签的多国语言
    /// </summary>
    public Dictionary<string, string> label_i18nS = new Dictionary<string, string>();

    /// <summary>
    /// 标语的多国语言
    /// </summary>
    public Dictionary<string, string> topic_i18nS = new Dictionary<string, string>();

    /// <summary>
    /// 说明描述的多国语言
    /// </summary>
    public Dictionary<string, string> description_i18nS = new Dictionary<string, string>();

    /// <summary>
    /// 预设标签
    /// </summary>
    public string[] labelS = new string[0];

    /// <summary>
    /// 自定义标签
    /// </summary>
    public string[] tagS = new string[0];

    /// <summary>
    /// 包的uuid
    /// </summary>
    public string foreign_bundle_uuid { get; set; } = "";

    /// <summary>
    /// 附件的列表
    /// </summary>
    public FileSchema[] AttachmentS { get; set; } = new FileSchema[0];
} //class


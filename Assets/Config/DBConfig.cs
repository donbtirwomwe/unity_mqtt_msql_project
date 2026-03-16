using UnityEngine;

[CreateAssetMenu(fileName = "DBConfig", menuName = "Config/DBConfig")]
public class DBConfig : ScriptableObject
{
    public string serverIp = "192.168.1.50";
    public int port = 1433;
    public string database = "IndustrialAssets";
    public string userId = "sa";
    [TextArea]
    public string password = "Industrial@Demo2026!";
    public bool trustServerCertificate = true;
    // MQTT settings (can be same as serverIp or different)
    public string mqttServerIp = "192.168.1.50";
    public int mqttPort = 1884;

    [Header("Data File Link Settings")]
    [Tooltip("If true, relative DATAFILES.Link values resolve to \\\\serverIp\\serverFileShareName first (Windows SMB share path).")]
    public bool useServerShareForRelativeLinks = false;

    [Tooltip("SMB share name on the DB host (for example mqtt-project if exported as a Windows share).")]
    public string serverFileShareName = "mqtt-project";

    [Tooltip("Base local folder used to resolve relative DATAFILES.Link values.")]
    public string localFileRoot = "";

    [Tooltip("Optional web root (for example SharePoint) used when preferWebFileLinks is enabled.")]
    public string webFileRoot = "http://192.168.1.50:8080";

    [Tooltip("If enabled and webFileRoot is set, relative links resolve to webFileRoot instead of localFileRoot.")]
    public bool preferWebFileLinks = true;
}

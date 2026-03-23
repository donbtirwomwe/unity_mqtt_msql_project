using UnityEngine;

public class DBConfigHolder : MonoBehaviour
{
    public DBConfig config;

    [Tooltip("If true, prints config values to the console at Start().")]
    public bool logOnStart = true;

    void Start()
    {
        if (config == null)
        {
            Debug.LogError("DBConfig not assigned on " + gameObject.name);
            return;
        }

        if (logOnStart)
        {
            string resolvedServerIp = config.GetResolvedServerIp();
            string resolvedDatabase = config.GetResolvedDatabase();
            string resolvedMqttIp = config.GetResolvedMqttServerIp();
            bool webMode = config.preferWebFileLinks && !string.IsNullOrWhiteSpace(config.webFileRoot);
            bool serverShareMode = !webMode
                && config.useServerShareForRelativeLinks
                && !string.IsNullOrWhiteSpace(resolvedServerIp)
                && !string.IsNullOrWhiteSpace(config.serverFileShareName);

            string linkMode = webMode ? "web" : (serverShareMode ? "server-share" : "local");
            string linkRoot = webMode
                ? config.webFileRoot
                : (serverShareMode ? "\\" + resolvedServerIp + "\\" + config.serverFileShareName : config.localFileRoot);
            Debug.Log($"DB server: {resolvedServerIp}:{config.port} DB: {resolvedDatabase} MQTT:{resolvedMqttIp}:{config.mqttPort} FileLinks:{linkMode} Root:{linkRoot}");
        }
    }
}

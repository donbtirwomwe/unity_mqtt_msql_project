using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "DBConfig", menuName = "Config/DBConfig")]
public class DBConfig : ScriptableObject
{
    public string serverIp = "127.0.0.1";
    public int port = 1433;
    public string database = "IndustrialAssets";
    public string userId = "app_reader";
    [TextArea]
    public string password = "";
    public bool trustServerCertificate = true;
    // MQTT settings (can be same as serverIp or different)
    public string mqttServerIp = "127.0.0.1";
    public int mqttPort = 1884;
    public string mqttUserName = "";
    [TextArea]
    public string mqttPassword = "";

    [Header("Data File Link Settings")]
    [Tooltip("If true, relative DATAFILES.Link values resolve to \\\\serverIp\\serverFileShareName first (Windows SMB share path).")]
    public bool useServerShareForRelativeLinks = false;

    [Tooltip("SMB share name on the DB host (for example mqtt-project if exported as a Windows share).")]
    public string serverFileShareName = "mqtt-project";

    [Tooltip("Base local folder used to resolve relative DATAFILES.Link values.")]
    public string localFileRoot = "";

    [Tooltip("Optional web root (for example SharePoint) used when preferWebFileLinks is enabled.")]
    public string webFileRoot = "";

    [Tooltip("If enabled and webFileRoot is set, relative links resolve to webFileRoot instead of localFileRoot.")]
    public bool preferWebFileLinks = true;

    private Dictionary<string, string> cachedEnvValues;
    private bool envLoaded;

    public string GetResolvedServerIp()
    {
        return ResolveValue(serverIp, "APP_DB_HOST", "DB_SERVER_IP");
    }

    public string GetResolvedDatabase()
    {
        return ResolveValue(database, "SQL_DATABASE");
    }

    public string GetResolvedUserId()
    {
        string resolved = ResolveValue(userId, "APP_DB_USER");
        return string.IsNullOrWhiteSpace(resolved) ? "app_reader" : resolved;
    }

    public string GetResolvedPassword()
    {
        return ResolveValue(password, "APP_DB_PASSWORD");
    }

    public string GetResolvedMqttServerIp()
    {
        string resolved = ResolveValue(mqttServerIp, "MQTT_HOST", "MQTT_SERVER_IP");
        return string.IsNullOrWhiteSpace(resolved) ? GetResolvedServerIp() : resolved;
    }

    public string GetResolvedMqttUserName()
    {
        return ResolveValue(mqttUserName, "MQTT_APP_USERNAME");
    }

    public string GetResolvedMqttPassword()
    {
        return ResolveValue(mqttPassword, "MQTT_APP_PASSWORD");
    }

    public string BuildConnectionString()
    {
        return $"Server={GetResolvedServerIp()},{port};Database={GetResolvedDatabase()};User Id={GetResolvedUserId()};Password={GetResolvedPassword()};TrustServerCertificate={trustServerCertificate};";
    }

    private string ResolveValue(string configuredValue, params string[] envKeys)
    {
        if (!string.IsNullOrWhiteSpace(configuredValue))
            return configuredValue;

        EnsureEnvLoaded();
        foreach (string envKey in envKeys)
        {
            if (string.IsNullOrWhiteSpace(envKey))
                continue;

            string environmentValue = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrWhiteSpace(environmentValue))
                return environmentValue;

            if (cachedEnvValues != null && cachedEnvValues.TryGetValue(envKey, out string fileValue) && !string.IsNullOrWhiteSpace(fileValue))
                return fileValue;
        }

        return configuredValue;
    }

    private void EnsureEnvLoaded()
    {
        if (envLoaded)
            return;

        envLoaded = true;
        cachedEnvValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                return;

            string envPath = Path.Combine(projectRoot, "containers", ".env");
            if (!File.Exists(envPath))
                envPath = Path.Combine(projectRoot, "containers", ".env.example");

            if (!File.Exists(envPath))
                return;

            foreach (string rawLine in File.ReadAllLines(envPath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                string[] parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                    continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();
                if (key.Length > 0)
                    cachedEnvValues[key] = value;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"DBConfig env resolution warning: {ex.Message}");
        }
    }
}

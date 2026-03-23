using UnityEngine;
using TMPro;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using System;
using System.Collections.Generic;

public class AssetController : MonoBehaviour
{
    public string serverIp = "127.0.0.1";
    public DBConfig dbConfig;
    public TMP_Text statusLabel;

    private string assetId;
    private MqttClient mqttClient;

    void Start()
    {
        assetId = gameObject.name;

        if (dbConfig == null)
            dbConfig = Resources.Load<DBConfig>("DBConfig");

        if (statusLabel == null)
        {
            Debug.LogError("Status Label not assigned!", this);
            return;
        }

        statusLabel.text = "Initializing...";
        LoadAssetData();
    }

    void LoadAssetData()
    {
        if (dbConfig == null)
        {
            if (statusLabel != null)
                statusLabel.text = "Missing DBConfig";
            Debug.LogError("DBConfig is required for AssetController.");
            return;
        }

        string conString = dbConfig.BuildConnectionString();

        try
        {
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("dbo.usp_GetAssetTopics", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", assetId);
                    using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            List<string> topics = new List<string>();
                            string assetName = "";

                            while (reader.Read())
                            {
                                assetName = reader["Name"].ToString();
                                string topic = reader["RealTopicPath"] != null && reader["RealTopicPath"] != DBNull.Value
                                    ? reader["RealTopicPath"].ToString()
                                    : reader["Target"].ToString();
                                if (!string.IsNullOrWhiteSpace(topic))
                                    topics.Add(topic);
                            }

                            statusLabel.text = $"{assetName}: Online";
                            ConnectToMqtt(topics.ToArray());
                        }
                        else
                        {
                            statusLabel.text = $"ID '{assetId}' Not Found";
                            Debug.LogWarning($"No results for Asset ID: {assetId}");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (statusLabel != null)
                statusLabel.text = "DB Connection Fail";
            Debug.LogError($"SQL Error: {e.Message}");
        }
    }

    void ConnectToMqtt(string[] topics)
    {
        try
        {
            int mqttPort = (dbConfig != null) ? dbConfig.mqttPort : 1884;
            string mqttServer = (dbConfig != null) ? dbConfig.GetResolvedMqttServerIp() : serverIp;
            mqttClient = new MqttClient(mqttServer, mqttPort, false, null, null, MqttSslProtocols.None);

            mqttClient.MqttMsgPublishReceived += (s, e) =>
            {
                string msg = Encoding.UTF8.GetString(e.Message);
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (statusLabel != null)
                        statusLabel.text = msg;
                });
            };

            string clientId = Guid.NewGuid().ToString();
            string mqttUserName = dbConfig != null ? dbConfig.GetResolvedMqttUserName() : null;
            string mqttPassword = dbConfig != null ? dbConfig.GetResolvedMqttPassword() : null;
            if (!string.IsNullOrWhiteSpace(mqttUserName))
                mqttClient.Connect(clientId, mqttUserName, mqttPassword ?? string.Empty);
            else
                mqttClient.Connect(clientId);

            byte[] qos = new byte[topics.Length];
            for (int i = 0; i < topics.Length; i++) qos[i] = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;

            if (topics != null && topics.Length > 0)
                mqttClient.Subscribe(topics, qos);
            Debug.Log($"Subscribed to {topics.Length} topics.");
        }
        catch (Exception e)
        {
            Debug.LogError($"MQTT Error: {e.Message}");
        }
    }

    void OnDestroy()
    {
        try
        {
            if (mqttClient != null)
            {
                if (mqttClient.IsConnected)
                {
                    mqttClient.Disconnect();
                }
                mqttClient = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error during MQTT cleanup: {e.Message}");
        }
    }
}
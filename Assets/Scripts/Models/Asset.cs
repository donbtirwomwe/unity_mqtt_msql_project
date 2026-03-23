using System;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;

public class Asset
{
    public string id;
    public string name;
    public string description;
    public string messagingRole;
    public int? messagingRoleCode;
    public StatusType status = StatusType.Normal;
    public List<DataPoint> dataPoints = new List<DataPoint>();

    public Asset() { }

    public Asset(string id)
    {
        this.id = id;
    }

    // Pulls Name & description from ASSETS table
    public void GetData(string conString)
    {
        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                using (var cmd = new System.Data.SqlClient.SqlCommand("dbo.usp_GetAssetDetails", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            this.name = r["Name"].ToString();
                            this.description = r["Description"].ToString();
                            this.status = (StatusType)r.GetInt32(r.GetOrdinal("Status"));

                            this.messagingRole = r["MessagingRole"] != null && r["MessagingRole"] != DBNull.Value
                                ? r["MessagingRole"].ToString()
                                : null;

                            if (r["MessagingRoleCode"] != null && r["MessagingRoleCode"] != DBNull.Value)
                                this.messagingRoleCode = Convert.ToInt32(r["MessagingRoleCode"]);
                            else
                                this.messagingRoleCode = null;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Asset.GetData error: {e.Message}");
        }
    }

    // Fills dataPoints list with matching DATAPOINTS entries
    public void GetDataPoints(string conString)
    {
        dataPoints.Clear();
        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                using (var cmd = new System.Data.SqlClient.SqlCommand("dbo.usp_GetAssetDataPoints", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@assetId", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var dp = new DataPoint(r["ID"]?.ToString())
                            {
                                name = r["Name"]?.ToString(),
                                description = r["Description"]?.ToString()
                            };
                            dataPoints.Add(dp);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Asset.GetDataPoints error: {e.Message}");
        }
    }

    // Populate all asset data including datapoints
    public void PopulateData(string conString)
    {
        GetData(conString);
        GetDataPoints(conString);
    }

    // converts string code from DB into StatusType enum
    private StatusType ParseStatus(string code)
    {
        if (string.IsNullOrEmpty(code)) return StatusType.Normal;
        if (Enum.TryParse<StatusType>(code, true, out var s))
            return s;
        return StatusType.Normal;
    }

    // Subscribe to all channels in all datapoints of this asset
    public void SubscribeAllChannels(MqttClient mqttClient)
    {
        try
        {
            foreach (var dp in dataPoints)
            {
                dp.SubscribeChannels(mqttClient);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Asset.SubscribeAllChannels error: {e.Message}");
        }
    }

    // Get all subscribed MQTT topics across all datapoints
    public List<string> GetSubscribedTopics()
    {
        var topics = new List<string>();
        foreach (var dp in dataPoints)
        {
            foreach (var ch in dp.channels)
            {
                if (!string.IsNullOrEmpty(ch.realTopicPath))
                    topics.Add(ch.realTopicPath);
            }
        }
        return topics;
    }

    // Get all files across all datapoints
    public List<DataFile> GetAllFiles()
    {
        var allFiles = new List<DataFile>();
        foreach (var dp in dataPoints)
        {
            allFiles.AddRange(dp.files);
        }
        return allFiles;
    }

    // Get summary for UI display
    public string GetSummary()
    {
        return $"{name} - {dataPoints.Count} datapoints, {GetAllFiles().Count} files, {GetSubscribedTopics().Count} channels";
    }
}

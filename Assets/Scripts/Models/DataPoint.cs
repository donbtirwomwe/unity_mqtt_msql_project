using System;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;

public class DataPoint
{
    public string id;
    public string name;
    public string description;
    public StatusType status = StatusType.Normal;
    public List<DataFile> files = new List<DataFile>();
    public List<Channel> channels = new List<Channel>();

    public DataPoint() { }

    public DataPoint(string id)
    {
        this.id = id;
    }

    // Pulls Name & description from DATAPOINTS for this dataPoint and asset
    public void GetData(string conString, string assetId)
    {
        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                using (var cmd = new System.Data.SqlClient.SqlCommand("dbo.usp_GetDataPointDetails", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            name = r["Name"]?.ToString();
                            description = r["Description"]?.ToString();
                            status = ParseStatus(r["Status"]?.ToString());
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DataPoint.GetData error: {e.Message}");
        }
    }

    // Fill files list for this datapoint
    public void GetFiles(string conString, string assetId, DBConfig config = null)
    {
        files.Clear();
        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                using (var cmd = new System.Data.SqlClient.SqlCommand("dbo.usp_GetDataPointFiles", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    cmd.Parameters.AddWithValue("@dpid", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var f = new DataFile(
                                r["ID"]?.ToString(),
                                r["Name"]?.ToString(),
                                r["Description"]?.ToString(),
                                r["Type"]?.ToString(),
                                DataFile.ResolveLink(r["Link"]?.ToString(), config)
                            );
                            files.Add(f);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DataPoint.GetFiles error: {e.Message}");
        }
    }

    // Fill channels list for this datapoint
    public void GetChannels(string conString, string assetId)
    {
        channels.Clear();
        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                using (var cmd = new System.Data.SqlClient.SqlCommand("dbo.usp_GetDataPointChannels", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@assetId", assetId);
                    cmd.Parameters.AddWithValue("@dpid", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var c = new Channel(
                                r["ID"]?.ToString(),
                                r["Name"]?.ToString(),
                                r["Description"]?.ToString(),
                                r["Target"]?.ToString(),
                                r["RealTopicPath"]?.ToString()
                            );
                            channels.Add(c);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DataPoint.GetChannels error: {e.Message}");
        }
    }

    // Calls update on all channels using the provided value resolver
    public void UpdateChannels(IValueProvider provider)
    {
        try
        {
            foreach (var c in channels)
            {
                c.UpdateChannel(provider);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DataPoint.UpdateChannels error: {e.Message}");
        }
    }

    // converts string code from DB into StatusType enum
    private StatusType ParseStatus(string code)
    {
        if (string.IsNullOrEmpty(code)) return StatusType.Normal;
        if (Enum.TryParse<StatusType>(code, true, out var s))
            return s;
        return StatusType.Normal;
    }

    // Populate all items (files and channels) for this datapoint when clicked
    public void PopulateItems(string conString, string assetId, DBConfig config = null)
    {
        GetData(conString, assetId);
        GetFiles(conString, assetId, config);
        GetChannels(conString, assetId);
    }

    // Subscribe to all channels in this datapoint
    public void SubscribeChannels(MqttClient mqttClient)
    {
        try
        {
            foreach (var c in channels)
            {
                c.Subscribe(mqttClient);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DataPoint.SubscribeChannels error: {e.Message}");
        }
    }

    // Unsubscribe from all channels in this datapoint
    public void UnsubscribeChannels(MqttClient mqttClient)
    {
        try
        {
            foreach (var c in channels)
            {
                c.Unsubscribe(mqttClient);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DataPoint.UnsubscribeChannels error: {e.Message}");
        }
    }

    // Get all MQTT topics for this datapoint
    public List<string> GetChannelTopics()
    {
        var topics = new List<string>();
        foreach (var ch in channels)
        {
            if (!string.IsNullOrEmpty(ch.realTopicPath))
                topics.Add(ch.realTopicPath);
        }
        return topics;
    }

    // Get file links as a formatted list
    public List<string> GetFileLinks()
    {
        var links = new List<string>();
        foreach (var f in files)
        {
            if (!string.IsNullOrEmpty(f.link))
                links.Add(f.link);
        }
        return links;
    }

    // Get summary for UI display
    public string GetSummary()
    {
        return $"{name} - {files.Count} files, {channels.Count} channels";
    }
}

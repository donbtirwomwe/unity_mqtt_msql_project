using System;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class Channel
{
    public string id;
    public string name;
    public string description;
    public string target;
    public string realTopicPath;
    public string value;

    public Channel() { }

    public Channel(string id, string name, string description, string target, string realTopicPath = null)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.target = target;
        this.realTopicPath = realTopicPath ?? target;
    }

    public void UpdateChannel(IValueProvider provider)
    {
        try
        {
            if (provider == null) return;
            value = provider.GetValue(realTopicPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Channel.UpdateChannel error for target '{target}': {e.Message}");
        }
    }

    public void Subscribe(MqttClient mqttClient)
    {
        try
        {
            if (mqttClient == null || string.IsNullOrEmpty(realTopicPath)) return;
            if (!mqttClient.IsConnected) return;

            byte[] qos = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };
            mqttClient.Subscribe(new string[] { realTopicPath }, qos);
            Debug.Log($"Subscribed to channel '{id}'.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Channel.Subscribe error for target '{target}': {e.Message}");
        }
    }

    public void Unsubscribe(MqttClient mqttClient)
    {
        try
        {
            if (mqttClient == null || string.IsNullOrEmpty(realTopicPath)) return;
            if (!mqttClient.IsConnected) return;

            mqttClient.Unsubscribe(new string[] { realTopicPath });
            Debug.Log($"Unsubscribed from channel '{id}'.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Channel.Unsubscribe error for target '{target}': {e.Message}");
        }
    }
}

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
    public string value;

    public Channel() { }

    public Channel(string id, string name, string description, string target)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.target = target;
    }

    public void UpdateChannel(IValueProvider provider)
    {
        try
        {
            if (provider == null) return;
            value = provider.GetValue(target);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Channel.UpdateChannel error for target '{target}': {e.Message}");
        }
    }

    // Subscribe to this channel's target topic via MQTT
    public void Subscribe(MqttClient mqttClient)
    {
        try
        {
            if (mqttClient == null || string.IsNullOrEmpty(target)) return;
            if (!mqttClient.IsConnected) return;

            byte[] qos = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };
            mqttClient.Subscribe(new string[] { target }, qos);
            Debug.Log($"Subscribed to channel topic: {target}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Channel.Subscribe error for target '{target}': {e.Message}");
        }
    }

    // Unsubscribe from this channel's topic
    public void Unsubscribe(MqttClient mqttClient)
    {
        try
        {
            if (mqttClient == null || string.IsNullOrEmpty(target)) return;
            if (!mqttClient.IsConnected) return;

            mqttClient.Unsubscribe(new string[] { target });
            Debug.Log($"Unsubscribed from channel topic: {target}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Channel.Unsubscribe error for target '{target}': {e.Message}");
        }
    }
}

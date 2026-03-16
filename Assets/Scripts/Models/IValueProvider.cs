using System;
using System.Collections.Generic;

public interface IValueProvider
{
    // Return a string value for the given target code (e.g. MQTT topic or websocket target)
    string GetValue(string target);
}

// Example MQTT-based provider that caches latest values from subscribed topics
public class MqttValueProvider : IValueProvider
{
    private Dictionary<string, string> _latestValues = new Dictionary<string, string>();

    public void UpdateValue(string topic, string value)
    {
        _latestValues[topic] = value;
    }

    public string GetValue(string target)
    {
        return _latestValues.TryGetValue(target, out var val) ? val : null;
    }
}

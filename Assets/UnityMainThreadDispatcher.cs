using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class UnityMainThreadDispatcher : MonoBehaviour {
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;
    public static UnityMainThreadDispatcher Instance() => _instance;

    void Awake() { if (_instance == null) _instance = this; }
    void Update() {
        lock(_executionQueue) {
            while (_executionQueue.Count > 0) _executionQueue.Dequeue().Invoke();
        }
    }
    public void Enqueue(Action action) { lock (_executionQueue) { _executionQueue.Enqueue(action); } }
}
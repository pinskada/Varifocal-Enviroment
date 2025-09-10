using System;
using System.Collections.Concurrent;
using UnityEngine;
using Contracts;

public class MainThreadQueue : MonoBehaviour, IMainThreadQueue
{
    /* 
    This class provides a thread-safe queue to schedule actions to be executed on the main Unity thread.
    */

    // Thread-safe queue to hold actions
    private static readonly ConcurrentQueue<Action> q = new();

    void Update()
    {
        Pump(); // runs any work enqueued by background threads
    }


    // Enqueue an action to be executed on the main thread
    public void Enqueue(Action a)
    {
        if (a != null) q.Enqueue(a);
    }


    // Call this once per frame on the main thread (e.g., from NetworkManager.Update)
    public static void Pump()
    {
        while (q.TryDequeue(out var a))
        {
            try { a(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}

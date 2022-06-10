using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkTransport.Utils
{
    public class MainThreadUnityLogger : MonoBehaviour, ILogger
    {
        private readonly Queue<string> _queueWithLogs = new Queue<string>();
        private readonly Queue<string> _queueWithWarnings = new Queue<string>();
        private readonly Queue<string> _queueWithErrors = new Queue<string>();
        private readonly Queue<Exception> _queueWithExceptions = new Queue<Exception>();

        public void Log(string msg)
        {
            _queueWithLogs.Enqueue(msg);
        }

        public void LogWarning(string msg)
        {
            _queueWithWarnings.Enqueue(msg);
        }

        public void LogError(string msg)
        {
            _queueWithErrors.Enqueue(msg);
        }

        public void LogException(Exception exc)
        {
            _queueWithExceptions.Enqueue(exc);
        }

        private void Update()
        {
            while (_queueWithLogs.Count > 0)
            {
                Debug.Log(_queueWithLogs.Dequeue());
            }

            while (_queueWithWarnings.Count > 0)
            {
                Debug.LogWarning(_queueWithWarnings.Dequeue());
            }

            while (_queueWithErrors.Count > 0)
            {
                Debug.LogError(_queueWithErrors.Dequeue());
            }

            while (_queueWithExceptions.Count > 0)
            {
                Debug.LogException(_queueWithExceptions.Dequeue());
            }                     
        }
    }
}



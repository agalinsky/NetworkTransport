using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using NetworkTransport.Utils;
using NetworkTransport.Pools;

namespace NetworkTransport.Tests
{
    [DefaultExecutionOrder(-1)]
    public class TestServer : MonoBehaviour
    {
        BufferPool<NetworkBuffer> _bufferPool;
        ThreadSocket _socket;
        public MainThreadUnityLogger logger;
        public string serverName;
        public Text textHistory;
        public int port;

        void Start()
        {
            InitSocket();                        
        }

        private void Update()
        {
            // Test messanger-like logic
            ResendMessage();

            // Test server broadcasting
            //SendMsg("Server is broadcasting...");

            // Test allocation free send-recieve byte data.
            //ResendSumValue();
        }

        private void InitSocket()
        {
            _bufferPool = new BufferPool<NetworkBuffer>();
            var serverEP = NetworkConfig.ServerAddress;
            logger.Log($"Server {serverName} EP {serverEP}");
            _socket = new ThreadSocket(_bufferPool, logger);
            _socket.Bind(serverEP);
            _socket.Run();
        }

        private void OnDestroy()
        {
            _socket.Dispose();
        }

        private void SendMsg(string msg)
        {
            Profiler.BeginSample("Server Side String to Bytes");
            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
            Profiler.EndSample();

            var buffer = _bufferPool.Get(NetworkConfig.MTU);
            Array.Copy(data, 0, buffer.buffer, PacketHeader.HeaderLength, data.Length);
            buffer.payload = buffer.offset = data.Length + PacketHeader.HeaderLength;
            _socket.EnqueueForSend(buffer);
        }

        private void ResendMessage()
        {
            NetworkBuffer netBuffer = null;
            while (_socket.ReceiveFromQueue(ref netBuffer))
            {
                if (netBuffer == null)
                {
                    throw new NullReferenceException("NetworkBuffer is null.");
                }

                Profiler.BeginSample("Server Received String to Bytes");
                string msg = System.Text.Encoding.UTF8.GetString(netBuffer.buffer, netBuffer.offset, netBuffer.payload - PacketHeader.HeaderLength);
                Profiler.EndSample();

                logger.Log($"Server Received msg {msg}");
                textHistory.text += $"\n{serverName}>>> " + msg;

                _bufferPool.Put(netBuffer);

                SendMsg(msg);
            }
        }

        private void ResendSumValue()
        {
            int sum = 0;

            NetworkBuffer netBuffer = null;
            while (_socket.ReceiveFromQueue(ref netBuffer))
            {
                if (netBuffer == null)
                {
                    throw new NullReferenceException("NetworkBuffer is null.");
                }

                int val = netBuffer.buffer[netBuffer.payload - 1];

                //logger.Log($"{serverName} received: {val}");

                sum += val;

                _bufferPool.Put(netBuffer);
            }

            var buffer = _bufferPool.Get(PacketHeader.HeaderLength + 1);
            buffer.buffer[buffer.offset++] = (byte) sum;
            _socket.EnqueueForSend(buffer);
        }
    }
}

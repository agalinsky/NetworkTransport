using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using NetworkTransport.Pools;
using NetworkTransport.Utils;

namespace NetworkTransport.Tests
{    
    public class TestClient : MonoBehaviour
    {
        ThreadSocket _socket;
        BufferPool<NetworkBuffer> _bufferPool;
        public MainThreadUnityLogger logger;
        public string clientName;
        public InputField input;
        public int port;
        public Text textHistory;
        public int connDelayMs;
        public bool isInit;
        public int sendValue;

        private async void Start()
        {
            await DelayBeforeConnect();
            isInit = true;
            input.onEndEdit.AddListener(OnReceiveInput);
            InitSocket();
        }

        private Task DelayBeforeConnect()
        {
            return Task.Delay(connDelayMs);
        }

        private void Update()
        {
            if (!isInit) return;

            // Test messanger-like logic
            ReceiveMessage();

            // Test allocation free send-recieve byte data.
            //SendValue(sendValue);
            //ReceiveSumValue();
        }

        private void InitSocket()
        {
            _bufferPool = new BufferPool<NetworkBuffer>();
            var clientEP = new IPEndPoint(IPAddress.Loopback, port);
            logger.Log($"Client {clientName} EndPoint {clientEP}");
            _socket = new ThreadSocket(_bufferPool, logger);
            _socket.Bind(clientEP); // listen local ep
            _socket.AddConnection(NetworkConfig.ServerAddress.Address, NetworkConfig.ServerAddress.Port); // remote ep
            _socket.Run();
        }

        private void OnReceiveInput(string msg)
        {
            SendMsg(msg);
        }

        private void SendMsg(string msg)
        {
            Profiler.BeginSample("InputFiled String Bytes");
            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
            Profiler.EndSample();

            var buffer = _bufferPool.Get(NetworkConfig.MTU);
            Array.Copy(data, 0, buffer.buffer, PacketHeader.HeaderLength, data.Length);
            buffer.offset += data.Length;
            buffer.payload += data.Length;
            _socket.EnqueueForSend(buffer);
        }

        private void OnDestroy()
        {
            _socket.Dispose();
        }

        private void SendValue(int value)
        {
            var buffer = _bufferPool.Get(PacketHeader.HeaderLength + 1);
            buffer.buffer[buffer.offset++] = (byte)value;
            buffer.payload += 1;
            _socket.EnqueueForSend(buffer);
        }

        private void ReceiveSumValue()
        {
            NetworkBuffer netBuffer = null;
            while (_socket.ReceiveFromQueue(ref netBuffer))
            {
                if (netBuffer == null)
                {
                    throw new NullReferenceException("NetworkBuffer is null.");
                }

                int value = netBuffer.buffer[netBuffer.offset];

                //logger.Log($"{clientName} received: {value}");

                _bufferPool.Put(netBuffer);
            }
        }

        private void ReceiveMessage()
        {
            NetworkBuffer netBuffer = null;
            while (_socket.ReceiveFromQueue(ref netBuffer))
            {
                if (netBuffer == null)
                {
                    throw new NullReferenceException("NetworkBuffer is null.");
                }

                Profiler.BeginSample("Client Received String to Bytes");
                string msg = System.Text.Encoding.UTF8.GetString(netBuffer.buffer, netBuffer.offset, netBuffer.payload - PacketHeader.HeaderLength);
                Profiler.EndSample();

                logger.Log($"Client Received msg {msg}");
                if (textHistory.text.Length > 700)
                {
                    textHistory.text = "";
                }
                textHistory.text += $"\n{clientName}>>> " + msg;

                _bufferPool.Put(netBuffer);
            }
        }
    }
}

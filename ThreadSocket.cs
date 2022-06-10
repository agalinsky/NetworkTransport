#define LOGGER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using NetworkTransport.Pools;
using NetworkTransport.Utils;

namespace NetworkTransport
{
    /// <summary>
    /// Allocation free UDP socket using for server authoritative application.
    /// Supports only IPv4.
    /// </summary>
    public class ThreadSocket : IDisposable
    {
        private Socket _socket;
        private Thread _receiveThread;
        private readonly Queue<ReceivedNetworkPacket> _receiveQueue;
        private Thread _sendThread;
        private readonly Queue<SendNetworkPacket> _sendQueue;
        private readonly HashSet<NetworkConnection> _connections;
        private readonly BufferPool<NetworkBuffer> _bufferPool;
        private readonly ILogger _logger;

        public IPEndPoint LocalEndPoint { get; private set; }

        public ThreadSocket(BufferPool<NetworkBuffer> bufferPool, ILogger logger)
        {
            if (bufferPool == null)
            {
                throw new ArgumentNullException(nameof(bufferPool), "bufferPool is null");
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger), "logger is null");
            }

            _bufferPool = bufferPool;
            _logger = logger;

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _receiveThread = new Thread(ReceiveLoop);
            _receiveQueue = new Queue<ReceivedNetworkPacket>();
            _sendThread = new Thread(SendLoop);
            _sendQueue = new Queue<SendNetworkPacket>();
            _connections = new HashSet<NetworkConnection>();
        }

        public void Dispose()
        {
            _socket.Close();
            _socket = null;
            _receiveThread.Abort();
            _receiveThread = null;
            _sendThread.Abort();
            _sendThread = null;
        }

        /// <summary>
        /// Socket starts listening local end point.
        /// </summary>
        public void Bind(IPAddress address, int port)
        {
            if (address == null)
            {
                throw new NullReferenceException("IPAddress is null");
            }

            ExceptionUtility.CheckPortValidity(port);

            Bind(new IPEndPoint(address, port));
        }

        public void Bind(IPEndPoint localEndPoint)
        {
            if (localEndPoint == null)
            {
                throw new NullReferenceException("IPEndPoint is null");
            }

            LocalEndPoint = localEndPoint;
            _socket.Bind(LocalEndPoint);
        }

        /// <summary>
        /// Add remote EndPoint to local connections and send request to connect.
        /// </summary>
        public void AddConnection(IPAddress destAddress, int destPort)
        {
            if (LocalEndPoint == null)
            {
                throw new NullReferenceException($"Firstly set 'LocalEndPoint' via 'Bind' call before add remote connections.");
            }

            if (destAddress == null)
            {
                throw new NullReferenceException("IPAddress is null");
            }

            ExceptionUtility.CheckPortValidity(destPort);

            destAddress.CheckAddressFamily();

            var header = new PacketHeader(LocalEndPoint.Address, LocalEndPoint.Port,
                                          destAddress, destPort,
                                          ConnectionRequest.ConnectRequest);

            NetworkBuffer buffer = _bufferPool.Get(PacketHeader.HeaderLength);            
            header.WriteToBuffer(buffer.buffer);
            SendNetworkPacket packet = new SendNetworkPacket(buffer, true);

            EnqueueForSendInternal(packet);

            //TODO: Here should be network routine to get response that connection is confirmed...
            
            header.SetRequestState(ConnectionRequest.StayConnectedRequest);
            var connection = new NetworkConnection(header, ConnectionState.Connected);
            _connections.Add(connection);
        }

        public void Run()
        {
            _receiveThread.Start();
            _sendThread.Start();
        }

        /// <summary>
        /// Return NetworkBuffer to pool on your side when buffer will be read.
        /// </summary>
        public bool ReceiveFromQueue(ref NetworkBuffer buffer)
        {
            if (_receiveQueue.Count > 0)
            {
                buffer = _receiveQueue.Dequeue().networkBuffer;                
                return true;
            }
            return false;
        }

        private void ReceiveLoop()
        {
            while (true)
            {
                int receivedLength = 0;
                NetworkBuffer receivedBuffer = _bufferPool.Get(NetworkConfig.MTU);                

                if (receivedBuffer == null)
                {
                    throw new NullReferenceException("NetworkBuffer from BufferPool is null.");
                }

                ReceivedNetworkPacket receivedPacket = new ReceivedNetworkPacket(receivedBuffer);

                try
                {
                    receivedLength = _socket.Receive(receivedPacket.networkBuffer.buffer);                    
                }
                catch (SocketException se)
                {
                    _logger.LogException(se);
                }

                receivedPacket.networkBuffer.payload = receivedLength;

                if (ValidateRecievedPacket(ref receivedPacket))
                {
                    _receiveQueue.Enqueue(receivedPacket);

#if LOGGER
                    _logger.Log($"Received packet on local EP {LocalEndPoint}. Buffer length {receivedLength} bytes. Current queue length {_receiveQueue.Count}.");
#endif
                }
                else
                {
                    _bufferPool.Put(receivedPacket.networkBuffer);
                }
            }
        }

        public void EnqueueForSend(NetworkBuffer buffer)
        {
            EnqueueForSendInternal(new SendNetworkPacket(buffer, false));
        }

        private void EnqueueForSendInternal(SendNetworkPacket packet)
        {
            _sendQueue.Enqueue(packet);
        }

        private void SendLoop()
        {
            while (true)
            {
                if (_sendQueue.Count > 0)
                {
                    SendNetworkPacket packetToSend = _sendQueue.Dequeue();

                    if (packetToSend.networkBuffer == null)
                    {
                        throw new NullReferenceException("NetworkPacket has nullable buffer.");
                    }

                    if (packetToSend.networkBuffer.buffer.Length > NetworkConfig.MTU)
                    {
                        throw new NullReferenceException("NetworkBuffer length exceeds MTU.");
                    }

                    foreach (var conn in _connections)
                    {
                        if (packetToSend.isHeaderIncluded == false)
                        {
                            conn.header.WriteToBuffer(packetToSend.networkBuffer.buffer);
                        }

                        if (packetToSend.networkBuffer.offset == 0)
                        {
                            packetToSend.networkBuffer.offset = PacketHeader.HeaderLength;
                        }

                        var remoteEndPoint = conn.header.DestEndPoint;                        
                        int bufferLength = packetToSend.networkBuffer.offset == 0 ? packetToSend.networkBuffer.GetBufferLength() : packetToSend.networkBuffer.offset;

                        try
                        {                            
                            _socket.SendTo(packetToSend.networkBuffer.buffer, 0, bufferLength, SocketFlags.None, remoteEndPoint);
                        }
                        catch (SocketException se)
                        {
                            _logger.LogException(se);
                        }

#if LOGGER
                        _logger.Log($"Sent packet from local EP {LocalEndPoint} to remote EP {remoteEndPoint}. Payload length {packetToSend.networkBuffer.offset}. Send queue length {_sendQueue.Count}");
#endif
                    }

                    _bufferPool.Put(packetToSend.networkBuffer);
                }                
            }
        }

        /// <summary>
        /// Returns true if packet should be enqueued.
        /// </summary>
        private bool ValidateRecievedPacket(ref ReceivedNetworkPacket receivedPacket)
        {
            var receivedHeader = new PacketHeader();
            receivedHeader.ReadFromBuffer(receivedPacket.networkBuffer.buffer);

            // Try to find this connection
            foreach (var conn in _connections)
            {
                if (conn.header.destAddress.Equals(receivedHeader.sourceAddress)
                    && conn.header.destPort == receivedHeader.sourcePort)
                {
                    // Don't add packet to queue
                    if (conn.state == ConnectionState.Disconnected)
                    {
                        _connections.Remove(conn);
                        return false;
                    }

                    // Don't add packet to queue
                    if (conn.state == ConnectionState.Connected
                        && receivedHeader.requestType == ConnectionRequest.DisconnectRequest)
                    {
                        _connections.Remove(conn);
                        return false;
                    }

                    return true;
                }
            }

            // Don't add packet to queue
            if (receivedHeader.requestType == ConnectionRequest.ConnectRequest)
            {
                receivedHeader.SwipeSourceDest();
                receivedHeader.SetRequestState(ConnectionRequest.StayConnectedRequest);
                _connections.Add(new NetworkConnection(receivedHeader, ConnectionState.Connected));
#if LOGGER
                _logger.Log($"Socket {LocalEndPoint} received connection request from {receivedHeader.DestEndPoint}. Total connections {_connections.Count}.");
#endif
                return false;
            }

            return true;
        }
    }
}


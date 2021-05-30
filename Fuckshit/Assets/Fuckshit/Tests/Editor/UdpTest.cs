// helper class with udp connection setup

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Fuckshit.Tests
{
    public abstract class UdpTest
    {
        public int Port = 1337;

        // server
        public Socket serverSocket;

        // client
        public IPEndPoint clientRemoteEndPoint;
        public Socket clientSocket;

        [SetUp]
        public void SetUp()
        {
            // create server
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, Port));

            // create client
            clientRemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port);
            clientSocket = new Socket(clientRemoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            clientSocket.Connect(clientRemoteEndPoint);
            Thread.Sleep(100);

            // send hello
            ClientSend(new byte[]{0x12, 0x34});

            // server should have something to poll now
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
            bool result = ServerPoll(ref newClientEP, out ArraySegment<byte> _);
            Assert.That(result, Is.True);
        }

        public void ClientSend(byte[] data)
        {
            // send and wait a little bit for it to be delivered
            clientSocket.Send(data, data.Length, SocketFlags.None);
            Thread.Sleep(100);
        }

        public bool ServerPoll(ref EndPoint newClientEP, out ArraySegment<byte> message)
        {
            byte[] receiveBuffer = new byte[1200];
            if (serverSocket != null && serverSocket.Poll(0, SelectMode.SelectRead))
            {
                // get message
                int msgLength = serverSocket.ReceiveFrom_NonAlloc(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, out SocketAddress remoteAddress);
                Debug.Log($"ServerPoll from {newClientEP}:  {BitConverter.ToString(receiveBuffer, 0, msgLength)}");
                message = new ArraySegment<byte>(receiveBuffer, 0, msgLength);
                // convert SocketAddress to EndPoint again, just for tests
                newClientEP = newClientEP.Create(remoteAddress);
                return true;
            }
            return false;
        }

        [TearDown]
        public void TearDown()
        {
            serverSocket.Close();
            clientSocket.Close();
        }
    }
}
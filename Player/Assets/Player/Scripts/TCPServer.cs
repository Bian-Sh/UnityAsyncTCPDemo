using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using zFrame.ThreadEx;

public class TCPServer : IDisposable
{

    IPEndPoint endpoint;

    TcpListener listener;
    List<TcpClient> clients = new List<TcpClient>();

    volatile bool acceptLoop = true;
    public IReadOnlyList<TcpClient> Clients => clients;
    public TCPServer(IPEndPoint endpoint)
    {
        this.endpoint = endpoint;
    }

    public async Task Listen()
    {
        lock (this)
        {
            if (listener != null)
                throw new InvalidOperationException("Already started");
            acceptLoop = true;
            listener = new TcpListener(endpoint);
        }

        listener.Start();
        Debug.Log("[播放器] 开始监听！");
        while (acceptLoop)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                var _ = Task.Run(() => OnConnectClient(client));
            }
            catch (ObjectDisposedException e)
            {
                // thrown if the listener socket is closed
                throw e;
            }
            catch (SocketException e)
            {
                // Some socket error
                throw e;
            }
        }
    }

    public void Stop()
    {
        lock (this)
        {
            if (listener == null)
                throw new InvalidOperationException("Not started");
            acceptLoop = false;
            listener.Stop();
            listener = null;
        }
        lock (clients)
        {
            foreach (var c in clients)
            {
                c.Close();
            }
            clients.Clear();
        }
    }

    async Task OnConnectClient(TcpClient client)
    {
        var clientEndpoint = client.Client.RemoteEndPoint;
        Debug.Log($"完成握手 {clientEndpoint.ToString()}");
        clients.Add(client);

        await NetworkStreamHandler(client);
        Debug.Log($"连接断开 {clientEndpoint.ToString()}");
        clients.Remove(client);
    }


    async Task NetworkStreamHandler(TcpClient client)
    {
        while (client.Connected)
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];
            var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            stream.Flush();
            if (byteCount==0)break;//断线了
            var str = Encoding.UTF8.GetString(buffer, 0, byteCount);
            var msg = JsonUtility.FromJson<Message>(str);
            Debug.Log($"[播放器]收到信息 {str}");
            UnitySynchronizationContext.Post(() =>
            {
                if (Application.isPlaying)
                {
                    EventManager.Invoke(msg);
                }
            });
        }
    }


    public void BroadcastToClients(byte[] data)
    {
        Debug.Log($"Clients.Count : {Clients.Count}");
        foreach (var c in Clients)
        {
            c.GetStream().Write(data, 0, data.Length);
            c.GetStream().Flush();
        }
    }


    public void SendMessageToClient(TcpClient c, byte[] data)
    {
        c.GetStream().Write(data, 0, data.Length);
        c.GetStream().Flush();
    }


    public void Dispose()
    {
        Stop();
    }
}

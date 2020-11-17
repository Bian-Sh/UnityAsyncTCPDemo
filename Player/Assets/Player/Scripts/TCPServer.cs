using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using zFrame.ThreadEx;
using UnityEngine.Events;
using System.Net.NetworkInformation;
using Cysharp.Threading.Tasks;

public class TCPServer : IDisposable
{
    /// <summary>
    /// 当有客户端连接上时触发
    /// </summary>
    public TCPServerEvent OnClientConnected = new TCPServerEvent();
    /// <summary>
    /// 当服务器断线时触发
    /// </summary>
    public UnityEvent OnServiceClosed = new UnityEvent();
    public class TCPServerEvent : UnityEvent<TcpClient> { }

    CircularBuffer recvbuffer = new CircularBuffer();
    PacketParser recvparser;


    IPEndPoint endpoint;
    TcpListener listener;
    List<TcpClient> clients = new List<TcpClient>();
    volatile bool acceptLoop = true;
    public IReadOnlyList<TcpClient> Clients => clients;
    public TCPServer(IPEndPoint endpoint)
    {
        this.endpoint = endpoint;
        recvparser = new PacketParser(recvbuffer);
    }


    public async Task ListenAsync()
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
                await UniTask.Yield(); //通过该语句，程序将返回主线程上下文，其他地方一个意思
                OnClientConnected.Invoke(client);
            }
            catch (ObjectDisposedException e)// thrown if the listener socket is closed
            {
                Debug.Log($"{nameof(TCPServer)}: Server was Closed! {e}");
            }
            catch (SocketException e)// Some socket error
            {
                Debug.Log($"{nameof(TCPServer)}: Some socket error occurred! {e}");
            }
            finally
            {
                await UniTask.Yield();
                OnServiceClosed.Invoke();
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
                c?.Close();
            }
            clients.Clear();
        }
    }

    async Task OnConnectClient(TcpClient client)
    {
        var clientEndpoint = client.Client.RemoteEndPoint;
        Debug.Log($"完成握手 {clientEndpoint}");
        clients.Add(client);
        try
        {
            await NetworkStreamHandler(client); //但连接断开时，stream 会抛出dispose相关异常
        }
        catch (Exception e)
        {
            Debug.Log($"{nameof(TCPServer)}: 客户端意外断开连接 {e}");
        }
        finally
        {
            Debug.Log($"连接断开 {clientEndpoint.ToString()}");
            clients.Remove(client);
        }
    }


    async Task NetworkStreamHandler(TcpClient client)
    {
        while (client.Connected)
        {
            var stream = client.GetStream();
            await recvbuffer.WriteAsync(stream);
            var buffer = new byte[4096];
            var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            stream.Flush();
            bool isOK = this.recvparser.Parse();
            if (isOK)
            {
                Packet packet = this.recvparser.GetPacket();
                var request = Encoding.UTF8.GetString(packet.Bytes, 0, packet.Length);
                Debug.Log($"[控制器] 接收到播放器消息 {request}!");
                await UniTask.Yield();
                try
                {
                    EventManager.Invoke(JsonUtility.FromJson<Message>(request)); // 这里必须使用Try catch ，避免这条语句触发异常被外部捕捉而导致网络意外断开
                }
                catch (Exception e)
                {
                    Debug.Log($"{nameof(TCPServer)}: {e}");
                }
            }
        }
    }

    /// <summary>
    /// 广播
    /// </summary>
    /// <param name="data"></param>
    public void BroadcastToClients(byte[] data)
    {
        Debug.Log($"Clients.Count : {Clients.Count}");
        foreach (var c in Clients)
        {
            SendMessageToClient(c, data);
        }
    }

    /// <summary>
    /// 像指定的客户端发消息
    /// </summary>
    /// <param name="c"></param>
    /// <param name="data"></param>
    public void SendMessageToClient(TcpClient c, byte[] data)
    {
        if (null != c)
        {
            try
            {
                byte[] size = BytesHelper.GetBytes((ushort)data.Length); //简易封包协议：包长度+包体
                var temp = new byte[size.Length + data.Length];
                Buffer.BlockCopy(size, 0, temp, 0, size.Length);
                Debug.Log($"{nameof(TCPServer)}: {data.Length}");
                Buffer.BlockCopy(data, 0, temp, size.Length, data.Length);
                c.GetStream().Write(temp, 0, temp.Length);
                c.GetStream().Flush();
            }
            catch (Exception e)
            {
                Debug.Log($"{nameof(TCPServer)}: Send Message To Client Failed - {e}");
            }
        }
    }


    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// 获得本地IP(ipv4)
    /// </summary>
    /// <returns></returns>
    public static List<string> GetIP()
    {

        List<string> output = new List<string>();
        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
            NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

            if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) && item.OperationalStatus == OperationalStatus.Up)
#endif 
            {
                foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                {

                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        output.Add(ip.Address.ToString());
                    }
                }
            }
        }
        return output;
    }
}

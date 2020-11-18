using System;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using System.Linq;
using Shell32;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;

public class PlayerServer : MonoBehaviour
{

    public VideoPlayer player;
    private string currentPlayFile = string.Empty;
    private PlayList playList;
    TCPServer TCPServer;
    private void Awake()
    {
        RefreshFile();
    }

    void Start()
    {
        TCPServer = new TCPServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));
        player.loopPointReached += Player_loopPointReached;
        EventManager.AddListener(Command.Play, OnPlayRequest);
        EventManager.AddListener(Command.Pause, OnPauseRequest);
        EventManager.AddListener(Command.Stop, OnStopRequest);
        EventManager.AddListener(Command.PlayList, OnPlayListResponse);

        player.targetTexture.Release();
        TCPServer.OnClientConnected.AddListener(OnClientConnected);
        _ = Task.Run(TCPServer.ListenAsync); //此处务必使用 Task 执行，否则这个Awake 方法会回不到主线程，如果下面还有逻辑也不会执行咯
    }


    private void OnClientConnected(TcpClient arg0) 
    {
        string pl = JsonUtility.ToJson(playList);
        Message message = new Message { command = Command.PlayList, cmdContext = pl };
        TCPServer.SendMessageToClient(arg0,Encoding.UTF8.GetBytes(JsonUtility.ToJson(message))); 
    }

    private void OnPlayListResponse(string obj)
    {
        Debug.Log("[播放器] 向控制器发送播放列表");
        string pl = JsonUtility.ToJson(playList);
        Message message = new Message { command = Command.PlayList, cmdContext = pl };
        TCPServer.BroadcastToClients(Encoding.UTF8.GetBytes(JsonUtility.ToJson(message)));
    }

    private void Player_loopPointReached(VideoPlayer source)
    {
        Debug.Log("播放完毕！");
    }

    private void OnStopRequest(string obj)
    {
        Message msg = JsonUtility.FromJson<Message>(obj);
        currentPlayFile = string.Empty;
        Debug.Log("播放器收到停止播放指令！");
        player.Stop();
        player.targetTexture.Release();
        //： 向所有控制器同步视频被停止的状态
        TCPServer.BroadcastToClients(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Message { command = Command.Stop })));
    }

    private void OnPauseRequest(string obj)
    {
        Message msg = JsonUtility.FromJson<Message>(obj);
        if (!player.isPlaying) return;
        Debug.Log("播放器收到暂停播放指令！");
        player.Pause();
        //： 向所有控制器同步视频被暂停的状态
        TCPServer.BroadcastToClients(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Message { command = Command.Pause })));
    }

    private void OnPlayRequest(string obj)
    {
        Debug.Log("播放器收到开始播放指令！");
        Message message = JsonUtility.FromJson<Message>(obj);
        var item = playList.items.Find(v => v.name == message.cmdContext);

        if (currentPlayFile == message.cmdContext)
        {
            player.Play();

        }
        else
        {
            if (null != item)
            {
                currentPlayFile = message.cmdContext;
                string url = item.path;
                player.url = url;
                player.Play();
            }
            else
            {
                var msg = $"找不到指定的文件 {message.cmdContext}";
                Debug.LogError(msg);
                //todo : 向请求的控制器发送错误消息
            }
        }
        //向所有控制器同步视频被播放的状态
        TCPServer.BroadcastToClients(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Message { command = Command.Play, cmdContext = JsonUtility.ToJson(item) })));
    }

    private void RefreshFile()//刷新文件列表
    {
        DirectoryInfo direction = new DirectoryInfo(Application.streamingAssetsPath);
        FileInfo[] files = direction.GetFiles("*.mp4", SearchOption.AllDirectories);
        if (null != files && files.Length > 0)
        {
            Func<FileInfo, VideoItem> CreateItem = v =>
             {
                 VideoItem itm = new VideoItem();
                 itm.name = v.Name;
                 itm.path = v.FullName;
                 ShellClass sh = new ShellClass();
                 Folder dir = sh.NameSpace(v.DirectoryName);
                 FolderItem item = dir.ParseName(v.Name);
                 itm.description = dir.GetDetailsOf(item, 24);
                 return itm;
             };
            playList = new PlayList { items = files.Select(CreateItem).ToList() };
        }
        foreach (var item in playList.items)
        {
            Debug.Log(item.name + " : " + item.description);
        }
    }
    private void OnDestroy()
    {
        TCPServer?.OnClientConnected.RemoveListener(OnClientConnected);
        TCPServer?.Stop();
    }
}

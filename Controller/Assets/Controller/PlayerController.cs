using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using zFrame.ThreadEx;

public class PlayerController : MonoBehaviour
{

    public Button playAndPause;
    public Button stop;
    public Dropdown dropdown;
    public Text message;
    public bool isPlay = false;
    public PlayList playList;
    private string currentPlayFile;
    TcpClient tcpClient;
    bool isRun = false;
    async void Start()
    {
        playAndPause.onClick.AddListener(OnPlayAndPauseButtonClicked);
        stop.onClick.AddListener(Stop);
        dropdown.onValueChanged.AddListener(OnDropDownValueChanged);
        stop.GetComponentInChildren<Text>().text = "Stop";
        playAndPause.GetComponentInChildren<Text>().text = "Play";

        EventManager.AddListener(Command.Play, OnPlayResponse);
        EventManager.AddListener(Command.Pause, OnPauseResponse);
        EventManager.AddListener(Command.Stop, OnStopResponse);
        EventManager.AddListener(Command.PlayList, OnPlayListResponse);
        await ConnectAsTcpClient();
        RequestPlayList();
    }
    private async Task ConnectAsTcpClient()
    {
        isRun = true;
        tcpClient = new TcpClient();
        tcpClient.NoDelay = true;
        try
        {
            await tcpClient.ConnectAsync("127.0.0.1", 8888);
        }
        catch (Exception e)
        {
            message.text = $"[控制器] 连接到播放器失败 {e}!";
            throw;
        }
        UnitySynchronizationContext.Post(() => message.text = "[控制器] 连接到播放器!");
        var _ = Task.Run(StreamReadHandleAsync);
    }

    async Task StreamReadHandleAsync()
    {
        Debug.Log("开启数据读逻辑");
        try
        {
            while (isRun && tcpClient.Connected)
            {
                var networkStream = tcpClient.GetStream();
                var buffer = new byte[4096];
                var byteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount == 0) break;//服务器端口
                var response = Encoding.UTF8.GetString(buffer, 0, byteCount);
                Debug.Log($"[控制器] 接收到播放器消息 {response}!");
                UnitySynchronizationContext.Post(() => EventManager.Invoke(JsonUtility.FromJson<Message>(response)));
            }
        }
        catch (Exception e)
        {
            UnitySynchronizationContext.Post(() => { if (message) message.text = $"[控制器] 接收消息失败: {e}!"; });
            throw;
        }
    }


    void SendNetMessage(string str)
    {
        try
        {
            if (null != tcpClient && tcpClient.Connected)
            {
                var networkStream = tcpClient.GetStream();
                var ClientRequestBytes = Encoding.UTF8.GetBytes(str);
                networkStream.Write(ClientRequestBytes, 0, ClientRequestBytes.Length);
                networkStream.Flush();
                Debug.Log($"[控制器] 发送到播放器消息 {str}!");
                UnitySynchronizationContext.Post(() => { if (message) message.text = $"[控制器] 发送到播放器消息 {str}!"; });
            }
        }
        catch (Exception e)
        {
            UnitySynchronizationContext.Post(() => { if (message) message.text = $"[控制器] 发送消息到播放器错误 {e}!"; });
            throw;
        }
    }

    private void OnPlayListResponse(string obj)
    {
        Message m = JsonUtility.FromJson<Message>(obj);
        playList = JsonUtility.FromJson<PlayList>(m.cmdContext);
        UpdatePlayList();
    }

    private void OnStopResponse(string obj)
    {
        Message m = JsonUtility.FromJson<Message>(obj);
        isPlay = false;
        playAndPause.GetComponentInChildren<Text>().text = "Play";
        message.text = $"播放停止 ";
        currentPlayFile = string.Empty;
    }

    private void OnPauseResponse(string obj)
    {
        Message m = JsonUtility.FromJson<Message>(obj);
        isPlay = false;
        playAndPause.GetComponentInChildren<Text>().text = "Play";
        message.text = $"暂停播放 {currentPlayFile}";

    }

    private void OnPlayResponse(string obj)
    {
        Message m = JsonUtility.FromJson<Message>(obj);
        isPlay = true;
        playAndPause.GetComponentInChildren<Text>().text = "Pause";
        if (null != m)
        {
            VideoItem i = JsonUtility.FromJson<VideoItem>(m.cmdContext);
            message.text = $"生在播放 ：{i.name}\n文件备注 ：{ i.description}";
        }
        else
        {
            message.text = $"播放失败 !";
        }
    }

    private void OnDropDownValueChanged(int arg0)
    {
        Play();
    }


    #region PlayerBehaviours

    private void Stop()//停止播放
    {
        message.text = $"请求停止播放视频！ ";
        SendNetMessage(JsonUtility.ToJson(new Message { command = Command.Stop }));
    }
    private void Play()
    {
        if (dropdown.options.Count == 0)
        {
            message.text = "播放列表为空！";
            return;
        }
        if (isPlay && currentPlayFile == dropdown.captionText.text)
        {
            message.text = $"{currentPlayFile } 正在播放中！";
            return;
        }
        VideoItem video = playList.items.Find(v => v.name == dropdown.captionText.text);
        if (null != video)
        {
            currentPlayFile = video.name;
            message.text = $"正在请求播放 {currentPlayFile}...";
            SendNetMessage(JsonUtility.ToJson(new Message { command = Command.Play, cmdContext = currentPlayFile }));

        }
        else
        {
            message.text = $"请求的文件 : {dropdown.captionText.text} 不在播放列表";
        }
    }

    private void Pause()
    {
        message.text = $"请求暂停视频播放！ ";
        SendNetMessage(JsonUtility.ToJson(new Message { command = Command.Pause }));

    }

    //请求播放列表
    private void RequestPlayList()
    {
        //登陆后请求更新播放列表
        SendNetMessage(JsonUtility.ToJson(new Message { command = Command.PlayList }));
    }

    //更新播放列表
    private void UpdatePlayList()
    {
        Debug.Log("updateplaylist");
        if (null != playList && playList.items.Count > 0)
        {
            dropdown.ClearOptions();
            var files = playList.items.Select(v => v.name).ToList();
            dropdown.AddOptions(files);
            message.text = "列表更新完毕！";
        }
    }

    #endregion


    private void OnPlayAndPauseButtonClicked()//播放/暂停
    {
        if (isPlay)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }
    private void OnDestroy()
    {
        isRun = false;
    }
}

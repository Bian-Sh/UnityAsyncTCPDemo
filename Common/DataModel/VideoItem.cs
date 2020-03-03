[System.Serializable]
public class VideoItem
{
    public string name; //视频名称包括后缀
    [System.NonSerialized]
    public string path; //绝对路径
    public string description;
}
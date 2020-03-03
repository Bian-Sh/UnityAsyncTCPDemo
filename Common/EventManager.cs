using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简易事件总线
/// </summary>
public static class EventManager {
    private static Dictionary<Command, List<Action<string>>> events = new Dictionary<Command, List<Action<string>>>();
    /// <summary>
    /// 监听指定事件
    /// </summary>
    /// <param name="command"></param>
    /// <param name="action"></param>
    public static void AddListener(Command command,Action<string> action)
    {
        List<Action<string>> acts = null;
        if (events.TryGetValue(command,out acts))
        {
            if (!acts.Contains(action))
            {
                acts.Add(action);
            }
        }
        else
        {
            events.Add(command, new List<Action<string>> { action } );
        }
    }

    /// <summary>
    /// 执行指定事件
    /// </summary>
    /// <param name="message"></param>
    public static void Invoke(Message message )
    {
        if (null == message) return;
        if (events.TryGetValue( message.command, out List<Action<string>> acts))
        {
            foreach (var item in acts)
            {
                item.Invoke(JsonUtility.ToJson(message));
            }
        }
    }
}

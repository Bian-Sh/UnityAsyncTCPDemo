using System;
using System.Threading;
using UnityEngine;
namespace zFrame.ThreadEx
{
    public static class UnitySynchronizationContext
    {
        public static int ThreadId { get; private set; }
        static SynchronizationContext context;

        /// <summary>
        /// 从任何线程投递此委托并在主线程中执行,此动作不卡原线程
        /// </summary>
        public static void Post(Action action)
        {
            if (SynchronizationContext.Current == context)
            {
                action?.Invoke();
            }
            else
            {
                context.Post(_ => action(), null);
            }
        }

        /// <summary>
        /// 从任何线程发送此委托并在主线程中执行,此动作会卡原线程
        /// </summary>
        /// <param name="action"></param>
        public static void Send(Action action)
        {
            context.Send(_ => action(), null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            context = SynchronizationContext.Current;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
        }
    }
}
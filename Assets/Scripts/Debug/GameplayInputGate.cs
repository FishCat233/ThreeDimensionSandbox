using UnityEngine;

// TODO: 我对这个做法有些存疑，不过就先这么做吧

namespace TDS.Debugging
{
    /// <summary>
    /// 游戏输入的全局闸门：调试界面（例如 <see cref="ConsoleUI"/>）打开时上锁，
    /// 直接读 <see cref="Input"/> 的脚本（例如 PlayerController）在 Update 开头检查 <see cref="Blocked"/> 决定是否让位。
    /// 计数让多个界面各自上锁、各自归还，互不干扰。
    /// </summary>
    public static class GameplayInputGate
    {
        static int blockCount;

        /// <summary>有任意一方上锁时为 true。</summary>
        public static bool Blocked => blockCount > 0;

        public static void Block() => blockCount++;

        public static void Unblock()
        {
            if (blockCount > 0) blockCount--;
        }

        /// <summary>域重载被关闭时静态字段会跨播放保留，这里在进入播放模式时清零。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => blockCount = 0;
    }
}

namespace Myd.Platform
{
    /// <summary>
    /// 保险箱密码全局状态：
    /// 母亲庭院保险箱（保险箱 老）设置密码后，SafeController.Combination 从默认 0516 更新为新密码。
    /// 女儿庭院保险箱（保险箱 新）的 SafeUnlocker 读取该密码校验。
    /// 静态存储：同一次游戏进程内跨场景保留（正式存档接入后可替换为持久化读写）。
    /// </summary>
    public static class SafeController
    {
        public const string DefaultCombination = "0516";
        private static string combination = DefaultCombination;

        public static string Combination
        {
            get => combination;
            set => combination = value;
        }

        public static bool IsDefault => combination == DefaultCombination;

        public static void Reset()
        {
            combination = DefaultCombination;
        }
    }
}

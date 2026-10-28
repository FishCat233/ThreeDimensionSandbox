namespace TDS.Debugging
{
    /// <summary>
    /// 一条控制台指令。实现类须提供公开无参构造函数，<see cref="ConsoleController"/> 才能在启动时自动发现它；
    /// 需要引用场景对象的指令，用 <see cref="ConsoleController.Register"/> 手动注册。
    /// </summary>
    public interface ICommand
    {
        /// <summary>指令名，例如 "timescale"。匹配时不区分大小写。</summary>
        string Name { get; }

        /// <summary>用法串，例如 "usage: timescale [value]"，`help <指令名>` 直接显示它。</summary>
        string Usage { get; }

        /// <param name="args">指令名之后的参数，已按空白字符切分；没有参数时是空数组。</param>
        /// <returns>显示在控制台里的文本，可以含换行；返回 null 或空串表示没有输出。
        /// 以 "error: " 开头的行会被 <see cref="ConsoleUI"/> 当作错误着色。</returns>
        string Execute(string[] args);
    }
}

using System;
using System.Linq;

namespace TDS.Debugging
{
    /// <summary>
    /// <c>help</c>：不带参数时列出所有已注册的指令名，带一个参数时显示该指令的 <see cref="ICommand.Usage"/>。
    /// 要读取指令表，所以由 <see cref="ConsoleController"/> 显式注册，不参与反射自动发现。
    /// </summary>
    public class CommandHelp : ICommand
    {
        readonly ConsoleController console;

        public CommandHelp(ConsoleController console) => this.console = console;

        public string Name => "help";

        public string Usage => "usage: help [command]";

        public string Execute(string[] args)
        {
            if (args == null || args.Length == 0)
                return string.Join("\n", console.Commands.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

            if (args.Length > 1)
                return $"error: too many arguments. {Usage}";

            if (!console.Commands.TryGetValue(args[0], out ICommand command))
                return $"error: unknown command '{args[0]}'. {Usage}";

            return string.IsNullOrWhiteSpace(command.Usage)
                ? $"error: '{command.Name}' has no usage recorded."
                : command.Usage;
        }
    }
}

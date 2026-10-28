using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TDS.Debugging
{
    /// <summary>
    /// 控制台指令的注册表与派发器：把一行文本切成「指令名 + 参数」，找到 <see cref="ICommand"/> 并调用它。
    /// 未知指令与指令内部抛出的异常都会转成 error 行回显，绘制循环不会被中断。
    /// </summary>
    public class ConsoleController : MonoBehaviour
    {
        static readonly char[] Whitespace = { ' ', '\t' };

        [Tooltip("启动时用反射扫描所有 ICommand 实现并自动注册（要求公开无参构造函数）")]
        [SerializeField] bool discoverCommands = true;

        readonly Dictionary<string, ICommand> commands =
                    new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, ICommand> Commands => commands;

        /// <summary>指令回显的文本。可以含换行；以 "error: " 开头的行会被 UI 当作错误着色。</summary>
        public event Action<string> Output;

        void Awake()
        {
            Register(new CommandHelp(this));

            if (discoverCommands) DiscoverCommands();
        }

        /// <summary>注册一条指令，名字重复时返回 false 并保留先注册的那条。</summary>
        public bool Register(ICommand command)
        {
            if (command == null)
            {
                Debug.LogWarning("Couldn't register a null command. To avoid this error, please pass a non-null ICommand instance.", this);
                return false;
            }

            string name = command.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning(
                    $"Command {command.GetType().Name} has an empty Name. To avoid this error, please give it a non-empty name.", this);
                return false;
            }

            if (commands.TryGetValue(name, out ICommand existing))
            {
                Debug.LogWarning(
                    $"Command '{name}' already exists. Kept {existing.GetType().Name}, ignored {command.GetType().Name}. " +
                    "To avoid this error, please give each command a unique name.", this);
                return false;
            }

            commands.Add(name, command);
            return true;
        }

        /// <summary>注销一条指令，名字不区分大小写。</summary>
        public bool Unregister(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && commands.Remove(name.Trim());
        }

        /// <summary>执行一行控制台输入，空行和纯空白直接返回。</summary>
        public void Execute(string line)
        {
            string[] tokens = Tokenize(line);
            if (tokens.Length == 0) return;

            if (!commands.TryGetValue(tokens[0], out ICommand command))
            {
                Write($"error: unknown command '{tokens[0]}'.");
                return;
            }

            string[] args = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, args, 0, args.Length);

            string result;
            try
            {
                result = command.Execute(args);
            }
            catch (Exception exception)
            {
                Write($"error: '{command.Name}' failed: {exception.Message}");
                Debug.LogException(exception, this);
                return;
            }

            if (!string.IsNullOrEmpty(result)) Write(result);
        }

        static string[] Tokenize(string line)
        {
            return string.IsNullOrEmpty(line)
                ? Array.Empty<string>()
                : line.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);
        }

        void Write(string text) => Output?.Invoke(text);

        void DiscoverCommands()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetAvailableCommandTypes(assembly))
                {
                    try
                    {
                        Register((ICommand)Activator.CreateInstance(type));
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(
                            $"Couldn't instantiate command {type.FullName}: {exception.Message}. " +
                            "To avoid this error, please give it a public parameterless constructor.", this);
                    }
                }
            }
        }

        private static IEnumerable<Type> GetAvailableCommandTypes(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                Debug.LogWarning($"There are some command that cant assembly.GetTypes safely. For more information: {exception}");
                types = exception.Types; // exception 的时候 null 类型会被加进 types
            }

            types = types.Where(t => t != null
                    && !t.IsAbstract
                    && !t.IsInterface
                    && !t.IsGenericTypeDefinition
                    && typeof(ICommand).IsAssignableFrom(t)
                    && !typeof(UnityEngine.Object).IsAssignableFrom(t) // UnityEngine.Object 派生类不能用 Activator 创建，只能显式注册。
                    && t.GetConstructor(Type.EmptyTypes) != null).ToArray();

            return types;
        }
    }
}

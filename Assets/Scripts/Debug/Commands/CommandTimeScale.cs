using System.Globalization;
using UnityEngine;

namespace TDS.Debugging
{
    /// <summary>
    /// <c>timescale</c>：读取或设置 <see cref="Time.timeScale"/>，不带参数时回显当前值。
    /// </summary>
    public class CommandTimeScale : ICommand
    {
        const float MinTimeScale = 0f;
        const float MaxTimeScale = 100f;

        public string Name => "timescale";

        public string Usage => "usage: timescale [value]";

        public string Execute(string[] args)
        {
            if (args == null || args.Length == 0)
                return $"timeScale = {Format(Time.timeScale)}";

            if (args.Length > 1)
                return $"error: too many arguments. {Usage}";

            if (!float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                return $"error: '{args[0]}' is not a number. {Usage}";

            Time.timeScale = Mathf.Clamp(value, MinTimeScale, MaxTimeScale);
            return $"timeScale = {Format(Time.timeScale)}";
        }

        static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

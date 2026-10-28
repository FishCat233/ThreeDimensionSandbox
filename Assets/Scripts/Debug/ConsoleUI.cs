using System;
using System.Collections.Generic;
using UnityEngine;

// TODO: 必要时候代码审查

namespace TDS.Debugging
{
    /// <summary>
    /// 用 Unity 内置 IMGUI（OnGUI）绘制控制台：上方滚动输出区，下方一行输入框，回车执行。
    /// 界面只负责收一行字符串、显示文本，解析与调用交给 <see cref="ConsoleController"/>。
    /// </summary>
    /// <remarks>
    /// 布局用显式 Rect：滚动位置要停在最底部，必须先算出内容高度再夹取，否则内容会被推到视口外面。
    /// 打开时解锁光标并关上 <see cref="GameplayInputGate"/>，关闭后的下一帧才归还，这样 PlayerController
    /// 不会把关闭控制台的那次 Esc 当成「解锁光标」再处理一遍。
    /// </remarks>
    [RequireComponent(typeof(ConsoleController))]
    public class ConsoleUI : MonoBehaviour
    {
        const string InputControlName = "TDSConsoleInput";
        const int MaxLines = 500;
        const int MaxInputLength = 1024;
        const float Padding = 8f;
        const float ScrollbarWidth = 16f;
        const float RowGap = 5f;

        static readonly Color PanelColor = new Color(0.04f, 0.04f, 0.05f, 0.94f);
        static readonly Color TextColor = new Color(0.88f, 0.88f, 0.9f, 1f);
        static readonly Color ErrorColor = new Color(1f, 0.42f, 0.42f, 1f);
        static readonly Color PromptColor = new Color(0.55f, 0.75f, 1f, 1f);
        static readonly Color SeparatorColor = new Color(1f, 1f, 1f, 0.12f);

        [Tooltip("控制台窗口占屏幕高度的比例")]
        [SerializeField, Range(0.15f, 0.8f)] float heightRatio = 0.35f;

        [Tooltip("执行指令的控制器，必须显式绑定")]
        [SerializeField] ConsoleController controller;

        readonly List<Line> lines = new List<Line>();
        readonly GUIContent measure = new GUIContent();

        bool isOpen;
        string input = string.Empty;
        bool focusInput;
        bool clearFocus;
        bool scrollToBottom;
        bool blocksGameplay;
        Vector2 scroll;
        CursorLockMode previousLockMode;
        bool previousCursorVisible;

        GUIStyle textStyle;
        GUIStyle errorStyle;
        GUIStyle promptStyle;
        GUIStyle inputStyle;
        int stylesForScreenHeight = -1;

        /// <summary>控制台窗口当前是否可见；可见时游戏输入被压制、光标被解锁。</summary>
        public bool IsOpen => isOpen;

        void Awake()
        {
            if (controller == null)
                Debug.LogError("Couldn't find controller by explicit binding. To avoid this error, please bind controller explicitly.", this);
        }

        void OnEnable()
        {
            if (controller != null) controller.Output += OnControllerOutput;

            Append("Console ready. Press ` to open, Esc to close.");
        }

        void OnDisable()
        {
            if (controller != null) controller.Output -= OnControllerOutput;

            SetOpen(false);
            ReleaseGameplayInput();
        }

        /// <summary>打开或关闭控制台，同时接管、归还光标与游戏输入。</summary>
        public void SetOpen(bool open)
        {
            if (isOpen == open) return;
            isOpen = open;

            if (open)
            {
                input = string.Empty;
                focusInput = true;
                scrollToBottom = true;
                previousLockMode = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                GameplayInputGate.Block();
                blocksGameplay = true;
            }
            else
            {
                clearFocus = true;
                Cursor.lockState = previousLockMode;
                Cursor.visible = previousCursorVisible;
            }
        }

        void ReleaseGameplayInput()
        {
            if (!blocksGameplay) return;
            GameplayInputGate.Unblock();
            blocksGameplay = false;
        }

        void OnGUI()
        {
            EnsureStyles();

            if (clearFocus)
            {
                clearFocus = false;
                GUI.FocusControl(null);
            }

            // 按键在绘制之前处理并吃掉事件，输入框才不会把 ` 和回车再处理一次。
            if (HandleKeyEvents()) return;

            if (!isOpen)
            {
                ReleaseGameplayInput();
                return;
            }

            DrawPanel();
        }

        /// <summary>处理开关与提交键，返回 true 表示本次事件不再绘制。</summary>
        bool HandleKeyEvents()
        {
            Event current = Event.current;
            if (current == null || current.type != EventType.KeyDown) return false;

            // IMGUI 把一次可打印按键拆成两个 KeyDown 事件：先一个 keyCode 有效（物理键）、character 为空，
            // 再一个 character 有效（打出的字符）、keyCode 是 None。开关只认 keyCode 那一个；
            // 带 ` 的字符事件必须一起吃掉，否则打开控制台那一下会把 ` 漏进输入框。
            if (current.keyCode == KeyCode.BackQuote)
            {
                SetOpen(!isOpen);
                current.Use();
                return true;
            }

            if (current.character == '`')
            {
                current.Use();
                return true;
            }

            if (!isOpen) return false;

            if (current.keyCode == KeyCode.Escape)
            {
                SetOpen(false);
                current.Use();
                return true;
            }

            if (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
            {
                Submit();
                current.Use();
                return true;
            }

            return false;
        }

        void DrawPanel()
        {
            float panelHeight = Mathf.Clamp(Screen.height * heightRatio, 120f, Screen.height);
            Rect panel = new Rect(0f, 0f, Screen.width, panelHeight);

            Color previousColor = GUI.color;
            GUI.color = PanelColor;
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previousColor;

            Rect inner = new Rect(
                panel.x + Padding,
                panel.y + Padding,
                Mathf.Max(1f, panel.width - Padding * 2f),
                Mathf.Max(1f, panel.height - Padding * 2f));

            float inputHeight = inputStyle.CalcHeight(GUIContent.none, 1f);
            float outputHeight = Mathf.Max(0f, inner.height - inputHeight - RowGap);

            Rect outputRect = new Rect(inner.x, inner.y, inner.width, outputHeight);
            Rect separatorRect = new Rect(inner.x, outputRect.yMax + RowGap * 0.5f, inner.width, 1f);
            Rect inputRect = new Rect(inner.x, separatorRect.yMax + RowGap * 0.5f, inner.width, inputHeight);

            DrawOutput(outputRect);
            DrawSeparator(separatorRect);
            DrawInput(inputRect);
        }

        void DrawOutput(Rect rect)
        {
            float contentWidth = Mathf.Max(1f, rect.width - ScrollbarWidth);
            float contentHeight = MeasureContentHeight(contentWidth);
            float maxScroll = Mathf.Max(0f, contentHeight - rect.height);

            if (scrollToBottom)
            {
                scrollToBottom = false;
                scroll.y = maxScroll;
            }

            scroll.x = 0f;
            scroll.y = Mathf.Clamp(scroll.y, 0f, maxScroll);

            Rect content = new Rect(0f, 0f, contentWidth, contentHeight);
            scroll = GUI.BeginScrollView(rect, scroll, content);

            float y = 0f;
            foreach (Line line in lines)
            {
                GUIStyle style = line.IsError ? errorStyle : textStyle;
                float height = MeasureHeight(style, line.Text, contentWidth);
                GUI.Label(new Rect(0f, y, contentWidth, height), line.Text, style);
                y += height;
            }

            GUI.EndScrollView();
        }

        void DrawSeparator(Rect rect)
        {
            Color previousColor = GUI.color;
            GUI.color = SeparatorColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        void DrawInput(Rect rect)
        {
            float promptWidth = Mathf.Max(12f, promptStyle.fontSize * 1.2f);
            GUI.Label(new Rect(rect.x, rect.y, promptWidth, rect.height), ">", promptStyle);

            Rect field = new Rect(rect.x + promptWidth, rect.y, Mathf.Max(1f, rect.width - promptWidth), rect.height);
            GUI.SetNextControlName(InputControlName);
            input = GUI.TextField(field, input ?? string.Empty, MaxInputLength, inputStyle);

            // 控件画完之后再请求焦点，IMGUI 会在这一帧结束前应用。
            if (focusInput)
            {
                focusInput = false;
                GUI.FocusControl(InputControlName);
            }
        }

        float MeasureContentHeight(float width)
        {
            float height = 0f;
            foreach (Line line in lines)
                height += MeasureHeight(line.IsError ? errorStyle : textStyle, line.Text, width);
            return height;
        }

        float MeasureHeight(GUIStyle style, string text, float width)
        {
            measure.text = text ?? string.Empty;
            return style.CalcHeight(measure, width);
        }

        void EnsureStyles()
        {
            if (textStyle != null && stylesForScreenHeight == Screen.height) return;
            stylesForScreenHeight = Screen.height;

            int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.0165f), 10, 40);

            textStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                richText = false,
                fontSize = fontSize,
            };
            textStyle.normal.textColor = TextColor;

            errorStyle = new GUIStyle(textStyle);
            errorStyle.normal.textColor = ErrorColor;

            promptStyle = new GUIStyle(textStyle) { wordWrap = false };
            promptStyle.normal.textColor = PromptColor;

            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = fontSize };
        }

        void Submit()
        {
            string line = input == null ? string.Empty : input.Trim();
            input = string.Empty;
            if (line.Length == 0) return;

            Append("> " + line);

            if (controller == null)
            {
                Append("error: no ConsoleController attached.", true);
                return;
            }

            controller.Execute(line);
        }

        void OnControllerOutput(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            foreach (string line in text.Split('\n'))
                Append(line, IsErrorLine(line));
        }

        static bool IsErrorLine(string text)
        {
            return text.StartsWith("error:", StringComparison.OrdinalIgnoreCase);
        }

        void Append(string text, bool isError = false)
        {
            lines.Add(new Line(text, isError));
            if (lines.Count > MaxLines) lines.RemoveRange(0, lines.Count - MaxLines);
            scrollToBottom = true;
        }

        readonly struct Line
        {
            public Line(string text, bool isError)
            {
                Text = text;
                IsError = isError;
            }

            public string Text { get; }

            public bool IsError { get; }
        }
    }
}

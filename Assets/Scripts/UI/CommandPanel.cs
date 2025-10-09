using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Cysharp.Threading.Tasks;
using Looper.Console.Commands;
using Looper.Console.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Looper.Console.Core
{
    public interface ICommand
    {
        string Name { get; }
        IReadOnlyList<string> Aliases { get; }
        string Summary { get; }
        string Usage { get; }
        bool IsAsync { get; }

        /// <summary>
        /// 명령어 실행
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args"></param>
        void Execute(CommandContext ctx, string[] args);
        /// <summary>
        /// 비동기 명령어 실행
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        UniTask ExecuteAsync(CommandContext ctx, string[] args);
    }
    public sealed class CommandContext
    {
        public readonly GameObject Actor;
        public readonly Action<string> Print;
        public readonly Func<string, UniTask> PrintAsync;
        public readonly Transform World;
        public readonly IServiceProvider Service;

        public CommandContext(
            Action<string> print,
            Func<string, UniTask> printAsync,
            IServiceProvider service
        )
        {
            Print = print ?? Debug.Log;
            PrintAsync = printAsync ?? (s => { Debug.Log(s); return UniTask.CompletedTask; });
            Service = service;
        }
        public void Info(string msg) => Print(msg);
        public void Warn(string msg) => Print($"<color=yellow>{msg}</color>");
        public void Error(string msg) => Print($"<color=red>{msg}</color>");
    }
    public static class CommandParser
    {
        public static string[] Tokenize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Array.Empty<string>();
            var list = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            foreach (char c in input)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (sb.Length > 0)
                    {
                        list.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else sb.Append(c);
            }
            if (sb.Length > 0) list.Add(sb.ToString());
            return list.ToArray();
        }
        public static (string cmd, string[] args) Split(string input)
        {
            var tokens = Tokenize(input);
            if (tokens.Length == 0) return (string.Empty, Array.Empty<string>());
            return (tokens[0].ToLowerInvariant(), tokens.Skip(1).ToArray());
        }
    }
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, ICommand> _byName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _aliasToName = new(StringComparer.OrdinalIgnoreCase);

        public void Register(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(command.Name)) throw new ArgumentException("명령어는 반드시 Name 필드를 포함하고 있어야 합니다.");
            if (_byName.ContainsKey(command.Name)) throw new InvalidOperationException($"이미 {command.Name} 명령어가 등록되어 있습니다.");

            _byName[command.Name] = command;
            if (command.Aliases != null)
            {
                foreach (var a in command.Aliases)
                {
                    if (string.IsNullOrWhiteSpace(a)) continue;
                    _aliasToName[a] = command.Name;
                }
            }
        }
        public bool TryGet(string nameOrAlias, out ICommand cmd)
        {
            cmd = null;
            if (string.IsNullOrWhiteSpace(nameOrAlias)) return false;
            if (_byName.TryGetValue(nameOrAlias, out cmd)) return true;
            if (_aliasToName.TryGetValue(nameOrAlias, out var canonical) && _byName.TryGetValue(canonical, out cmd)) return true;
            return false;
        }
        public IEnumerable<ICommand> All() => _byName.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }
}
namespace Looper.Console.UI
{
    public class CommandPanel : MonoBehaviour, IUI
    {
        [SerializeField] private UIContext uiContext;
        [SerializeField] private TextMeshProUGUI outputSource;
        private TMP_InputField inputSource;
        private InputSystem_Actions inputActions;
        private CommandRegistry _reg;
        [SerializeField] private LogMessageParent logMessageParent;

        private void Awake()
        {
            uiContext = uiContext != null ? uiContext : GetComponentInParent<UIContext>();
            uiContext.UIRegistry.Register(this, UIType.COMMAND_PANEL);
            inputActions = UIManager.inputAction;
            inputSource = GetComponentInChildren<TMP_InputField>();
            outputSource.text = string.Empty;
            if (inputSource != null)
                inputSource.onSubmit.AddListener(OnSubmit);
            RegisterCommand();
        }
        /// <summary>
        /// 명령어를 등록
        /// </summary>
        private void RegisterCommand()
        {
            _reg = new();
            _reg.Register(new HelpCommand(_reg));
            _reg.Register(new EchoCommand());
            _reg.Register(new LogMessageCommand());
            _reg.Register(new ClearCommand(outputSource));
            _reg.Register(new ExitCommand(new Action(() => { NegativeInteract(new()); })));
            _reg.Register(new TimeScaleCommand());
            _reg.Register(new GetItemCommand());
            _reg.Register(new SetBaseStatCommand());
            _reg.Register(new EquipItemCommand());
            _reg.Register(new TeleportCommand());
            _reg.Register(new OriginCommand());
            _reg.Register(new SetHPCommand());
            _reg.Register(new ClearBackpackCommand());
            _reg.Register(new RemoveItemCommand());
        }
        void OnEnable()
        {
            Time.timeScale = 0.0f;
        }
        void OnDisable()
        {
            if(PlayableCharacter.Inst != null)
                Time.timeScale = PlayableCharacter.Inst.gameTimeScale;
        }
        public void PositiveInteract(InputAction.CallbackContext context) => Show();
        public void NegativeInteract(InputAction.CallbackContext context) => Hide();
        private void OnSubmit(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            PrintLine($"> {text}");

            var (cmdName, args) = CommandParser.Split(text);
            if (string.IsNullOrEmpty(cmdName))
            {
                PrintLine("입력창이 비어있습니다.");
                inputSource.text = string.Empty;
            }
            if (!_reg.TryGet(cmdName, out var cmd))
            {
                PrintLine($"알 수 없는 명령어 : {cmdName} ('help'를 입력하여 도움말을 확인)");
                inputSource.text = string.Empty;
                inputSource.ActivateInputField();
                return;
            }
            var ctx = new CommandContext(
                print: PrintLine,
                printAsync: async s => { PrintLine(s); await UniTask.Yield(); },
                service: GameBootstrapper.ServiceProvider);

            if (cmd.IsAsync) RunAsync(cmd, ctx, args).Forget();
            else
            {
                try
                {
                    cmd.Execute(ctx, args);
                }
                catch (Exception e)
                {
                    PrintLine($"<color=orange>에러 : </color> {e.Message}");
                }
                inputSource.text = string.Empty;
                inputSource.ActivateInputField();
            }
        }
        private async UniTaskVoid RunAsync(ICommand cmd, CommandContext ctx, string[] args)
        {
            try
            {
                await cmd.ExecuteAsync(ctx, args);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                PrintLine($"<color=orange>에러 : </color> {e.Message}");
            }
            finally
            {
                inputSource.text = string.Empty;
                inputSource.ActivateInputField();
            }
        }
        private void PrintLine(string s)
        {
            if (outputSource == null)
            {
                Debug.Log(s);
                return;
            }
            outputSource.text += (outputSource.text.Length > 0 ? "\n" : "") + s;
        }
        public void Show() => gameObject.SetActive(true);
        public void Hide()
        {
            uiContext.UIRegistry.CloseUI(this);
            gameObject.SetActive(false);
        }
    }
}

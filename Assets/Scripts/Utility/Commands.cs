using System.Collections.Generic;
using UnityEngine;
using Looper.Console.Core;
using Cysharp.Threading.Tasks;
using System;
using System.Text;
using TMPro;

/// <summary>
/// ICommand 인터페이스를 불러오기 위해 네임스페이스를 사용
/// </summary>
namespace Looper.Console.Commands
{
    public sealed class HelpCommand : ICommand
    {
        public HelpCommand(CommandRegistry reg) => _reg = reg;
        private readonly CommandRegistry _reg;
        public string Name => "help";

        public IReadOnlyList<string> Aliases => Array.Empty<string>();

        public string Summary => "명령어 리스트 혹은 특정 명령어의 정보를 출력합니다.";

        public string Usage => "help [command]";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args)
        {
            if (args.Length == 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("명령어 : ");
                foreach (var c in _reg.All())
                    sb.AppendLine($" - {c.Name} : {c.Summary}");
                sb.AppendLine("help <command>를 입력하면 명령어에 대한 정보를 출력합니다.");
                ctx.Info(sb.ToString());
                return;
            }

            var name = args[0];
            if (_reg.TryGet(name, out var cmd))
                ctx.Info($"{cmd.Name}\n {cmd.Summary}\n 사용법 : {cmd.Usage}\n 명령어 별칭 : {string.Join(", ", cmd.Aliases ?? Array.Empty<string>())}");
            else
                ctx.Warn($"{name} 명령어는 존재하지 않습니다.");
        }
        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class EchoCommand : ICommand
    {
        public string Name => "echo";

        public IReadOnlyList<string> Aliases => new string[] { "print" };

        public string Summary => "입력한 텍스트를 출력합니다.";

        public string Usage => "echo <text>";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args)
        {
            if (args.Length == 0)
            {
                ctx.Warn("echo <text>의 형식으로 입력해야합니다.");
                return;
            }
            ctx.Info(string.Join(" ", args));
        }
        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class LogMessageCommand : ICommand
    {
        private LogMessageParent logMessageParent;
        public LogMessageCommand()
        {
            logMessageParent = Component.FindAnyObjectByType<LogMessageParent>();
        }
        public string Name => "message";

        public IReadOnlyList<string> Aliases => new string[] { "log" };

        public string Summary => "인게임에 메시지를 출력합니다.";

        public string Usage => "message <text>";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args)
        {
            if (args.Length == 0)
            {
                ctx.Warn("message <text>의 형식으로 입력해야합니다.");
                return;
            }
            StringBuilder sb = new();
            if (logMessageParent == null) return;

            logMessageParent = Component.FindAnyObjectByType<LogMessageParent>();
            foreach (var arg in args)
            {
                sb.Append(arg);
                sb.Append(" ");
            }

            logMessageParent.Spawn(sb.ToString());
        }

        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class ClearCommand : ICommand
    {
        private TextMeshProUGUI outputSource;
        public ClearCommand(TextMeshProUGUI source) => outputSource = source;
        public string Name => "clear";

        public IReadOnlyList<string> Aliases => new string[] { "cls", "clean", "cln" };

        public string Summary => "커맨드에 출력된 메시지를 모두 지웁니다.";

        public string Usage => "clear";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args)
        {
            outputSource.text = string.Empty;
        }

        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class ExitCommand : ICommand
    {
        private readonly Action exitCommandPanel;
        public ExitCommand(Action action) => this.exitCommandPanel = action;
        public string Name => "exit";

        public IReadOnlyList<string> Aliases => new string[] { "quit", "close", "hide" };

        public string Summary => "커맨드 패널을 닫습니다.";

        public string Usage => "exit";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args) => exitCommandPanel?.Invoke();

        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
}
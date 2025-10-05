using System.Collections.Generic;
using UnityEngine;
using Looper.Console.Core;
using Cysharp.Threading.Tasks;
using System;
using System.Text;
using TMPro;
using UnityEngine.Playables;
using System.Threading.Tasks;
using Looper.Console.UI;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading;
using System.Globalization;

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
    public sealed class TimeScaleCommand : ICommand
    {
        public string Name => "timescale";
        public IReadOnlyList<string> Aliases => new string[] { "ts" };
        public string Summary => "게임의 시간 배속을 조절합니다. (기본값 = 1.0)";
        public string Usage => "timescale <value>";
        public bool IsAsync => false;
        public void Execute(CommandContext ctx, string[] args)
        {
            if (args.Length == 0)
            {
                ctx.Warn("timescale <value>의 형식으로 입력해야합니다.");
                return;
            }
            if (float.TryParse(args[0], out float value))
            {
                PlayableCharacter.Inst.gameTimeScale = value;
                ctx.Info($"TimeScale이 {value}로 설정되었습니다.");
            }
            else
            {
                ctx.Warn("value는 숫자여야 합니다.");
            }
        }

        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class GetItemCommand : ICommand
    {
        public string Name => "getitem";
        public IReadOnlyList<string> Aliases => new string[] { "gi", "get", "item" };
        public string Summary => "아이템을 획득합니다.";
        public string Usage => "getitem <item_id> [quantity] [slot_number]";
        public bool IsAsync => false;
        public void Execute(CommandContext ctx, string[] args)
        {
            if (args.Length == 0)
            {
                ctx.Warn("getitem <item_id> [quantity]의 형식으로 입력해야합니다.");
                return;
            }
            string itemId = args[0];
            int quantity = 1;
            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out quantity) || quantity <= 0)
                {
                    ctx.Warn("quantity는 양의 정수여야 합니다.");
                    return;
                }
            }
            var item = ItemDataManager.GetItem(itemId);
            if (item.Equals(default))
            {
                ctx.Warn($"아이템 ID '{itemId}'에 해당하는 아이템이 존재하지 않습니다.");
                return;
            }
            if (args.Length > 2)
            {
                if (int.TryParse(args[2], out int slotNumber))
                {
                    if (slotNumber < 16 && slotNumber > 0)
                    {
                        PlayableCharacter.Inst.Inventory.backpack[slotNumber - 1] = (item, quantity);
                        ctx.Info($"{slotNumber}번 아이템 슬롯에 '{item.name[1]}'을(를) {quantity}개 설정했습니다.");
                        return;
                    }
                    ctx.Warn("아이템 슬롯의 번호가 올바르지 않습니다.(1~15)");
                    return;
                }
            }
            if (PlayableCharacter.Inst.Inventory.backpack.IsFull)
            {
                ctx.Warn("인벤토리가 가득 찼습니다.");
                return;
            }
            PlayableCharacter.Inst.GetItem(item, quantity);
            ctx.Info($"아이템 '{item.name[1]}'을(를) {quantity}개 획득했습니다.");
        }
        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class SetBaseStatCommand : ICommand
    {
        public string Name => "setstat";

        public IReadOnlyList<string> Aliases => new string[] { "setbasestat", "setbase", "basestat" };

        public string Summary => "기본 스탯을 설정합니다.";

        public string Usage => "setstat <stat_type: HP, ATK, ATS, DEF, CRI, CRID, SPD, JMP, JCNT> <value>";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args)
        {
            if (args.Length < 2)
            {
                ctx.Warn("setstat <stat_type: HP, ATK, ATS, DEF, CRI, CRID, SPD, JMP, JCNT> <value>의 형식으로 입력해야합니다.");
                return;
            }
            if (!Enum.TryParse<StatType>(args[0].ToUpperInvariant(), out var stat))
            {
                ctx.Warn("stat_type은 HP, ATK, ATS, DEF, CRI, CRID, SPD, JMP, JCNT 중 하나여야 합니다.");
                return;
            }
            if (!float.TryParse(args[1], out float value) || value < 0)
            {
                ctx.Warn("value는 0 이상의 유리수여야 합니다.");
                return;
            }
            PlayableCharacter.Inst.Data.GetStats().SetBase(stat, value);
            ctx.Info($"기본 {stat} 스탯이 {value}(으)로 설정되었습니다.");
        }
        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class EquipItemCommand : ICommand
    {
        public string Name => "equip";

        public IReadOnlyList<string> Aliases => new string[] { "eq", "e", "wear" };

        public string Summary => "장비 아이템을 착용합니다.";

        public string Usage => "equip <item_id>";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args)
        {
            if (args.Length == 0)
            {
                ctx.Warn("equip <item_id>의 형식으로 입력해야합니다.");
                return;
            }
            string itemId = args[0];
            if (itemId[0] == '0' && itemId[1] > '5')
            {
                ctx.Warn("장비 아이템만 착용할 수 있습니다.");
                return;
            }
            var item = ItemDataManager.GetItem(itemId);
            if (item.Equals(default))
            {
                ctx.Warn($"아이템 ID '{itemId}'에 해당하는 아이템이 존재하지 않습니다.");
                return;
            }
            switch (itemId[1])
            {
                case '1':
                    PlayableCharacter.Inst.SetMainWeapon(item);
                    ctx.Info($"'{item.name[1]}'을(를) 착용했습니다.");
                    break;
                case '2':
                    PlayableCharacter.Inst.SetSubWeapon(item);
                    ctx.Info($"'{item.name[1]}'을(를) 착용했습니다.");
                    break;
                case '3':
                    PlayableCharacter.Inst.SetHelmet(item);
                    ctx.Info($"'{item.name[1]}'을(를) 착용했습니다.");
                    break;
                case '4':
                    PlayableCharacter.Inst.SetArmor(item);
                    ctx.Info($"'{item.name[1]}'을(를) 착용했습니다.");
                    break;
                case '5':
                    PlayableCharacter.Inst.SetPants(item);
                    ctx.Info($"'{item.name[1]}'을(를) 착용했습니다.");
                    break;
            }
        }
        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class TeleportCommand : ICommand
    {
        public string Name => "teleport";
        public IReadOnlyList<string> Aliases => new string[] { "tp" };
        public string Summary => "플레이어를 지정한 좌표로 순간이동시킵니다.";
        public string Usage => "teleport <x> <y>";
        public bool IsAsync => false;
        public void Execute(CommandContext ctx, string[] args)
        {
            if (args.Length < 2)
            {
                ctx.Warn("teleport <x> <y>의 형식으로 입력해야합니다.");
                return;
            }
            if (!float.TryParse(args[0], out float x))
            {
                ctx.Warn("x는 유리수여야 합니다.");
                return;
            }
            if (!float.TryParse(args[1], out float y))
            {
                ctx.Warn("y는 유리수여야 합니다.");
                return;
            }
            var player = PlayableCharacter.Inst;
            if (player == null)
            {
                ctx.Warn("플레이어 오브젝트를 찾을 수 없습니다.");
                return;
            }
            player.transform.position = new Vector3(x, y, player.transform.position.z);
            ctx.Info($"플레이어를 ({x}, {y}) 좌표로 이동시켰습니다.");
        }
        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class OriginCommand : ICommand
    {
        public string Name => "origin";

        public IReadOnlyList<string> Aliases => new string[] { "startposition", "tporigin" };

        public string Summary => "해당 맵의 캐릭터 초기 스폰 위치로 이동합니다.";

        public string Usage => "origin";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args)
        {
            PlayableCharacter.Inst.InitPos();
            ctx.Info("캐릭터를 초기 위치로 이동하였습니다.");
        }
        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class SetHPCommand : ICommand
    {
        public string Name => "hp";

        public IReadOnlyList<string> Aliases => new string[] { "health" };

        public string Summary => "플레이어의 체력을 변경합니다.";

        public string Usage => "hp <value>";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args)
        {
            if (args.Length < 1)
            {
                ctx.Warn("hp <value>의 형식으로 입력해야합니다.");
                return;
            }
            if (int.TryParse(args[0], out var val) && val > -1)
            {
                PlayableCharacter.Inst.SetHP(val);
                ctx.Info($"플레이어의 체력을 {val}로 변경하였습니다.");
                return;
            }
            ctx.Warn("value는 0 이상의 정수여야 합니다.");
            return;
        }

        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class ClearBackpackCommand : ICommand
    {
        public string Name => "clearbackpack";

        public IReadOnlyList<string> Aliases => new string[] { "newbackpack", "backpackclear", "cleanbackpack", "clearinventory", "newinventory", "inventoryclear" };

        public string Summary => "가방을 비웁니다.";

        public string Usage => "clearbackpack";

        public bool IsAsync => false;

        public void Execute(CommandContext ctx, string[] args)
        {
            PlayableCharacter.Inst.Inventory.backpack.Clear();
            ctx.Info("가방을 비웠습니다.");
        }
        public UniTask ExecuteAsync(CommandContext ctx, string[] args) => UniTask.CompletedTask;
    }
    public sealed class RemoveItemCommand : ICommand
    {
        public string Name => "remove";

        public IReadOnlyList<string> Aliases => new string[] { "removeitem", "deleteitem", "rm", "rmitem", "rmi", "deli", "delitem" };

        public string Summary => "선택한 아이템을 제거합니다.";

        public string Usage => "";

        public bool IsAsync => true;

        public void Execute(CommandContext ctx, string[] args)
        {
            ExecuteAsync(ctx, args).Forget();
        }
        public async UniTask ExecuteAsync(CommandContext ctx, string[] args)
        {
            var data = (IInventoryData)ctx.Service.GetService(typeof(IInventoryData));
            var ui = (IInventoryUI)ctx.Service.GetService(typeof(IInventoryUI));
            var neg = (INegativeSignal)ctx.Service.GetService(typeof(INegativeSignal));

            if (data is null || ui is null || neg is null)
            {
                ctx.Error("서비스 누락: IInventoryData/IInventoryUI/INegativeSignal");
                return;
            }

            var backpack = data.Backpack;

            if (TryParseIndex(args, out var argIndex))
            {
                if (!IsValidIndex(backpack, argIndex))
                {
                    ctx.Warn($"유효하지 않은 슬롯: {argIndex}");
                    return;
                }
                if (backpack[argIndex].item.id == null)
                {
                    ctx.Info($"슬롯 {argIndex}는 비어있습니다.");
                    return;
                }
                backpack.RemoveSlot(argIndex);
                ctx.Info($"슬롯 {argIndex}의 아이템을 삭제했습니다.");
                return;
            }

            bool prevCmd = ui.CommandPanel.activeSelf;
            bool prevInv = ui.CharacterInformationPanel.activeSelf;
            float prevTS = Time.timeScale;
            var prevSel = ui.EventSystem.currentSelectedGameObject;

            using var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            void OnEsc()
            {
                if (!cts.IsCancellationRequested) cts.Cancel();
            }

            try
            {
                neg.Negative += OnEsc;

                ui.CommandPanel.SetActive(false);
                ui.CharacterInformationPanel.SetActive(true);
                Time.timeScale = 0.0f;

                if (ui.FirstInventorySelectable != null)
                    ui.EventSystem.SetSelectedGameObject(ui.FirstInventorySelectable);

                int picked = await data.Inventory.PickSlotAsync(cts.Token);
                
                if (!IsValidIndex(backpack, picked))
                {
                    ctx.Warn("선택이 올바르지 않습니다.");
                    return;
                }
                if (backpack[picked].item.id is null)
                {
                    ctx.Info($"슬롯 {picked}는 비어 있습니다.");
                    return;
                }
                PlayableCharacter.Inst.RemoveItem(backpack[picked]);
                ctx.Info($"슬롯 {picked}의 아이템을 삭제했습니다.");
            }
            catch (OperationCanceledException)
            {
                ctx.Info("아이템 삭제를 실패했습니다.");
            }
            finally
            {
                neg.Negative -= OnEsc;
                Time.timeScale = prevTS;
                ui.CharacterInformationPanel.SetActive(prevInv);
                ui.CommandPanel.SetActive(prevCmd);
                ui.EventSystem.SetSelectedGameObject(prevSel);
            }
        }
        static bool TryParseIndex(string[] args, out int index)
        {
            index = -1;
            if (args is null || args.Length == 0) return false;
            return int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
        }
        static bool IsValidIndex(Backpack backpack, int i) => (uint)i < (uint)backpack.Count;
    }
}
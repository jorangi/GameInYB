using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TextMeshProUGUI))]
public class InteractableText : UIHoverClickSFX, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMPLinkEvent e;
    [SerializeField] private bool enableHover = true;
    [SerializeField] private ModalController modalController;
    private TextMeshProUGUI source;
    private bool isOver;
    private void Start()
    {
        modalController = FindAnyObjectByType<UIManager>().modalController;
    }

    /// <summary>
    /// 검증을 통해 TMPUGUI가 존재하지 않을 경우 등록
    /// </summary>
    private void OnValidate()
    {
        if (source == null) source = GetComponent<TextMeshProUGUI>();
    }
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        int index = TMP_TextUtilities.FindIntersectingLink(source, eventData.position, eventData.pressEventCamera);
        if (index == -1) return;
        TMP_LinkInfo info = source.textInfo.linkInfo[index];
        string linkId = info.GetLinkID();
        if (linkId.ToString()[0] == '0') // 아이템일 경우
        {
            int lang = 1;
            Item item = ServiceHub.Get<IItemRepository>().GetItem(linkId.ToString());
            StringBuilder sb = new();
            sb.AppendLine(item.description[lang] + '\n');
            foreach (var option in item.GetProvider().GetStatModifiers())
            {
                // sb.AppendLine($"<link=\"{itemSlot.item.id}\"><u>{option.Stat}</u> {(option.Op == StatOp.ADD ? '+' : '*')}{(option.Stat == StatType.CRI || option.Stat == StatType.CRID ? option.Value * 100 + "%" : option.Value)}</link>");
                sb.AppendLine($"{option.Stat} {(option.Op == StatOp.ADD ? '+' : '*')}{(option.Stat == StatType.CRI || option.Stat == StatType.CRID ? option.Value * 100 + "%" : option.Value)}");
            }
            if (item.skills.Length > 0)
            {
                sb.AppendLine("\n<size=\"20%\">보유 스킬 :");
                var svc = ServiceHub.Get<ISkillRepository>();
                foreach (var skill in item.skills)
                {
                    sb.AppendLine($"<link=\"{skill}\"><u><color=#b8b8b8>{svc.GetSkill(skill).name[1]}</color></u></link>");
                }
                sb.AppendLine("</size>");
            }
            string titleColor = item.rarity switch
            {
                "uncommon" => "green",
                "rare" => "blue",
                "epic" => "purple",
                "legendary" => "orange",
                _ => "white",
            };
            string gradeKor = item.rarity switch
            {
                "uncommon" => "드문",
                "rare" => "레어",
                "epic" => "에픽",
                "legendary" => "레전더리",
                _ => "흔함",
            };
            string itemType = item.id[1] switch
            {
                '1' => item.twoHander ? "양손무기" : "한손무기",
                '2' => "보조무기",
                '3' => "투구",
                '4' => "갑옷",
                '5' => "바지",
                _ => "아이템"
            };
            _ = modalController.SpawnModal(isParent: false,
                                $"<color=\"{titleColor}\">{item.name[lang]}</color>    <align=\"right\"><size=\"20%\">({gradeKor}-{itemType})</align>",
                                sb.ToString(),
                                Vector2.zero);
        }
        else if (linkId.ToString()[0] == '2') // 스킬일 경우
        {
            int lang = 1;
            Skill skill = ServiceHub.Get<ISkillRepository>().GetSkill(linkId.ToString());
            _ = modalController.SpawnModal(isParent: false, title: $"{skill.name[lang]}", ctx: $"{skill.description[1]}", Vector2.zero);
        }
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        isOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isOver = false;
    }
}
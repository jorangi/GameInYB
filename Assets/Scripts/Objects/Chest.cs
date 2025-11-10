using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ChestGrade
{
    Normal,
    Rare,
    Epic,
    Legendary
}

[RequireComponent(typeof(Animator))]
public class Chest : InteractableObject
{
    [SerializeField] private ChestGrade grade;
    [SerializeField] private Animator anim;
    protected void Awake()
    {
        anim = anim != null ? anim : GetComponent<Animator>();
    }
    protected override void OnTriggerEnter2D(Collider2D col)
    {
        base.OnTriggerEnter2D(col);
        if (col.CompareTag("Player")) anim.SetBool("isOverlap", isOn);
    }
    protected override void OnTriggerExit2D(Collider2D col)
    {
        base.OnTriggerExit2D(col);
        if (col.CompareTag("Player")) anim.SetBool("isOverlap", isOn);
    }
    protected override void OnInteract(InputAction.CallbackContext context)
    {
        base.OnInteract(context);
        if (!isOn) return;
        string[] gradeItem = new string[] { };
        switch (grade)
        {
            case ChestGrade.Normal:
                gradeItem = new string[] { "common", "uncommon" };
                break;
            case ChestGrade.Rare:
                gradeItem = new string[] { "common", "uncommon", "rare" };
                break;
            case ChestGrade.Epic:
                gradeItem = new string[] { "rare", "epic" };
                break;
            case ChestGrade.Legendary:
                gradeItem = new string[] { "epic", "legendary" };
                break;
            default:
                break;
        }
        Item[] items = ItemDataManager.GetItems(gradeItem);
        Item randomItem = items[Random.Range(0, items.Length)];
        if (PlayableCharacter.Inst.GetItem(randomItem)) ServiceHub.Get<ILogMessage>().Spawn($"일반 상자로부터 '{randomItem.name[1]}'을(를) 획득했습니다.");
        Destroy(gameObject);
    }
}

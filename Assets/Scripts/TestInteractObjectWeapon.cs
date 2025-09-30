using UnityEngine;
using UnityEngine.InputSystem;

public class TestInteractObjectWeapon : TestInteractItems
{
    public string weaponId;
    public Item data;
    protected override void Awake()
    {
        base.Awake();
        itemType = ItemType.Weapon;
        data = ItemDataManager.GetItem(weaponId);
    }
    protected override void OnInteract(InputAction.CallbackContext context)
    {
        if (!isOn) return;
        base.OnInteract(context);
        PlayableCharacter.Inst.SetMainWeapon(data);
    }
}

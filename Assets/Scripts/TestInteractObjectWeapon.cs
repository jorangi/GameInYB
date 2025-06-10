using UnityEngine;
using UnityEngine.InputSystem;

public class TestInteractObjectWeapon : TestInteractItems
{
    public WeaponData data;
    protected override void Awake()
    {
        base.Awake();
        itemType = ItemType.Weapon;
    }
    protected override void OnInteract(InputAction.CallbackContext context)
    {
        if (!isOn) return;
        base.OnInteract(context);
        Debug.Log(data.Id);
        PlayableCharacter.Inst.SetWeapon(data);
    }
}

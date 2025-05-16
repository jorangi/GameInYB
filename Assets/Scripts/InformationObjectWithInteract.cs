using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InformationObjectWithInteract : InformationObject
{
    InputSystem_Actions inputAction;
    private bool isOn = false;
    protected override void Awake()
    {
        base.Awake();
        inputAction = new();
    }
    private void OnEnable() {
        inputAction.Enable();
        inputAction.Player.Interact.performed += OnInteract;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (isOn) PlayableCharacter.Inst.ShowMessage(info);
    }

    public override void TriggerEnter(Collider2D col)
    {
        isOn = true;
    }
}

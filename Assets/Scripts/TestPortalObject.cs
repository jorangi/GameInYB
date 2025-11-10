using UnityEngine;
using UnityEngine.InputSystem;

public class TestPortalObject : InteractableObject
{
    public Transform targetPosition;
    protected override void OnInteract(InputAction.CallbackContext context)
    {
        base.OnInteract(context);
        if (isOn)
        {
            PlayableCharacter.Inst.transform.position = targetPosition.position;
        }
    }
}

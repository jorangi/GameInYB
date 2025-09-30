using UnityEngine;
using UnityEngine.InputSystem;

public class TestPortalObject : TestInteractItems
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

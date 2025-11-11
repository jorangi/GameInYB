using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractableObject : MonoBehaviour
{
    public enum ItemType
    {
        Item,
        Weapon,
        Object
    }
    [SerializeField]
    protected ItemType itemType;
    protected bool isOn = false;
    private void OnEnable()
    {
        if(PlayableCharacter.Inst.inputAction != null)
            PlayableCharacter.Inst.inputAction.Player.Interact.performed += OnInteract;
    }
    private void OnDisable()
    {
        if(PlayableCharacter.Inst.inputAction != null)
            PlayableCharacter.Inst.inputAction.Player.Interact.performed -= OnInteract;
    }
    protected virtual void OnInteract(InputAction.CallbackContext context)
    {
    }
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isOn = true;
        }
    }
    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isOn = false;
        }
    }
}

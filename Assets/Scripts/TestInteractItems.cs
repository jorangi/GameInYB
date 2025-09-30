using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestInteractItems : MonoBehaviour
{
    public enum ItemType
    {
        Item,
        Weapon,
        Object
    }
    [SerializeField]
    protected ItemType itemType;

    protected InputSystem_Actions inputAction;
    protected bool isOn = false;
    protected virtual void Awake()
    {
        inputAction = new();
    }
    private void OnEnable()
    {
        inputAction.Enable();
        inputAction.Player.Interact.performed += OnInteract;
    }

    protected virtual void OnInteract(InputAction.CallbackContext context)
    {
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isOn = true;
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isOn = false;
        }
    }
}

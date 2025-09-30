using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "TMPLinkEvent", menuName = "Scriptable Objects/TMPLinkEvent")]
public class TMPLinkEvent : ScriptableObject
{
    [Serializable]
    public enum EventType
    {
        MOUSEOVER,
        MOUSEOUT,
        CLICK
    }
    public struct TMPLinkEventPayload
    {
        public EventType type;
        public string id;
        public string linkText;
        public Vector2 screenPos;
        public Camera cam;
        public Canvas can;
        public TextMeshProUGUI source;
        public int index;
    }

    public class TMPLinkUnityEvent : UnityEvent<TMPLinkEventPayload> { }
    public event Action<TMPLinkEventPayload> OnRaised;
    [SerializeField] private TMPLinkUnityEvent e;
    public void Raise(TMPLinkEventPayload payload)
    {
        OnRaised?.Invoke(payload);
        e?.Invoke(payload);
    }
}

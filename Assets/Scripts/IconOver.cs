using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

public class IconOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SpriteRenderer sprite;
    private String desciption;

    private void Start()
    {
        //TMP_TextUtilities.FindIntersectingLink();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
    }
    public void OnPointerExit(PointerEventData eventData)
    {
    }
    private void SetImage(Sprite sprite)
    {
        this.sprite.sprite = sprite;
    }
}
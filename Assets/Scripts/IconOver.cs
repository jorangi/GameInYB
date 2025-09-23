using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

public class IconOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Item item;
    private SpriteRenderer sprite;
    private String desciption;
    private void SetItem(string itemId)
    {
        
    }
    private void Awake()
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
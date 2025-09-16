using UnityEngine;

public class MoveableInformationModal : MonoBehaviour
{
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
    public virtual void Move(Vector2 screenPos, Camera cam)
    {

    }
    public virtual void SetFollow(bool enabled = true)
    {

    }
    public virtual void SetOffset(Vector2 offset)
    {
        
    }
}

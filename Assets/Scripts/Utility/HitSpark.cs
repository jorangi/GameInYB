using UnityEngine;

public class HitSpark : MonoBehaviour
{
    public void OnEnable() => transform.SetAsLastSibling();
    public void OnFin() => gameObject.SetActive(false);
}

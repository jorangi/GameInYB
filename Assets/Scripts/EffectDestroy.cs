using UnityEngine;

public class EffectDestroy : MonoBehaviour
{
    public void OnEffectDestroy()
    {
        Destroy(gameObject);
    }
}
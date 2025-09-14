using System.Collections.Generic;
using UnityEngine;

public class AttackHitBox : MonoBehaviour
{
    public float timer = 0.2f;
    public float atk = 0;
    private List<GameObject> targets = new();
    void Awake()
    {
        name = "AttackHitBox";
        Destroy(gameObject, timer);
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && !targets.Contains(collision.gameObject))
        {
            targets.Add(collision.gameObject);
            collision.transform.parent.GetComponent<NonPlayableCharacter>().TakeDamage(atk);
        }
    }
}
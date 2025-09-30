using System.Collections.Generic;
using UnityEngine;

public class AttackHitBox : MonoBehaviour
{
    public float timer = 0.2f;
    private List<GameObject> targets = new();
    private CharacterStats stats;
    
    public IStatProvider provider;
    void Awake()
    {
        name = "AttackHitBox";
        stats = provider.GetStats();
        Destroy(gameObject, timer);
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && !targets.Contains(collision.gameObject))
        {
            targets.Add(collision.gameObject);
            collision.transform.parent.GetComponent<NonPlayableCharacter>().TakeDamage(stats.GetFinal(StatType.ATK));
        }
    }
}
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
        Destroy(gameObject, timer);
    }
    void Start()
    {
        stats = provider.GetStats();
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && collision.gameObject.layer == LayerMask.NameToLayer("HitBox") && !targets.Contains(collision.gameObject))
        {
            targets.Add(collision.gameObject);
            collision.transform.parent.GetComponent<NonPlayableCharacter>().TakeDamage(stats.GetFinal(StatType.ATK));
        }
    }
}
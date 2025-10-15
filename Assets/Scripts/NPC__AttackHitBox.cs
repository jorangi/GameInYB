using System.Collections.Generic;
using AnimationImporter.PyxelEdit;
using UnityEngine;

public class NPC__AttackHitBox : MonoBehaviour
{
    public float timer = 0.2f;
    private CharacterStats stats;
    private GameObject target;
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
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.layer == LayerMask.NameToLayer("HitBox") && target != collision.gameObject)
        {
            target=collision.gameObject;
            collision.transform.parent.GetComponent<PlayableCharacter>().TakeDamage(stats.GetFinal(StatType.ATK));
        }
    }
}
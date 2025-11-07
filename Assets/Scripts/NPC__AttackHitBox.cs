using System.Collections.Generic;

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
    }
    void Start()
    {
        stats = provider.GetStats();
    }
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
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
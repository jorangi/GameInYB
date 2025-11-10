using System.Collections.Generic;

using UnityEngine;

public class NPC__AttackHitBox : HitBox
{
    private GameObject target;
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.layer == LayerMask.NameToLayer("HitBox") && target != collision.gameObject)
        {
            target =collision.gameObject;
            collision.transform.parent.GetComponent<PlayableCharacter>().TakeDamage(stats.GetFinal(StatType.ATK));
        }
    }
}
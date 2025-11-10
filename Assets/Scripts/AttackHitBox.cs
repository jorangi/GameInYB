using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    private BoxCollider2D col;
    public float timer = 0.2f;
    protected CharacterStats stats;
    public IStatProvider provider;
    protected IHitManager hitManager = null;
    private void Awake()
    {
        hitManager = ServiceHub.Get<IHitManager>();
        col = col != null ? col : GetComponent<BoxCollider2D>();
    }
    protected virtual void OnEnable()
    {
        stats = provider?.GetStats();
        timer = 0.2f;
        transform.SetAsLastSibling();
    }
    protected virtual void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
            gameObject.SetActive(false);
    }
    protected virtual void OnTriggerEnter2D(Collider2D collision) { }
    public void SetSize(float sizeX, float sizeY) => col.size = new(sizeX, sizeY);
}
public class AttackHitBox : HitBox
{
    private readonly List<GameObject> targets = new();
    protected override void Update()
    {
        base.Update();
        if (timer <= 0)
        {
            targets.Clear();
        }
    }
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && collision.gameObject.layer == LayerMask.NameToLayer("HitBox") && !targets.Contains(collision.gameObject))
        {
            targets.Add(collision.gameObject);
            float r = Random.value;
            if (r < stats.GetFinal(StatType.CRI))
            {
                hitManager.GetCriticalEffect().transform.position = (Vector2)collision.transform.position + Random.insideUnitCircle * collision.bounds.size * 0.5f;
                collision.transform.parent.GetComponent<NonPlayableCharacter>().TakeDamage(stats.GetFinal(StatType.ATK) * stats.GetFinal(StatType.CRID), true);
                AudioManager.Inst.PlaySFX("monster_critical_hit");
            }
            else
            {
                hitManager.GetHitEffect().transform.position = (Vector2)collision.transform.position + Random.insideUnitCircle * collision.bounds.size * 0.5f;
                collision.transform.parent.GetComponent<NonPlayableCharacter>().TakeDamage(stats.GetFinal(StatType.ATK));
                AudioManager.Inst.PlaySFX("monster_hit");
            }
        }
    }
}
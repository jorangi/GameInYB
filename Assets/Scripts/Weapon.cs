using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
[System.Serializable]
public class ItemData
{
    [SerializeField] private string id;
    [HideInInspector] public string Id => id;
    [SerializeField] private string name;
    [HideInInspector] public string Name => name;
    [SerializeField] private string description;
    [HideInInspector] public string Description => description;
    [SerializeField] private Sprite icon;
    [HideInInspector] public Sprite Icon => icon;
}
[System.Serializable]
public class WeaponData : ItemData
{
    [SerializeField] private Sprite sprite;
    [HideInInspector] public Sprite Sprite => sprite;
    [SerializeField] private float atk;
    [HideInInspector] public float Atk => atk;
    [SerializeField] private float ats;
    [HideInInspector] public float Ats => ats;
    [SerializeField] private float cri;
    [HideInInspector] public float Cri => cri;
    [SerializeField] private float crid;
    [HideInInspector] public float Crid => crid;
}

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Weapon : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public BoxCollider2D boxCollider;
    private PlayableCharacter player;
    public GameObject attackHitBox;
    private const int MAX_SWING_COUNT = 2; // Maximum number of swings allowed
    public Animator anim;
    private float Atk;
    private Transform hitPoint;
    private void Awake()
    {
        hitPoint = transform.GetChild(0);
        anim = GetComponent<Animator>();
        UpdateColliderToSprite();
    }
    public void SetAts(float ats)
    {
        anim.SetFloat("attackSpd", ats);
        UpdateColliderToSprite();
    }
    public void SetPlayerAtk(float atk) => this.Atk = atk;
    private void Update()
    {
        if (anim.GetBool("IsSwing"))
        {
            Vector2 center = (Vector2)transform.position;
            Vector2 size = boxCollider.size;
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, LayerMask.GetMask("HitBox")); // LayerMask 생략
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy") && hit.transform.parent.TryGetComponent(out NonPlayableCharacter enemy) && !targets.Contains(enemy))
                {
                    targets.Add(enemy);
                }
            }
        }
        else
        {
            targets.Clear();
        }
    }
    public void StartSwing()
    {
        UpdateColliderToSprite();
        if (anim.GetBool("IsSwing")) return;
        anim.SetInteger("SwingCount", (anim.GetInteger("SwingCount") + 1) % MAX_SWING_COUNT);
        anim.SetBool("IsSwing", true);
    }
    public void SwingEnd()
    {
        anim.SetBool("IsSwing", false);
    }
    public void StopSwing()
    {
        anim.SetInteger("SwingCount", 0);
        SwingEnd();
    }
    public List<NonPlayableCharacter> targets = new();
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(boxCollider.bounds.center, boxCollider.bounds.size);
    }
    /// <summary>
    /// 공격 순간 히트박스 생성
    /// </summary>
    public void CheckHit()
    {
        GameObject hitbox = Instantiate(attackHitBox);
        hitbox.GetComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
        hitbox.GetComponent<AttackHitBox>().atk = Atk;
        hitbox.transform.position = hitPoint.position;
        hitbox.transform.localScale = new(2f, 2f);
    }
    public void UpdateColliderToSprite()
    {
        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null) return;

        Vector2 spriteSize = sprite.bounds.size; // 유니티 단위(월드 단위)

        boxCollider.size = spriteSize;
        boxCollider.offset = sprite.bounds.center; // 스프라이트 pivot이 중앙이 아닌 경우 대비
    }
}

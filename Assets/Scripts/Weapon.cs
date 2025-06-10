using UnityEngine;
using Sirenix.OdinInspector;
[System.Serializable]
public class ItemData
{
    [SerializeField]private string id;
    [HideInInspector] public string Id => id;
    [SerializeField] private string name;
    [HideInInspector] public string Name => name;
    [SerializeField]private string description;
    [HideInInspector] public string Description => description;
    [SerializeField]private Sprite icon;
    [HideInInspector] public Sprite Icon => icon;
}
[System.Serializable]
public class WeaponData : ItemData
{
    [SerializeField]private Sprite sprite;
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
public class Weapon : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public BoxCollider2D boxCollider;
    private PlayableCharacter player;
    private NonPlayableCharacter target;
    private const int MAX_SWING_COUNT = 2; // Maximum number of swings allowed
    public Animator anim;
    public void SetAts(float ats)
    {
        anim.SetFloat("attackSpd", ats);
        UpdateColliderToSprite();
    }
    public void SetPlayer(PlayableCharacter player)
    {
        this.player = player;
    }
    private void Awake()
    {
        anim = GetComponent<Animator>();
        UpdateColliderToSprite();
    }
    public void StartSwing()
    {
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
    void OnDrawGizmosSelected()
    {
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireCube(transform.position + offset, size);
    }
    public void Hit()
    {
        Debug.Log("Hit!!");
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxCollider.size, 0f, LayerMask.GetMask("HitBox"));
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") && hit.transform.parent.TryGetComponent<NonPlayableCharacter>(out var enemy))
            {
                Debug.Log(hit.tag);
                enemy.TakeDamage(player.Data.Atk);
            }
        }
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

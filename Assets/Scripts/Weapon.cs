using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Playables;
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

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Weapon : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    private PlayableCharacter player;
    public GameObject attackHitBox;
    private const int MAX_SWING_COUNT = 2; // Maximum number of swings allowed
    public Animator anim;
    private Transform hitPoint;
    private CharacterStats stats;
    private IStatProvider provider;
    private void Awake()
    {
        Debug.Log(PlayableCharacter.Inst);
        Debug.Log(PlayableCharacter.Inst.Data);
        Debug.Log(PlayableCharacter.Inst.Data.GetStats());
        stats ??= PlayableCharacter.Inst.Data.GetStats();
        provider = stats as IStatProvider;
        provider.GetStats().OnRecalculated += OnStatChanged;
        if (provider == null) Debug.LogError("[WeaponScript] provider에 stats할당 실패");
        hitPoint = transform.GetChild(0);
        anim = GetComponent<Animator>();
    }
    public void OnStatChanged()
    {
        SetAts(provider.GetStats().GetFinal(StatType.ATS));
    }
    public void SetAts(float ats)
    {
        anim.SetFloat("attackSpd", ats);
    }
    private void Update()
    {
        targets.Clear();
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
    public List<NonPlayableCharacter> targets = new();
    /// <summary>
    /// 공격 순간 히트박스 생성
    /// </summary>
    public void CheckHit()
    {
        GameObject hitbox = Instantiate(attackHitBox);
        hitbox.GetComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
        hitbox.GetComponent<AttackHitBox>().provider = provider;
        hitbox.transform.position = hitPoint.position;
        hitbox.transform.localScale = new(2f, 2f);
    }
}

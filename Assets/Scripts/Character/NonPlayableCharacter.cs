using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;

public class NonPlayableCharacterData : CharacterData
{
    public NonPlayableCharacterData(string name) : base(name) { }
    public override string ToString()
    {
        return $"{base.ToString()}";
    }
}

[RequireComponent(typeof(Animator))]
public class NonPlayableCharacter : Character
{
    private static WaitForSeconds _waitForSeconds0_3 = new WaitForSeconds(0.3f);
    [Header("FSM관련")]
    public Blackboard blackboard = new();
    private NPCStateMachine _fsm;
    private StateRegistry _registry = new();
    private bool dieAnimFinished;

    // 예시 유틸
    public void ApplyImpulse(Vector2 v) { /* Rigidbody2D에 순간 힘/속도 적용 */ }
    public void BeginDash(Vector2 vel, float time) { /* time 동안 vel 유지(충돌/멈춤 로직 포함) */ }

    [Header("데이터 관련")]
    public CharacterData Data => data;
    private NonPlayableCharacterData _npcData;
    public NonPlayableCharacterData NPCData => _npcData;
    private IStatProvider provider;

    [Header("기타")]
    public GameObject attackHitBox;
    public SpriteRenderer sprite;
    protected enum State
    {
        Idle,
        Move,
        Chase,
        Attack,
        Hit,
        Dead
    }
    private Transform wallChecker;
    public float idleTimer = 0.0f;
    public float moveTimer = 0.0f;
    private const float hpBarSpd = 1.0f;
    private SpriteRenderer hpBar, hpBarSec;
    private bool isHealing;
    private Coroutine hpSmooth;
    private const float BAR_SIZE = 3.5f;
    public GameObject DamageTextPrefab;
    public Animator animator;
    [SerializeField] private string id;
    protected override void Awake()
    {
        data = new NonPlayableCharacterData(id);
        data.GetStats().SetBase(StatType.ATK, 50);
        data.SetInvicibleTime(0.5f);
        data.SetHitStunTime(0.5f);
        hpBar = transform.Find("HealthBar").Find("back").Find("healthBarMask").Find("health").GetComponent<SpriteRenderer>();
        hpBarSec = transform.Find("HealthBar").Find("back").Find("healthBarMask").Find("healthSec").GetComponent<SpriteRenderer>();
        wallChecker = transform.Find("wallChecker").transform;
        idleTimer = UnityEngine.Random.Range(0.0f, 1.0f);
        animator = animator != null ? animator : GetComponent<Animator>();
        animator.applyRootMotion = false;
        base.Awake();
        _npcData = data as NonPlayableCharacterData;
        _npcData.health.ApplyHP(_npcData.MaxHP);
        FSMInit();


        provider = Data;
        if (provider is null) Debug.LogError("[WeaponScript] provider에 stats할당 실패");
    }
    public void FSMInit()
    {
        _fsm = new();
        _registry.Register(new IdleState(this, blackboard));
        _registry.Register(new WanderState(this, blackboard));
        _registry.Register(new ChaseState(this, blackboard));
        _registry.Register(new AttackState(this, blackboard));
        _registry.Register(new HitState(this, blackboard));
        _registry.Register(new DieState(this, blackboard));
        _registry.Register(new DistancingState(this, blackboard));
    }
    public void OnEnable()
    {
        RequestState<IdleState>();
    }
    protected override void Update()
    {
        base.Update();
        blackboard.TimeNow = Time.time;
        blackboard.DistToTarget = Vector2.Distance(blackboard.self.position, blackboard.target.position);
        blackboard.CanSeeTarget = Mathf.Sign(blackboard.target.position.x - blackboard.self.position.x) == Mathf.Sign(FacingSign);
        wallChecker.localPosition = new(FacingSign > 0 ? 0.25f : -0.25f, 0.0f);
        RaycastHit2D hitWall = Physics2D.Raycast(wallChecker.position, FacingSign > 0 ? Vector2.right : Vector2.left, 0.1f, LayerMask.GetMask("Floor", "Platform"));
        blackboard.IsWallAhead = hitWall;
        blackboard.IsPrecipiceAhead = isPrecipice.collider == null;

        _fsm.Update();
    }
    protected override void Movement()
    {
        base.Movement();
        sprite.flipX = desiredMoveX > 0 || desiredMoveX >= 0 && sprite.flipX;
    }
    IEnumerator HpBarFillsSmooth(SpriteRenderer bar)
    {
        yield return _waitForSeconds0_3;

        float r = (float)Mathf.FloorToInt(Data.health.HP) / Mathf.FloorToInt(Data.MaxHP);
        while (Mathf.Abs(bar.size.x / BAR_SIZE - r) > 0.01f)
        {
            bar.size = new(
            Mathf.Lerp(bar.size.x, r * BAR_SIZE, Time.deltaTime * hpBarSpd)
            , 0.5f);
            bar.transform.localPosition = new(-1.75f + 1.75f * bar.size.x / BAR_SIZE, 0f);
            yield return null;
        }
        bar.transform.localPosition = new(-1.75f + 1.75f * bar.size.x / BAR_SIZE, 0f);
        bar.size = new(r * BAR_SIZE, 0.5f);
    }
    public void SetHP(int value)
    {
        isHealing = Mathf.FloorToInt(value) > Mathf.FloorToInt(Data.health.HP);
        Data.health.ApplyHP(value);
    }
    protected override void OnHPChanged()
    {
        base.OnHPChanged();
        //HpBar fills out smoothly
        if (hpSmooth != null)
            StopCoroutine(hpSmooth);
        float r = (float)Mathf.FloorToInt(Data.health.HP) / Mathf.FloorToInt(Data.MaxHP);
        if (isHealing)
        {
            hpBar.transform.localPosition = new(-1.75f + 1.75f * r, 0f);
            hpBar.size = new(r * BAR_SIZE, 0.5f);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBarSec));
        }
        else
        {
            hpBarSec.transform.localPosition = new(-1.75f + 1.75f * r, 0f);
            hpBarSec.size = new(r * BAR_SIZE, 0.5f);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBar));
        }

        if (Data.health.HP <= 0)
        {
            RequestState<DieState>();
        }
    }
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        if (damage < 0.0f) return;
        int dmg = Mathf.RoundToInt(damage - data.Def);
        if (dmg < 0) dmg = 0;
        SetHP(data.health.HP - dmg);

        GameObject dObj = Instantiate(DamageTextPrefab);
        dObj.transform.position = transform.position + new Vector3(0, 2f, 0);
        dObj.GetComponent<TextMeshPro>().text = Mathf.RoundToInt(dmg).ToString();
        if (data.health.HP <= 0)
        {
            // Handle death logic here
            Debug.Log($"{data.UnitName} has died.");
        }
    }
    protected override IEnumerator Hit()
    {
        RequestState<HitState>();
        yield return null;
        float oriX = desiredMoveX;
        hitCoroutine = null;
        SetDesiredMove(oriX);
    }
    public void RequestState<T>() where T : class, IStateBase
    {
        var next = _registry.Get<T>();
        if (next is null) return;
        _fsm.SetState(next);
    }
    public void AnimTrigger(string triggerName) => animator.SetTrigger(triggerName);
    public bool AnimIs(string stateName) => animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    public void OnAttackStart() { SetRooted(true); SetDesiredMove(0); }
    public void OnHitFrame() {/*히트박스 활성/데미지 처리*/}
    public void OnAttackEnd() { SetRooted(false); }

    public bool AnimGetTriggerAttack() => animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
    public void AnimSetMoving(bool on) => animator.SetBool("IsMoving", on);
    public void AnimTriggerAttack() => animator.SetTrigger("Attack");
    public void AnimTriggerHit() => animator.SetTrigger("Hit");
    public void AnimTriggerDeath() => animator.SetTrigger("Die");
    public bool InMinStateLock() => blackboard.TimeNow < blackboard.MinStateEndTime;
    public void SetMinStateLock(float minStateDuration) => blackboard.MinStateEndTime = blackboard.TimeNow + blackboard.MinStateDuration;
    public void OnDieAnimationComplete() => dieAnimFinished = true;
    public bool IsDieAnimFinished() => dieAnimFinished;
    public void CheckHit()
    {
        GameObject hitbox = Instantiate(attackHitBox);
        hitbox.GetComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
        hitbox.GetComponent<NPC__AttackHitBox>().provider = provider;
        hitbox.transform.position = transform.Find("attackPosition").position;
        hitbox.transform.localScale = new(2f, 2f);
    }
    /// <summary>
    /// EngageState 등록(후처리)
    /// </summary>
    /// <param name="engage"></param>
    public void BindEngage(EngageState engage) => _registry.Register(engage);
    /// <summary>
    /// abilities를 이용한 등록
    /// </summary>
    /// <param name="abilities"></param>
    public void BindEngage(List<IAbility> abilities)
    {
        var engage = new EngageState(this, blackboard, abilities);
        BindEngage(engage);
    }
    private void AnimSetAttack(int idx) => animator.SetFloat("AttackIndex", idx);
    private void AnimDoAttack()
    {
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
    }
    public void AnimPlayAttack(int idx)
    {
        AnimSetAttack(idx);
        AnimDoAttack();
    }
}
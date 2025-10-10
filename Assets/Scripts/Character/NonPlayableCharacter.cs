using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class NonPlayableCharacterData : CharacterData
{
    public NonPlayableCharacterData(CharacterData data) : base(data.UnitName){}
    public override string ToString()
    {
        return $"{base.ToString()}";
    }
}

public class NonPlayableCharacter : Character
{
    [Header("FSM관련")]
    public Blackboard blackboard = new();
    private NPCStateMachine _fsm;
    private StateRegistry _registry = new();

    [Header("기타")]
    public SpriteRenderer sprite;
    protected NonPlayableCharacterData Data => (NonPlayableCharacterData)data;
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
    [SerializeField]
    private State state = State.Idle;
    public float idleTimer = 0.0f;
    public float moveTimer = 0.0f;
    private const float hpBarSpd = 5.0f;
    public TextMeshPro behaviourPointer;
    private SpriteRenderer hpBar, hpBarSec;
    private bool isHealing;
    private Coroutine hpSmooth;
    private const float BAR_SIZE = 3.5f;
    public GameObject DamageTextPrefab;
    public Animator animator;
    protected override void Awake()
    {
        base.Awake();
        data = new NonPlayableCharacterData(new CharacterData("TestEnemy"));
        FSMInit();
        hpBar = transform.Find("HealthBar").Find("back").Find("healthBarMask").Find("health").GetComponent<SpriteRenderer>();
        hpBarSec = transform.Find("HealthBar").Find("back").Find("healthBarMask").Find("healthSec").GetComponent<SpriteRenderer>();
        wallChecker = transform.Find("wallChecker").transform;
        behaviourPointer = GetComponentInChildren<TextMeshPro>();
        idleTimer = Random.Range(0.0f, 1.0f);
        animator = animator != null ? animator : GetComponent<Animator>();
        animator.applyRootMotion = false;
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
        
        behaviourPointer.text = $"{blackboard.IsPrecipiceAhead}";
        _fsm.Update();
    }
    protected override void Movement()
    {
        base.Movement();
        sprite.flipX = desiredMoveX > 0 ? true : desiredMoveX < 0 ? false : sprite.flipX;
    }
    IEnumerator HpBarFillsSmooth(SpriteRenderer bar)
    {
        yield return new WaitForSeconds(0.3f);

        float r = (float)Mathf.FloorToInt(Data.HP) / Mathf.FloorToInt(Data.MaxHP);
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
        isHealing = Mathf.FloorToInt(value) > Mathf.FloorToInt(Data.HP);

        Data.HP = value;
        //HpBar fills out smoothly
        if (hpSmooth != null)
            StopCoroutine(hpSmooth);
        float r = (float)Mathf.FloorToInt(value) / Mathf.FloorToInt(Data.MaxHP);
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
    }
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        if (damage < 0.0f) return;
        int dmg = Mathf.RoundToInt(damage - data.Def);
        if (dmg < 0) dmg = 0;
        SetHP(data.HP - dmg);

        GameObject dObj = Instantiate(DamageTextPrefab);
        dObj.transform.position = transform.position + new Vector3(0, 2f, 0);
        dObj.GetComponent<TextMeshPro>().text = Mathf.RoundToInt(dmg).ToString();
        if (data.HP <= 0)
        {
            // Handle death logic here
            Debug.Log($"{data.UnitName} has died.");
        }
    }
    private State tempState;
    protected override IEnumerator Hit()
    {
        if (state != State.Hit)
        {
            tempState = state;
        }
        state = State.Hit;
        RequestState<HitState>();
        InvincibleTimer = data.InvincibleTime;
        HitStunTimer = data.HitStunTime;
        hitBox.enabled = false;
        float colorVal = 0f;
        float elapsedTime = 0f;
        sprite.color = Color.red;

        float oriX = desiredMoveX;

        while (state == State.Hit && HitStunTimer > 0.0f)
        {
            SetDesiredMove(0f);
            behaviourPointer.SetText($"Hit : {Mathf.Round(HitStunTimer * 10) * 0.1f}");
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / data.HitStunTime;
            colorVal = Mathf.Lerp(colorVal, 1f, t);
            sprite.color = new Color(1, colorVal, colorVal, 1);
            HitStunTimer -= Time.deltaTime;

            yield return null;
        }
        state = tempState;
        sprite.color = new Color(1, 1, 1, 1);
        hitCoroutine = null;
        state = tempState;
        SetDesiredMove(oriX);
    }
    public void RequestState<T>() where T : class, IStateBase
    {
        var next = _registry.Get<T>();
        if (next == null) return;
        _fsm.SetState(next);
    }
    public void AnimSetMoving(bool on) => animator.SetBool("IsMoving", on);
    public void AnimTriggerAttack() => animator.SetTrigger("Attack");
    public void AnimTriggerHit() => animator.SetTrigger("Hit");
    public void AnimTriggerDeath() => animator.SetTrigger("Die");
    public bool InMinStateLock() => blackboard.TimeNow < blackboard.MinStateEndTime;
    public void SetMinStateLock(float minStateDuration) => blackboard.MinStateEndTime = blackboard.TimeNow + blackboard.MinStateDuration; 
}
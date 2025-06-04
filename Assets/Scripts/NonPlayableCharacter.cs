using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class NonPlayableCharacterData : CharacterData
{
    public NonPlayableCharacterData(CharacterData data) : base(data.UnitName)
    {
        Spd = data.Spd;
        HP = data.HP;
        MaxHP = data.MaxHP;
        Atk = data.Atk;
        Ats = data.Ats;
        Def = data.Def;
    }
    public override string ToString()
    {
        return $"{base.ToString()}";
    }
}
public class NonPlayableCharacter : Character
{
    protected NonPlayableCharacterData Data => (NonPlayableCharacterData)data;
    protected enum State
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Hit,
        Dead
    }
    private Transform wallChecker;
    private State state;
    public float idleTimer = 0.0f;
    public float moveTimer = 0.0f;
    private bool moveDir; //f : left, t : right
    private const float hpBarSpd = 5.0f;
    private TextMeshPro behaviourPointer;
    private SpriteRenderer hpBar, hpBarSec;
    private bool isHealing;
    private Coroutine hpSmooth;
    private const float BAR_SIZE = 3.5f;
    protected override void Awake()
    {
        base.Awake();
        data = new NonPlayableCharacterData(new CharacterData("TestEnemy"));
        hpBar = transform.Find("HealthBar").Find("back").Find("healthBarMask").Find("health").GetComponent<SpriteRenderer>();
        hpBarSec = transform.Find("HealthBar").Find("back").Find("healthBarMask").Find("healthSec").GetComponent<SpriteRenderer>();
        wallChecker = transform.Find("wallChecker").transform;
        behaviourPointer = GetComponentInChildren<TextMeshPro>();
        idleTimer = Random.Range(0.0f, 1.0f);

    }
    protected override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.U))
        {
            SetHP(Random.Range(0, Data.MaxHP));
        }
        wallChecker.localPosition = new(moveDir ? 0.25f : -0.25f, 0.0f);
        RaycastHit2D hitWall = Physics2D.Raycast(wallChecker.position, moveDir ? Vector2.right : Vector2.left, 0.1f, LayerMask.GetMask("Floor", "Platform"));

        if (idleTimer > 0.0f)
        {
            idleTimer -= Time.deltaTime;
            behaviourPointer.SetText($"Idle : {Mathf.Round(idleTimer * 10) * 0.1f}");
            state = State.Idle;
            if (idleTimer <= 0.0f && moveTimer <= 0.0f)
            {
                moveTimer = Random.Range(1.0f, 5.0f);
                moveDir = Random.Range(0, 2) != 0;
                moveVec = new(moveDir ? 1.0f : -1.0f, 0f);
                state = State.Patrol;
            }
        }
        if (moveTimer > 0.0f)
        {
            moveTimer -= Time.deltaTime;
            behaviourPointer.SetText($"{(moveDir ? "right" : "left")} : {Mathf.Round(moveTimer * 10) * 0.1f}");
            if (moveTimer <= 0.0f && idleTimer <= 0.0f)
            {
                moveVec = Vector2.zero;
                rigid.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                idleTimer = Random.Range(0.0f, 5.0f);
                state = State.Idle;
            }
        }

        if (hitWall)
        {
            if (Random.Range(0.0f, 1.0f) > 0.7f)
            {
                moveDir = !moveDir;
                moveVec = Vector2.zero;
                rigid.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                idleTimer = Random.Range(0.0f, 5.0f);
            }
            else
            {
                moveDir = !moveDir;
                moveVec = new(moveDir ? 1.0f : -1.0f, 0f);
                behaviourPointer.SetText($"{(moveDir ? "right" : "left")} : {Mathf.Round(moveTimer * 10) * 0.1f}");
                state = State.Patrol;
            }
        }
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
        if (damage < 0.0f) return;
        int dmg = Mathf.RoundToInt(damage - data.Def);
        if (dmg < 0) dmg = 0;
        SetHP(data.HP - dmg);
        if (data.HP <= 0)
        {
            // Handle death logic here
            Debug.Log($"{data.UnitName} has died.");
        }
    }
}
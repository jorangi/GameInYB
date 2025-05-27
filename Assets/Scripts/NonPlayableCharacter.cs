using TMPro;
using UnityEngine;

public class NonPlayableCharacter : Character
{
    protected enum State
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Hit,
        Dead
    }
    private State state;
    public float idleTimer = 0.0f;
    public float moveTimer = 0.0f;
    private bool moveDir; //f : left, t : right
    private TextMeshPro behaviourPointer;
    private void Awake()
    {
        behaviourPointer = GetComponentInChildren<TextMeshPro>();
        idleTimer = Random.Range(0.0f, 1.0f);
    }
    protected override void Update()
    {
        base.Update();
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
            }
        }

        RaycastHit2D[] frontHits = Physics2D.RaycastAll(frontRay.position, moveDir ? Vector2.right : Vector2.left, 1f, LayerMask.GetMask(""));
        Debug.DrawLine(frontRay.position, (Vector2)frontRay.position + (moveDir ? Vector2.right : Vector2.left), Color.red);
    }
}

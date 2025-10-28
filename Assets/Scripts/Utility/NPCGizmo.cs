using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(NonPlayableCharacter))]
public class NPCGizmo : MonoBehaviour
{
    public Character owner;
    private Blackboard _bb;
    public Blackboard BB => _bb;
    public Transform frontRay;
    public Transform foot;

    public Vector2 attackBoxSize => new(BB.AttackEnter, BB.AttackEnter);
    public Vector2 attackBoxOffset => new(BB.AttackEnter * 0.5f + BB.AttackExit, 0.3f);

    void OnEnable()
    {
        _bb = GetComponent<NonPlayableCharacter>().blackboard;
    }
    void OnDrawGizmos()
    {
        if (BB is null) return;
        Vector3 pos = transform.position;

#if UNITY_EDITOR
        Handles.color = new Color(0f, 0.7f, 1f, 0.8f);
        Handles.DrawWireDisc(pos, Vector3.forward, BB.DetectEnter);
        Handles.color = new Color(0f, 0.5f, 1f, 0.4f);
        Handles.DrawWireDisc(pos, Vector3.forward, BB.DetectExit);

        Handles.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Handles.DrawWireDisc(pos, Vector3.forward, BB.AttackEnter);
#else
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.8f); Gizmos.DrawWireSphere(pos, BB.DetectEnter);
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.4f); Gizmos.DrawWireSphere(pos, BB.DetectExit);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f); Gizmos.DrawWireSphere(pos, BB.AttackEnter);
#endif
        if (frontRay)
        {
            Vector2 dir = (owner.FacingSign >= 0 ? Vector2.right : Vector2.left);
            Debug.DrawLine(frontRay.position, (Vector2)frontRay.position + dir * 0.2f, Color.yellow);
            
            Vector2 cliffDir = (owner.FacingSign >= 0 ? new Vector2(1, -2.5f) : new Vector2(-1, -2.5f)).normalized;
            Debug.DrawLine(frontRay.position, (Vector2)frontRay.position + cliffDir * 1.0f, new Color(1f, 0.5f, 0f, 1f));
        }

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Vector3 boxCenter = pos + new Vector3(attackBoxOffset.x * Mathf.Sign(owner.FacingSign), attackBoxOffset.y, 0f);
        Gizmos.DrawWireCube(boxCenter, new Vector3(attackBoxSize.x, attackBoxSize.y, 0f));
    }
}

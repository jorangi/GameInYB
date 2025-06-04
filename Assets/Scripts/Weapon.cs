using UnityEngine;

public class Weapon : MonoBehaviour
{
    private PlayableCharacter player;
    private NonPlayableCharacter target;
    private const int MAX_SWING_COUNT = 2; // Maximum number of swings allowed
    public Animator anim;
    public void SetPlayer(PlayableCharacter player)
    {
        this.player = player;
    }
    private void Awake()
    {
        anim = GetComponent<Animator>();
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
    public void Hit()
    {
        if (target == null) return;
        target.TakeDamage(player.Data.Atk);
    }
    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            target = collision.transform.parent.GetComponent<NonPlayableCharacter>();
        }
    }
}

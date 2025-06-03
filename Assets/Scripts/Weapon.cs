using UnityEngine;

public class Weapon : MonoBehaviour
{
    private Animator anim;
    public int swingCount = 0;
    public bool isSwing;
    private void Awake()
    {
        anim = GetComponent<Animator>();
    }
    public void StartSwing(){
        if (anim.GetBool("IsSwing")) return;
        anim.SetInteger("SwingCount", (anim.GetInteger("SwingCount") + 1) % 2);
        anim.SetBool("IsSwing", true);
        isSwing = true;
    }
    public void SwingEnd()
    {
        anim.SetBool("IsSwing", false);
        isSwing = false;
    }
    public void StopSwing()
    {
        anim.SetInteger("SwingCount", 0);
    }
}

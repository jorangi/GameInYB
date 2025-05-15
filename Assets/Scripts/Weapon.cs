using UnityEngine;

public class Weapon : MonoBehaviour
{
    private Animator anim;
    public bool isSwing;
    private void Awake()
    {
        anim = GetComponent<Animator>();
    }
    public void StartSwing(){
        if(anim.GetBool("IsSwing")) return;
        anim.SetBool("IsSwing", true);
        isSwing = true;
    }
    public void StopSwing(){
        anim.SetBool("IsSwing", false);
        isSwing = false;
    }
}

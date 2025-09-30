using UnityEngine;

public interface IInformation{
    public abstract void TriggerEnter(Collider2D col);
    public abstract void TriggerExit(Collider2D col);
}
[DisallowMultipleComponent]
public class InformationObject : ParentObject, IInformation
{
    public string[] info;
    protected virtual void Awake(){
    }
    private void OnTriggerEnter2D(Collider2D col)=>TriggerEnter(col);
    private void OnTriggerExit2D(Collider2D col)=>TriggerExit(col);

    public virtual void TriggerEnter(Collider2D col)
    {
        if(col.gameObject.layer.Equals(LayerMask.NameToLayer("Player"))){
            PlayableCharacter.Inst.ShowMessage(info);
        }
    }

    public virtual void TriggerExit(Collider2D col)
    {
        if(col.gameObject.CompareTag("Player")) PlayableCharacter.Inst.ShowMessage();
    }
}

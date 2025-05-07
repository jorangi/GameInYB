using UnityEngine;

public interface IInformation{
}
public class InformationObject : ParentObject, IInformation
{
    public string[] info;
    protected virtual void Awake(){

    }
    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        Debug.Log(col.gameObject.layer.Equals(LayerMask.NameToLayer("Player")));
        if(col.gameObject.layer.Equals(LayerMask.NameToLayer("Player"))){
            PlayableCharacter.Inst.ShowMessage(info);
        }
    }
    protected virtual void OnTriggerExit2D(Collider2D col)
    {
        
        PlayableCharacter.Inst.ShowMessage();
    }
}

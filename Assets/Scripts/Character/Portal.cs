using UnityEngine;

public class Portal : MonoBehaviour
{
    public string nextMapName;
    public void PortalOn() => transform.GetChild(0).gameObject.SetActive(true);
    private void Awake()
    {
        nextMapName = $"Forest_Map_0{UnityEngine.Random.Range(1, 9)}";
    }
    private void Start()
    {
        ServiceHub.Get<ISceneManager>().PortalRegistry(this);
    }
}

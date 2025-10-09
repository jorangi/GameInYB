using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayableCharacter playableCharacter;
    [SerializeField] private CharacterInformation characterInformation;

    public static UnityServiceProvider ServiceProvider { get; private set; }
    private void Awake()
    {
        if (ServiceProvider != null) return;

        var sp = new UnityServiceProvider();

        sp.Add<INegativeSignal>(uiManager);
        sp.Add<IInventoryData>(playableCharacter);
        sp.Add<IInventoryUI>(characterInformation);

        ServiceProvider = sp;
    }
}

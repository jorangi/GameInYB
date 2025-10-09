using UnityEngine;

public class NPCStateMachine
{
    private IStateBase _current;
    public IStateBase Current => _current;
    public void SetState(IStateBase next)
    {
        if (_current == next) return;
        _current?.Exit();
        _current = next;
        _current?.Enter();
    }
    public void Update()
    {
        _current?.Update();
    }
}

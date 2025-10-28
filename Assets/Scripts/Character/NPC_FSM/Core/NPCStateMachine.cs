using System;
using UnityEngine;

[Serializable]
public class NPCStateMachine
{
    private IStateBase _current;
    public IStateBase Current => _current;
    public string stateName;
    public void SetState(IStateBase next)
    {
        if (_current == next) return;
        Debug.Log(_current?.Name + " -> " + next?.Name);
        _current?.Exit();
        _current = next;
        _current?.Enter();
    }
    public void Update()
    {
        _current?.Update();
        stateName = _current.Name;
    }
}

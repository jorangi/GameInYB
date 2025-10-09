using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StateRegistry
{
    private readonly Dictionary<Type, IStateBase> _map = new();
    public void Register(IStateBase state)
    {
        _map[state.GetType()] = state;
    }
    public T Get<T>() where T : class, IStateBase
    {
        if (_map.TryGetValue(typeof(T), out var s)) return (T)s;
        return null;
    }

    public IStateBase Get(Type t) => _map.TryGetValue(t, out var s) ? s : null;
}

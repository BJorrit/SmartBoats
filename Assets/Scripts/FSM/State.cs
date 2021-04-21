using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class State
{
    protected readonly AISystem _system;
    public State(AISystem system)
    {
        _system = system;
    }
    public virtual IEnumerator Wander()
    {
        yield break;
    }
    public virtual IEnumerator Follow()
    {
        yield break;
    }
    public virtual IEnumerator Attack()
    {
        yield break;
    }
    public virtual IEnumerator Melee()
    {
        yield break;
    }
}

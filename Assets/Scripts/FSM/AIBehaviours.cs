using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Random = UnityEngine.Random;

public class AIBehaviours : State
{
    public AIBehaviours(AISystem system) : base(system)
    {

    }

    public override IEnumerator Wander()
    {

        if (_system.NeedsDestination())
        {
            _system.GetDestination();
        }

        _system.transform.rotation = _system._desiredRotation;

        var rayColor = _system.IsPathBlocked() ? Color.red : Color.green;
        Debug.DrawRay(_system.transform.position, _system._direction * _system._rayDistance, rayColor);

        _system.transform.Translate(Vector3.forward * Time.deltaTime * _system.wanderSpeed);

        while (_system.IsPathBlocked())
        {
            _system.GetDestination();
        }
        yield break;
    }

    public override IEnumerator Follow()
    {
        _system.transform.LookAt(_system.objectToFollow.transform);
        _system.transform.Translate(Vector3.forward * Time.deltaTime * 5f);

        yield break;
    }

    public override IEnumerator Attack()
    {
        GameObject _objectToFollow = _system.objectToFollow;
        if (_system.objectToFollow != null)
        {
            _system.ShootingAttack(_objectToFollow);
        }
        else
        {
            _system.SetState(new AIBehaviours(_system));
        }
        yield break;
    }

    public override IEnumerator Melee()
    {
        GameObject _objectToFollow = _system.objectToFollow;
        if (_system.objectToFollow != null)
        {
            _system.MeleeAttack(_objectToFollow);
        }

            _system.SetState(new AIBehaviours(_system));

        yield break;
    }

}

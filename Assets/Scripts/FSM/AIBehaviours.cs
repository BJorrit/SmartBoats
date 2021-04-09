using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

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
        //float interpolation = _system.followSpeed * Time.deltaTime;
        //GameObject _objectToFollow = _system.objectToFollow;

        //while (_system.IsPathBlocked())
        //{
        //    _system.GetDestination();
        //}
        //yield return new WaitForSeconds(2f);

        //Vector3 position = this._system.transform.position;
        //position.x = Mathf.Lerp(this._system.transform.position.x, _objectToFollow.transform.position.x, interpolation);
        //position.y = Mathf.Lerp(this._system.transform.position.y, _objectToFollow.transform.position.y, interpolation);
        //position.z = Mathf.Lerp(this._system.transform.position.z, _objectToFollow.transform.position.z, interpolation);

        //Quaternion rotation = Quaternion.Euler(0, _system.transform.rotation.y * interpolation, 0);

        //this._system.transform.rotation = rotation;
        //this._system.transform.position = position;

        _system.transform.LookAt(_system.objectToFollow.transform);
        _system.transform.Translate(Vector3.forward * Time.deltaTime * 5f);

        yield break;
    }

    public override IEnumerator Attack()
    {
        GameObject _objectToFollow = _system.objectToFollow;

        if (_system.objectToFollow != null)
        {
            _system.DestroyObject(_objectToFollow);
        }
        _system.SetState(new AIBehaviours(_system));
        yield break;
        
    }

}

using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

struct AIDirection : IComparable
{
    public Vector3 Direction2 { get; }
    public float AIutility;

    public AIDirection(Vector3 direction, float utility)
    {
        Direction2 = direction;
        this.AIutility = utility;
    }

    /// <summary>
    /// Notices that this method is an "inverse" sorting. It makes the higher values on top of the Sort, instead of
    /// the smaller values. For the smaller values, the return line would be utility.CompareTo(otherAgent.utility).
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public int CompareTo(object obj)
    {
        if (obj == null) return 1;

        AIDirection otherAgent = (AIDirection)obj;
        return otherAgent.AIutility.CompareTo(AIutility);
    }
}

[Serializable]
public struct AIData
{
    public float followSpeed;
    public float wanderSpeed;
    public int checkingRadius;
    public float attackRange;
    public float stoppingDistance;
    public float accuracy;

    public AIData(float followSpeed, float wanderSpeed, int checkingRadius, float attackRange, float stoppingDistance, float accuracy)
    {
        this.followSpeed = followSpeed;
        this.wanderSpeed = wanderSpeed;
        this.checkingRadius = checkingRadius;
        this.attackRange = attackRange;
        this.stoppingDistance = stoppingDistance;
        this.accuracy = accuracy;
    }
}

[RequireComponent(typeof(Rigidbody))]
public class AISystem : StateMachine, IComparable<AISystem>
{
    private Rigidbody _rigidbody;
    [SerializeField] public GameObject objectToFollow;
    private bool _isAwake;

    //characteristics
    [SerializeField] public float followSpeed = 0;
    [SerializeField] public float wanderSpeed = 0;
    [SerializeField] public int checkingRadius = 0;
    [SerializeField] public float _attackRange = 3f;
    [SerializeField] public float _stoppingDistance = 1.5f;
    [SerializeField] public float accuracy = 0;
    [SerializeField] public int kills = 0;

    #region Static Variables
    private static float _minimalSpeed = 1.0f;
    //private static float _minimalRayRadius = 1.0f;
    private static int _minimalSight = 2;
    private static float _minimalAttackRange = 2f;
    //private static float _minimalMovingSpeed = 1.0f;
    //private static float _speedInfluenceInSight = 0.1250f;
    private static float _sightInfluenceInSpeed = 0.0625f;
    private static float _minimalAccuracy = 5f;
    //private static float _maxUtilityChoiceChance = 0.85f;
    #endregion

    public Team Team => _team;
    [SerializeField] private Team _team;
    [SerializeField] public LayerMask _layerMask;
    [HideInInspector] public float _rayDistance = 5.0f;
    [HideInInspector] public Vector3 _destination;
    [HideInInspector] public Quaternion _desiredRotation;
    [HideInInspector] public Vector3 _direction;
    [SerializeField] public Vector3 wanderPositions;
    [HideInInspector] public Rigidbody _rigidBody;

    Quaternion startingAngle = Quaternion.AngleAxis(-60, Vector3.up);
    Quaternion stepAngle = Quaternion.AngleAxis(5, Vector3.up);

    /// <summary>
    /// Copies the genes / weights from the parent.
    /// </summary>
    /// <param name="parent"></param>
    public void Birth(AIData parent)
    {
        followSpeed = parent.followSpeed;
        wanderSpeed = parent.wanderSpeed;
        checkingRadius = parent.checkingRadius;
        _attackRange = parent.attackRange;
        _stoppingDistance = parent.stoppingDistance;
    }

    void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();

        if (this.Team == Team.Red)
        {
            objectToFollow = GameObject.FindGameObjectWithTag("AIBlue");
        }
        else if (this.Team == Team.Blue)
        {
            objectToFollow = GameObject.FindGameObjectWithTag("AIRed");
        }

        SetState(new AIBehaviours(this));
    }

    private void Update()
    {
        if (_isAwake)
        {
            var TargetToFollow = CheckEnvironment();

            if (TargetToFollow != null)
            {
                if (objectToFollow != null)
                {
                    StartCoroutine(State.Follow());
                    if (Vector3.Distance(transform.position, objectToFollow.transform.position) < 3f)
                    {
                        StartCoroutine(State.Attack());
                    }
                }
            }
            else
            {
                StartCoroutine(State.Wander());
            }
        }
    }

    /// <summary>
    /// Has a mutationChance ([0%, 100%]) of causing a mutationFactor [-mutationFactor, +mutationFactor] to each gene / weight.
    /// The chance of mutation is calculated per gene / weight.
    /// </summary>
    /// <param name="mutationFactor">How much a gene / weight can change (-mutationFactor, +mutationFactor)</param>
    /// <param name="mutationChance">Chance of a mutation happening per gene / weight.</param>
    public void Mutate(float mutationFactor, float mutationChance)
    {
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            followSpeed += (int)Random.Range(-mutationFactor, +mutationFactor);
            followSpeed = (int)Mathf.Max(followSpeed, _minimalSpeed);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            wanderSpeed += (int)Random.Range(-mutationFactor, +mutationFactor);
            wanderSpeed = (int)Mathf.Max(wanderSpeed, _minimalSpeed);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            _attackRange += (int)Random.Range(-mutationFactor, +mutationFactor);
            _attackRange = (int)Mathf.Max(_attackRange, _minimalAttackRange);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            float sightIncrease = Random.Range(-mutationFactor, +mutationFactor);
            checkingRadius += (int)sightIncrease;
            checkingRadius = Mathf.Max(checkingRadius, _minimalSight);
            if (sightIncrease > 0.0f)
            {
                wanderSpeed -= sightIncrease * _sightInfluenceInSpeed;
                wanderSpeed = Mathf.Max(wanderSpeed, _minimalSpeed);
            }
        }
        if (Random.Range(0.0f, 100.0f) < mutationChance)
        {
            accuracy += (int)Random.Range(-mutationFactor, +mutationFactor);
            accuracy = Mathf.Max(accuracy, _minimalAccuracy);
        }
    }


    /// <summary>
    /// This gets a random destination if there is no random position for the AI to go to.
    /// </summary>
    public void GetDestination()
    {
        Vector3 testPosition = (transform.position + (transform.forward * 4f)) +
                               new Vector3(UnityEngine.Random.Range(-wanderPositions.x, wanderPositions.x), 0f,
                                   UnityEngine.Random.Range(-wanderPositions.z, wanderPositions.z));

        _destination = new Vector3(testPosition.x, 1f, testPosition.z);

        _direction = Vector3.Normalize(_destination - transform.position);
        _direction = new Vector3(_direction.x, 0f, _direction.z);
        _desiredRotation = Quaternion.LookRotation(_direction);
    }

    /// <summary>
    /// This checks if the path is blocked. if this is true, it will get a new destination to go to
    /// </summary>
    /// <returns></returns>
    public bool IsPathBlocked()
    {
        Ray ray = new Ray(transform.position, _direction);
        var hitSomething = Physics.RaycastAll(ray, _rayDistance, _layerMask);
        return hitSomething.Any();
    }


    public bool NeedsDestination()
    {
        if (_destination == Vector3.zero)
            return true;

        var distance = Vector3.Distance(transform.position, _destination);
        if (distance <= _stoppingDistance)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// This just deletes the gameObject that is filled in between the parameters when calling the function.
    /// </summary>
    /// <param name="gameObject"></param>
    public void DestroyObject(GameObject gameObject)
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// This function checks the environment. If it sees the enemy the rays get red and it will follow. 
    /// If the object sees another object, it will turn yellow and get a new position.
    /// If the object sees nothing, it will just return nothing and the rays are white.
    /// </summary>
    /// <returns></returns>
    private Transform CheckEnvironment()
    {
        RaycastHit hit;
        var angle = transform.rotation * startingAngle;
        var direction = angle * Vector3.forward;
        var pos = transform.position;
        for (var i = 0; i < 24; i++)
        {
            if (Physics.Raycast(pos, direction, out hit, checkingRadius))
            {
                var enemy = hit.collider.GetComponent<AISystem>();
                if (enemy != null && enemy.Team != gameObject.GetComponent<AISystem>().Team)
                {
                    Debug.DrawRay(pos, direction * hit.distance, Color.red);
                    objectToFollow = enemy.gameObject;
                    return enemy.transform;
                }
                else
                {
                    Debug.DrawRay(pos, direction * hit.distance, Color.yellow);
                }
            }
            else
            {
                Debug.DrawRay(pos, direction * checkingRadius, Color.white);
            }
            direction = stepAngle * direction;
        }
        return null;
    }

    public void Sleep()
    {
        _isAwake = false;
    }

    /// <summary>
    /// Activates the agent update method.
    /// Does nothing if the agent is already awake.
    /// </summary>
    public void AwakeUp()
    {
        _isAwake = true;
    }

    public int GetKills()
    {
        return kills;
    }

    public AIData GetData()
    {
        return new AIData(followSpeed, wanderSpeed, checkingRadius, _attackRange, _stoppingDistance, accuracy);
    }

    public int CompareTo(AISystem other)
    {
        return other.kills.CompareTo(this.kills);
    }
}

public enum Team
{
    Red,
    Blue,
}

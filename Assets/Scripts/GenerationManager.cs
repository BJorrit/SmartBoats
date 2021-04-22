using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Random = UnityEngine.Random;

public class GenerationManager : MonoBehaviour
{
    [Header("Generators")]
    [SerializeField]
    private GenerateObjectsInArea redAIGenerator;
    [SerializeField]
    private GenerateObjectsInArea blueAIGenerator;

    [Header("File to save info")]
    //General data to save
    [SerializeField] private string _fileName;
    [SerializeField] private string _amountofRedFile;
    [SerializeField] private string _amountofBlueFile;
    [SerializeField] private string _generationCountFile;

    //speed data to save
    [SerializeField] private string _speedRedFile;
    [SerializeField] private string _speedBlueFile;

    //range data to save
    [SerializeField] private string _rangeBlueFile;
    [SerializeField] private string _rangeRedFile;

    //radius data to save
    [SerializeField] private string _radiusBlueFile;
    [SerializeField] private string _radiusRedFile;

    [Space(10)]
    [Header("Parenting and Mutation")]
    [SerializeField]
    private float mutationFactor;
    [SerializeField]
    private float mutationChance;
    [SerializeField]
    private int redArmySize;
    [SerializeField]
    private int blueArmySize;

    [Space(10)]
    [Header("Simulation Controls")]
    [SerializeField] private int _amountOfGenerations;
    [SerializeField, Tooltip("Time per simulation (in seconds).")]
    private float simulationTimer;
    [SerializeField, Tooltip("Current time spent on this simulation.")]
    private float simulationCount;
    [SerializeField, Tooltip("Automatically starts the simulation on Play.")]
    private bool runOnStart;
    [SerializeField, Tooltip("Initial count for the simulation. Used for the Prefabs naming.")]
    private int generationCount;

    /// <summary>
    /// Those variables are used mostly for debugging in the inspector.
    /// </summary>
    [Header("Former winners")]
    [SerializeField]
    private AIData lastBlueWinnerData;
    [SerializeField]
    private AIData lastRedWinnerData;

    private bool _runningSimulation;
    private List<AISystem> _activeRedAI;
    private List<AISystem> _activeBlueAI;
    private AISystem[] _redAIParents;
    private AISystem[] _blueAIParents;

    private void Start()
    {
        const int initialSeed = 666;
        Random.InitState(initialSeed);

        if (runOnStart)
        {
            StartSimulation();
        }
    }

    private void Update()
    {
        if (_runningSimulation)
        {
            //Creates a new generation.
            if (simulationCount >= simulationTimer)
            {
                ++generationCount;
                MakeNewGeneration();
                simulationCount = -Time.deltaTime;
            }
            simulationCount += Time.deltaTime;
        }

        if(generationCount >= _amountOfGenerations)
        {
            StopSimulation();
        }
    }

    /// <summary>
    /// Generates blue and red AI's using the parents list.
    /// If no parents are used, then they are ignored and the boats/pirates are generated using the default prefab
    /// specified in their areas.
    /// </summary>
    /// <param name="blueAI"></param>
    /// <param name="redAI"></param>
    public void GenerateObjects(AISystem[] blueAI = null, AISystem[] redAI = null)
    {
        GenerateRedAI(redAI);
        GenerateBlueAI(blueAI);
    }

    /// <summary>
    /// Generates the list of blue AI's using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created blue AI's will go under mutation (MutationChances and MutationFactor will be applied).
    /// Newly create blue AI's will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="_blueAIParents"></param>
    private void GenerateBlueAI(AISystem[] BlueAIParents)
    {
        _activeBlueAI = new List<AISystem>();
        List<GameObject> objects = blueAIGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            AISystem blueAI = obj.GetComponent<AISystem>();
            if (blueAI != null)
            {
                _activeBlueAI.Add(blueAI);
                if (_blueAIParents != null)
                {
                    AISystem BlueAIParent = BlueAIParents[Random.Range(0, BlueAIParents.Length)];
                    blueAI.Birth(BlueAIParent.GetData());
                }

                blueAI.Mutate(mutationFactor, mutationChance);
                blueAI.AwakeUp();
            }
        }
    }

    /// <summary>
    /// Generates the list of red AI's using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created red AI's will go under mutation (MutationChances and MutationFactor will be applied).
    /// Newly create red AI's will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="RedAIParents"></param>
    private void GenerateRedAI(AISystem[] RedAIParents)
    {
        _activeRedAI = new List<AISystem>();
        List<GameObject> objects = redAIGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            AISystem redAI = obj.GetComponent<AISystem>();
            if (redAI != null)
            {
                _activeRedAI.Add(redAI);
                if (RedAIParents != null)
                {
                    AISystem redAIParent = RedAIParents[Random.Range(0, RedAIParents.Length)];
                    redAI.Birth(redAIParent.GetData());
                }

                redAI.Mutate(mutationFactor, mutationChance);
                redAI.AwakeUp();
            }
        }
    }

    /// <summary>
    /// Creates a new generation by using GenerateBlueAI and GenerateRedAI.
    /// Previous generations will be removed and the best parents will be selected and used to create the new generation.
    /// The best parents (top 1) of the generation will have some data stored in .txt files which can later be used to process in Excel.
    /// </summary>
    public void MakeNewGeneration()
    {
        //Fetch parents
        _activeBlueAI.RemoveAll(item => item == null);
        _activeBlueAI.Sort();
        if (_activeBlueAI.Count == 0)
        {

            GenerateBlueAI(_blueAIParents);
        }
        _blueAIParents = new AISystem[blueArmySize];
        for (int i = 0; i < blueArmySize; i++)
        {
            _blueAIParents[i] = _activeBlueAI[i];
        }

        AISystem lastBlueWinner = _activeBlueAI[0];
        lastBlueWinner.name += "Gen-" + generationCount;
        lastBlueWinnerData = lastBlueWinner.GetData();

        _activeRedAI.RemoveAll(item => item == null);
        _activeRedAI.Sort();
        if (_activeRedAI.Count == 0)
        {

            GenerateRedAI(_redAIParents);
        }
        _redAIParents = new AISystem[redArmySize];
        for (int i = 0; i < redArmySize; i++)
        {
            _redAIParents[i] = _activeRedAI[i];
        }

        AISystem lastRedWinner = _activeRedAI[0];
        lastRedWinner.name += "Gen-" + generationCount;
        lastRedWinnerData = lastRedWinner.GetData();
        Debug.Log("Last winner red had: " + lastRedWinner.GetKills() + " kills!" + " Last winner blue had: " + lastBlueWinner.GetKills() + " kills!");

        float averageSpeedRed = (lastRedWinner.wanderSpeed +lastRedWinner.followSpeed) / 2;
        float averageSpeedBlue = (lastBlueWinner.wanderSpeed + lastBlueWinner.followSpeed) / 2;

        float averageRadiusBlue = (lastBlueWinner.attackRange + lastBlueWinner.checkingRadius) / 2;
        float averageRadiusRed = (lastRedWinner.attackRange + lastRedWinner.checkingRadius) / 2;

        float RadiusBlue = lastBlueWinner.checkingRadius;
        float RadiusRed = lastRedWinner.checkingRadius;

        float RangeBlue = lastBlueWinner.attackRange;
        float RangeRed = lastRedWinner.attackRange;

        if (_activeBlueAI.Count < _activeRedAI.Count)
        {
            //red won
            WriteString(_fileName, "Red");
            WriteString(_generationCountFile, generationCount.ToString());
            WriteString(_amountofBlueFile, _activeBlueAI.Count.ToString());
            WriteString(_amountofRedFile, _activeRedAI.Count.ToString());

            WriteString(_speedRedFile, averageSpeedRed.ToString());
            WriteString(_speedBlueFile, averageSpeedBlue.ToString());

            WriteString(_rangeBlueFile, averageRadiusBlue.ToString());
            WriteString(_rangeRedFile, averageRadiusRed.ToString());
        }
        if (_activeBlueAI.Count > _activeRedAI.Count)
        {
            //blue won
            WriteString(_fileName, "Blue");
            WriteString(_generationCountFile, generationCount.ToString());
            WriteString(_amountofBlueFile, _activeBlueAI.Count.ToString());
            WriteString(_amountofRedFile, _activeRedAI.Count.ToString());

            //WriteString(_radiusBlueFile, RadiusBlue.ToString());
            //WriteString(_radiusRedFile, RadiusRed.ToString());

            //WriteString(_rangeBlueFile, RangeBlue.ToString());
            //WriteString(_rangeRedFile, RangeRed.ToString());

            WriteString(_speedRedFile, averageSpeedRed.ToString());
            WriteString(_speedBlueFile, averageSpeedBlue.ToString());

            WriteString(_rangeBlueFile, averageRadiusBlue.ToString());
            WriteString(_rangeRedFile, averageRadiusRed.ToString());

        }
        if (_activeBlueAI.Count == _activeRedAI.Count)
        {
            //tied
            WriteString(_fileName, "Tie");
            WriteString(_generationCountFile, generationCount.ToString());
            WriteString(_amountofBlueFile, _activeBlueAI.Count.ToString());
            WriteString(_amountofRedFile, _activeRedAI.Count.ToString());

            WriteString(_speedRedFile, averageSpeedRed.ToString());
            WriteString(_speedBlueFile, averageSpeedBlue.ToString());

            WriteString(_rangeBlueFile, averageRadiusBlue.ToString());
            WriteString(_rangeRedFile, averageRadiusRed.ToString());

        }

        GenerateObjects(_blueAIParents, _redAIParents);
    }

    /// <summary>
    /// Starts a new simulation. It does not call MakeNewGeneration. It calls both GenerateBlueAI and GenerateRedAI and
    /// then sets the _runningSimulation flag to true.
    /// </summary>
    public void StartSimulation()
    {
        //GenerateBoxes();
        GenerateObjects();
        _runningSimulation = true;
    }

    /// <summary>
    /// Continues the simulation. It calls MakeNewGeneration to use the previous state of the simulation and continue it.
    /// It sets the _runningSimulation flag to true.
    /// </summary>
    public void ContinueSimulation()
    {
        MakeNewGeneration();
        _runningSimulation = true;
    }

    /// <summary>
    /// Stops the count for the simulation. It also removes null (Destroyed) boats from the _activeBoats list and sets
    /// all blue and red AI to sleep.
    /// </summary>
    public void StopSimulation()
    {
        _runningSimulation = false;
        _activeBlueAI.RemoveAll(item => item == null);
        _activeRedAI.RemoveAll(item => item == null);
        _activeBlueAI.ForEach(blueAI => blueAI.Sleep());
        _activeRedAI.ForEach(redAI => redAI.Sleep());
    }

    /// <summary>
    /// Writes a string to the file. The file is the path in this case. Once this function is called with the message that should be saved,
    /// It writes to the file.
    /// </summary>
    /// <param name="info"></param>
    static void WriteString(string fileName, string info)
    {
        string path = "Assets/Resources/" + fileName;

        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(info);
        writer.Close();
    }
}

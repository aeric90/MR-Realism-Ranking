using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.IO;
using UnityEditor.XR;

public enum EXPERIMENT_STATE
{
    START,
    EXP_START,
    DEMO_START,
    DEMO,
    ROUND_START,
    ROUND,
    ROUND_END,
    EXP_END
}

public class ExperimentController : MonoBehaviour
{
    public static ExperimentController instance;

    private int participantID = 0;

    public GameObject participantUI;
    public GameObject studyUI;
    public GameObject startRoundUI;
    public GameObject endUI;

    private EXPERIMENT_STATE currentState;

    public int maxRounds = 10;
    private int currentRound = 1;

    public int maxDemoMatchups = 3;
    public int maxMatchups = 12;
    private int currentMatchup = 0;

    private StreamWriter writer;

    private float lastTimeStamp = 0.0f;
    private int swapCount = 0;

    public GameObject roomPrefab;
    private GameObject room;

    private bool studyFileOpen = false;

    private bool buttonPress = false;

    private void Awake()
    {
        if(instance == null) instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        OVRManager.display.displayFrequency = 120.0f;
        OVRPlugin.systemDisplayFrequency = 120.0f;

        StartCoroutine(RoomAlignment());

        changeState(EXPERIMENT_STATE.START);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState != EXPERIMENT_STATE.EXP_END)
        {
            if (OVRInput.GetDown(OVRInput.Button.Three) && !buttonPress)
            {
                buttonPress = true;
                participantUI.SetActive(false);
                studyUI.SetActive(false);
                startRoundUI.SetActive(false);
                StartCoroutine(RoomAlignment());
                if (currentState == EXPERIMENT_STATE.START) participantUI.SetActive(true);
                if (currentState == EXPERIMENT_STATE.ROUND_START) startRoundUI.SetActive(true);
                if (currentState == EXPERIMENT_STATE.ROUND) studyUI.SetActive(true);
                StudyUIController.instance.roomAPressed();
            }
        }

        if (OVRInput.GetUp(OVRInput.Button.Three) && buttonPress) buttonPress = false;
    }

    private void OnDestroy()
    {
        if(studyFileOpen) closeStudyFile();
    }

    private void changeState(EXPERIMENT_STATE newState)
    {
        switch(newState)
        {
            case EXPERIMENT_STATE.START:
                participantUI.SetActive(true);
                studyUI.SetActive(false);
                startRoundUI.SetActive(false);
                StartCoroutine(changeExpRoom());
                RoomController.instance.confirgureRoom(15);
                currentState = EXPERIMENT_STATE.START;
                break;
            case EXPERIMENT_STATE.EXP_START:
                openStudyFile();
                EloController.instance.openEloFile();
                //StartCoroutine(changeExpRoom());
                changeState(EXPERIMENT_STATE.ROUND_START);
                break;
            /*
            case EXPERIMENT_STATE.DEMO_START:
                RoomController.instance.setForDemo();
                RoomController.instance.confirgureRoom(15);
                participantUI.SetActive(false);
                startRoundUI.SetActive(true);
                studyUI.SetActive(false);
                currentMatchup = 0;
                swapCount = 0;
                StartUIController.instance.updateRoundText(currentRound);
                currentState = EXPERIMENT_STATE.DEMO_START;
                break;
            case EXPERIMENT_STATE.DEMO:
                participantUI.SetActive(false);
                startRoundUI.SetActive(false);
                studyUI.SetActive(true);
                currentState = EXPERIMENT_STATE.DEMO;
                nextMatchup();
                break;
            */
            case EXPERIMENT_STATE.ROUND_START:
                RoomController.instance.confirgureRoom(15);
                startRoundUI.SetActive(true);
                studyUI.SetActive(false);
                currentMatchup = 0;
                swapCount = 0;
                StartUIController.instance.updateRoundText(currentRound);
                currentState = EXPERIMENT_STATE.ROUND_START;
                break;
            case EXPERIMENT_STATE.ROUND:
                participantUI.SetActive(false);
                startRoundUI.SetActive(false);
                studyUI.SetActive(true);
                currentState = EXPERIMENT_STATE.ROUND;
                nextMatchup();
                break;
            case EXPERIMENT_STATE.EXP_END:
                closeStudyFile();
                EloController.instance.writeEloFile();
                RoomController.instance.confirgureRoom(15);

                studyUI.SetActive(false);
                startRoundUI.SetActive(false);
                endUI.SetActive(true);

                currentState = EXPERIMENT_STATE.EXP_END;
                break;
        }
    }

    public void setParticipantID(int participantID)
    {
        this.participantID = participantID; 
    }

    public int getParticipantID()
    {
        return participantID;
    }

    public void nextMatchup()
    {
        currentMatchup++;

        if (currentState == EXPERIMENT_STATE.DEMO)
        {
            if (currentMatchup <= maxDemoMatchups)
            {
                StartCoroutine(demoMatchupPause());
            }
            else
            {
                nextRound();
            }
        }
        else
        {
            if (currentMatchup <= maxMatchups)
            {
                StartCoroutine(matchupPause());
            }
            else
            {
                nextRound();
            }
        }
    }

    public void nextRound()
    {
        currentRound++;


        if (currentState == EXPERIMENT_STATE.DEMO)
        {
            currentRound = 1;
            changeState(EXPERIMENT_STATE.ROUND_START);
        }
        else
        {

            if (currentRound <= maxRounds)
            {
                changeState(EXPERIMENT_STATE.ROUND_START);
            }
            else
            {
                changeState(EXPERIMENT_STATE.EXP_END);
            }
        }

    }

    private void updateStudyUI()
    {
        if (currentState == EXPERIMENT_STATE.DEMO)
        {
            StudyUIController.instance.updateRoundText(currentRound, currentMatchup, maxDemoMatchups);
        }
        else
        {
            StudyUIController.instance.updateRoundText(currentRound, currentMatchup, maxMatchups);
        }
    }

    IEnumerator changeExpRoom()
    {
        RoomController.instance.setForExperiment();
        yield return new WaitForSeconds(2.0f);
    }

    IEnumerator changeDemoRoom()
    {
        RoomController.instance.setForDemo();
        yield return new WaitForSeconds(2.0f);
    }

    IEnumerator demoMatchupPause()
    {
        MatchupController.instance.newDemoMatchup();
        //furnitureObjects.SetActive(false);
        studyUI.SetActive(false);
        yield return new WaitForSeconds(0.25f);
        //furnitureObjects.SetActive(true);
        studyUI.SetActive(true);
        yield return new WaitForSeconds(0.25f);
        lastTimeStamp = Time.time;
        updateStudyUI();
        StudyUIController.instance.roomAPressed();
    }

    IEnumerator matchupPause()
    {
        MatchupController.instance.newMatchup();
        //furnitureObjects.SetActive(false);
        studyUI.SetActive(false);
        yield return new WaitForSeconds(0.25f);
        //furnitureObjects.SetActive(true);
        studyUI.SetActive(true);
        yield return new WaitForSeconds(0.25f);
        lastTimeStamp = Time.time;
        updateStudyUI();
        StudyUIController.instance.roomAPressed();
    }

    public void voteProcess(char winRoom)
    {
        if (currentState != EXPERIMENT_STATE.DEMO)
        {
            updateStudyFile(winRoom);

            if (winRoom == 'A') EloController.instance.roomAWin();
            if (winRoom == 'B') EloController.instance.roomBWin();
        }
        swapCount = 0;
        nextMatchup();
    }

    private void openStudyFile()
    {
        writer = new StreamWriter(Application.persistentDataPath + "/qr_detail_" + participantID + ".csv");

        writer.WriteLine("PID,Round,Matchup,Room A,Room B,Winner,Swaps,Time");
        studyFileOpen = true;
    }

    private void closeStudyFile()
    {
        writer.Close();
        studyFileOpen = false;
    }

    private void updateStudyFile(char winRoom)
    {
        string output = "";

        output += participantID + ",";
        output += currentRound + ",";
        output += currentMatchup + ",";
        output += MatchupController.instance.getRoomA() + ",";
        output += MatchupController.instance.getRoomB() + ",";

        if (winRoom == 'A') output += MatchupController.instance.getRoomA() + ",";
        if (winRoom == 'B') output += MatchupController.instance.getRoomB() + ",";

        output += swapCount + ",";
        output += Time.time - lastTimeStamp;

        writer.WriteLine(output);
    }

    public void UIStart(string PID)
    {
        if (PID != "")
        {
            participantID = int.Parse(PID);

            if(participantID == 0)
            {
                maxMatchups = 3;
                maxRounds = 3;
            }
            participantUI.SetActive(false);
            changeState(EXPERIMENT_STATE.EXP_START);
        }
    }

    public void UIStartRound()
    {
        if(currentState == EXPERIMENT_STATE.DEMO_START)
        {
            changeState(EXPERIMENT_STATE.DEMO);
        } else
        {
            changeState(EXPERIMENT_STATE.ROUND);
        }
        
    }

    IEnumerator RoomAlignment()
    {
        yield return new WaitForSeconds(2.0f);

        GameObject floor = null;

        OVRSemanticClassification[] anchorLists = FindObjectsOfType(typeof(OVRSemanticClassification)) as OVRSemanticClassification[];

        foreach(OVRSemanticClassification anchor in anchorLists)
        {
            if(anchor.Contains("FLOOR"))
            {
                floor = anchor.gameObject;
                break;
            }
        }

        if(floor != null)
        {
            if(room !=  null) Destroy(room.gameObject);
            room = Instantiate(roomPrefab);
            room.transform.position = floor.transform.position;

            float floorZRoatation = floor.transform.eulerAngles.z + floor.transform.eulerAngles.y + 90.0f;
            room.transform.eulerAngles = new Vector3(0.0f, floorZRoatation, 0.0f);
        }

        yield return new WaitForSeconds(0.5f);
    }

    public void incrementSwaps()
    {
        swapCount++;
    }
}

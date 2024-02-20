using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StudyUIController : MonoBehaviour
{
    public static StudyUIController instance;

    public Button roomAButton;
    public Button roomBButton;

    private bool roomASelected = false;
    private bool roomBSelected = false;

    public TextMeshProUGUI roundText;


    public void Awake()
    {
        instance = this;
    }

    private void Start()
    {

    }

    public void roomAPressed()
    {
        if (roomBSelected) ExperimentController.instance.incrementSwaps();

        roomAButton.GetComponent<Image>().color = Color.yellow;
        roomBButton.GetComponent<Image>().color = Color.white;

        roomASelected = true;
        roomBSelected = false;

        Debug.Log(MatchupController.instance.getRoomA());
            
        RoomController.instance.confirgureRoom(MatchupController.instance.getRoomA());
    }

    public void roomBPressed()
    {
        if (roomASelected) ExperimentController.instance.incrementSwaps();

        roomAButton.GetComponent<Image>().color = Color.white;
        roomBButton.GetComponent<Image>().color = Color.yellow;

        roomASelected = false;
        roomBSelected = true;

        Debug.Log(MatchupController.instance.getRoomB());
            
        RoomController.instance.confirgureRoom(MatchupController.instance.getRoomB());
    }

    public void votePressed()
    {
        if (roomASelected) ExperimentController.instance.voteProcess('A'); 
        if (roomBSelected) ExperimentController.instance.voteProcess('B'); 
    }

    public void updateRoundText(int currentRound, int currentMatchup, int maxMatchup)
    {
        roundText.text = "Round " + currentRound + "<br>Match-Up " + currentMatchup + "/" + maxMatchup;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{

    public GameObject playerPrefab; //get player prefab object - PUBLIC
	private GameObject player; // private

	public Text continueText; // write to continue text
	public Text levelText; // write to level text
	private int currentLevel; // keep track of current level

	private float timeElapsed = 0f; // keeping track of active game time 
	private float blinkTime = 0f; // continue text, color blinking
	private bool blink; // bool for switching blink
	private bool gameStarted; // bool to check if game started
	private TimeManager timeManager; // get TimeManager object
	private Sokoban sokoban;

	void Awake()
	{
		timeManager = GetComponent<TimeManager>();
		sokoban = GetComponent<Sokoban>();
	}

	// Start is called before the first frame update
	void Start()
	{
		Time.timeScale = 0;

		continueText.text = "PRESS ANY KEY TO START!";
	}

    // Update is called once per frame
    void Update()
    {
		if (!gameStarted && Time.timeScale == 0)
		{
			if (Input.anyKeyDown)
			{
				timeManager.ManipulateTime(1, 1f);
				ResetGame();
			}
		}

		if (!gameStarted)
		{
			blinkTime++;

			if (blinkTime % 40 == 0)
			{
				blink = !blink;
			}

			continueText.canvasRenderer.SetAlpha(blink ? 0 : 1);

			//var textColor = beatBestTime ? "#FF0" : "#FFF";

			levelText.text = "LEVEL " + sokoban.levelName;
		}
		else
		{
			timeElapsed += Time.deltaTime;
			//levelText.text = "LEVEL " + FormatTime(timeElapsed);
		}
	}

	// Reset game to title screen
	void ResetGame()
	{
		gameStarted = true;

		continueText.canvasRenderer.SetAlpha(0);
		timeElapsed = 0;
		currentLevel = 0;
	}

	string FormatTime(float value)
	{
		TimeSpan t = TimeSpan.FromSeconds(value);

		return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
	}

	void PauseGame()
    {

    }
}

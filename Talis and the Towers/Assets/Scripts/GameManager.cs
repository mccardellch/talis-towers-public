using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
	public Text levelText; // write to level text
	private Sokoban sokoban;

	void Awake()
	{
		sokoban = GetComponent<Sokoban>();
	}

    // Update is called once per frame
    void Update()
    {
		levelText.text = "LEVEL: " + sokoban.levelName;
	}
}

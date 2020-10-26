using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    private GameObject pauseMenuUI;
    private bool isPaused;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("t"))
        {
            isPaused = !isPaused;
            PauseGame(isPaused);
        }

        if(Input.GetKeyDown("escape") && isPaused)
        {
            isPaused = !isPaused;
            PauseGame(isPaused);
        }
    } 

    void PauseGame(bool isPaused)
    {
        if(isPaused)
        {
            Time.timeScale = 0;

            AudioListener.pause = true;
            pauseMenuUI.SetActive(true);

        } 
        else
        {
            Time.timeScale = 1;
            AudioListener.pause = false;
            pauseMenuUI.SetActive(false);
        }
    }
}

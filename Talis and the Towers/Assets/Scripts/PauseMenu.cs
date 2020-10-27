using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private bool isPaused;
    [SerializeField] private bool isMuted;

    void Start()
    {
        pauseMenuUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("escape"))
        {
            isPaused = !isPaused;
        }

        if (isPaused)
        {
            ActivateMenu();
        } else {
            DeactivateMenu();
        }
    } 

    public void ActivateMenu()
    {
        Time.timeScale = 0;
        pauseMenuUI.SetActive(true);
    }

    public void DeactivateMenu()
    {
        Time.timeScale = 1;
        pauseMenuUI.SetActive(false);
        isPaused = false;
    }


    public void ToggleMusic(bool _isMuted)
    {     
        AudioListener.pause = _isMuted;
    }

     
}

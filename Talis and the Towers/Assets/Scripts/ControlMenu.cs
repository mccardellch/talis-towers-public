using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlMenu : MonoBehaviour
{
    [SerializeField] private GameObject controlMenuUI;
    [SerializeField] private GameObject pauseMenuUI;
    
    void Start()
    {
        controlMenuUI.SetActive(false);
        pauseMenuUI.SetActive(false);
    }

    public void ActivateMenu()
    {
        controlMenuUI.SetActive(true);
        pauseMenuUI.SetActive(false);
        Debug.Log("pauseMenuUI active: " + pauseMenuUI.active);
    }

    public void DeactivateMenu()
    {
        controlMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }
}

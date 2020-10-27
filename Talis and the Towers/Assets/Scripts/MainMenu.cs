using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject controlMenuUI;
    [SerializeField] private GameObject MainMenuUI;
    [SerializeField] private GameObject TitleArt;
    [SerializeField] private GameObject CreditsUI;

    void Start()
    {
        MainMenuUI.SetActive(true);
        TitleArt.SetActive(true);
        controlMenuUI.SetActive(false);
        CreditsUI.SetActive(false);
    }

    public void PlayGame()
    {
        // make sure build setting list TitleScreen in first (index 0) position and SampleScene (game scene) in the second (1) position
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Debug.Log("Player quit game");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    public void ActivateMenu()
    {
        controlMenuUI.SetActive(true);
        MainMenuUI.SetActive(false);
        TitleArt.SetActive(false);
        CreditsUI.SetActive(false);
    }

    public void DeactivateMenu()
    {
        controlMenuUI.SetActive(false);
        MainMenuUI.SetActive(true);
        TitleArt.SetActive(true);
    }

    public void ActivateCredits()
    {
        CreditsUI.SetActive(true);
        MainMenuUI.SetActive(false);
        TitleArt.SetActive(false);
    }
    
    public void DeactivateCredits()
    {
        CreditsUI.SetActive(false);
        MainMenuUI.SetActive(true);
        TitleArt.SetActive(true);
    }

}

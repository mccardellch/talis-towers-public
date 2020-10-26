using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
   public void PlayGame()
    {
        // make sure build setting list TitleScreen in first (index 0) position and SampleScene (game scene) in the second (1) position
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Debug.Log("Player quit game");
        Application.Quit();
    }
}

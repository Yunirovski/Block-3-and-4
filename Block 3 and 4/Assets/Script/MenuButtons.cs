using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MenuButtons
{
    void SwitchScene(string SceneName){

        SceneManager.LoadScene(SceneName);
    }

    void SwitchMenu(string NextMenu){


    }

    void Quit()
    {
        Application.Quit();
    }
}

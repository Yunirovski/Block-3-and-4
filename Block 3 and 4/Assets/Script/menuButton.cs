using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class menuButton : MonoBehaviour
{
    
    public void SwitchScene(string SceneName){

        SceneManager.LoadScene(SceneName);
    }

    void Quit(){
        Application.Quit();
    }
}

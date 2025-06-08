using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;


public class Dialog : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float txtSpeed;

 

    private int index = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textComponent.text = string.Empty;
        StartDialog();
        
    }

    // Update is called once per frame
    void Update()
    {
        // skip text or show next line
        if(Input.GetKeyDown(KeyCode.KeypadEnter)  || Input.GetKeyDown(KeyCode.T)){
            Debug.Log("key pressed");
            if (textComponent.text == lines[index]){
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                textComponent.text = lines[index];
            }
        }
    }

    void StartDialog(){

        index = 0;
        StartCoroutine(TypeLine());

    }

    // write lines by character
    IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(txtSpeed);
        }
    }

    // go to next line
    void NextLine(){

        Debug.Log("NextLine");

        if (index < lines.Length - 1){

            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else{
            gameObject.SetActive(false);
        }
    }

    

}

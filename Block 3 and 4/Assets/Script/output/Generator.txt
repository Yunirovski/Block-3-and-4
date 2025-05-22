using System.Collections;
using UnityEngine;

public class Generator : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //spawn something every n-seconds
        //the object that is spawned has its own beahavior
        StartCoroutine(SpawnSomething());
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    //spawn something
    //wait for n seconds
    //repeat
    IEnumerator SpawnSomething()
    {
        while (true)
        {
            Instantiate(prefab);
            yield return new WaitForSeconds(0.5f);
        }
    }
}

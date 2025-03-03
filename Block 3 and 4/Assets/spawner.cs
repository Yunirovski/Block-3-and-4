using UnityEngine;

public class spawner : MonoBehaviour
{
    [SerializeField] private GameObject obstacle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            Instantiate(obstacle, Random.insideUnitCircle * 10, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

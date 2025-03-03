using UnityEngine;

public class spawner : MonoBehaviour
{
    [SerializeField] private GameObject obstacle;
    [SerializeField] private int amount = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < amount; i++)
        {
            Vector2 rnd = Random.insideUnitCircle;
            Vector3 pos = new Vector3(rnd.x, 0, rnd.y);
            Instantiate(obstacle, pos * 10, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

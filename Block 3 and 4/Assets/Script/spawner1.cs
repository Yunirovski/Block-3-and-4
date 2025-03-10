using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class spawner1 : MonoBehaviour
{
    [SerializeField] private GameObject obstacle;
    [SerializeField] private List<GameObject> enemies;
    [SerializeField] private int amount = 100;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < amount; i++)
        {
            Vector2 rnd = Random.insideUnitCircle;
            Vector3 pos = new Vector3(rnd.x, rnd.y);
            GameObject go = Instantiate(obstacle, pos * 10, Quaternion.identity);

            enemies.Add(go);
        }

        foreach (var enemy in enemies)
        {
            Vector3 pos = enemy.transform.position;
            pos.y = Random.Range(0, 10);
            enemy.transform.position = pos;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}

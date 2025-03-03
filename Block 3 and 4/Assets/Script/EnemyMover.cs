using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    private Vector3 randomPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomPosition = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            randomPosition,
            2 * Time.deltaTime);

        //if we are very near the destination, destory this
        if (Vector3.Distance(transform.position, randomPosition) <= .1f)
        {
            Destroy(gameObject);
        }

    }
}

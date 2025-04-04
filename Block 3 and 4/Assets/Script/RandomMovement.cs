using UnityEngine;

public class RandomMovement : MonoBehaviour
{
    public float moveSpeed = 2f;   // Movement speed
    public float timeBetweenMoves = 5f;  // Time between random moves

    private Vector3 targetPosition;  // Target position
    private float timeSinceLastMove; // Time since last move

    void Start()
    {
        // Set the first random target position
        SetRandomTargetPosition();
    }

    void Update()
    {
        timeSinceLastMove += Time.deltaTime;

        // If enough time has passed, set a new target position
        if (timeSinceLastMove >= timeBetweenMoves)
        {
            SetRandomTargetPosition();
            timeSinceLastMove = 0f;
        }

        // Move towards the target position
        MoveTowardsTarget();
    }

    // Set a new random target position
    void SetRandomTargetPosition()
    {
        targetPosition = new Vector3(
            Random.Range(-10f, 10f),  // Random X position
            transform.position.y,     // Keep Y position the same
            Random.Range(-10f, 10f)   // Random Z position
        );
    }

    // Move towards the target position
    void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
}

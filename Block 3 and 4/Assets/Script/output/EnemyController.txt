using UnityEngine;

namespace Unity.FPS.AI
{
    /// <summary>
    /// Controls enemy patrol behavior along a sequence of waypoints.
    /// The enemy rotates toward each node, moves there at a constant speed,
    /// and pauses for a set duration before proceeding to the next node.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        /// <summary>
        /// The patrol path containing an ordered list of waypoints.
        /// </summary>
        [Tooltip("Patrol path consisting of sequential waypoints.")]
        public PatrolPath PatrolPath;

        /// <summary>
        /// Speed at which the enemy rotates to face the next waypoint (degrees per second).
        /// </summary>
        [Tooltip("Rotation speed when turning towards the next node.")]
        public float RotationSpeed = 2f;

        /// <summary>
        /// Speed at which the enemy moves along the path (units per second).
        /// </summary>
        [Tooltip("Movement speed along the patrol path.")]
        public float MoveSpeed = 2f;

        /// <summary>
        /// Time in seconds the enemy pauses at each waypoint before moving on.
        /// </summary>
        [Tooltip("Delay (seconds) at each waypoint before continuing.")]
        public float WaitTimeAtEachNode = 2f;

        // Index of the current target waypoint in the path
        private int m_CurrentNodeIndex = 0;
        // Timer accumulating how long we've been waiting at the current node
        private float m_WaitTimer = 0f;
        // Whether the enemy is currently paused at a waypoint
        private bool m_IsWaiting = false;

        private void Update()
        {
            // If no valid path or no nodes, do nothing
            if (PatrolPath == null || PatrolPath.PathNodes.Count == 0)
                return;

            // If we're in the waiting state, update the wait timer
            if (m_IsWaiting)
            {
                m_WaitTimer += Time.deltaTime;
                if (m_WaitTimer >= WaitTimeAtEachNode)
                {
                    // Done waiting: reset timer, exit waiting, advance to next node
                    m_WaitTimer = 0f;
                    m_IsWaiting = false;
                    m_CurrentNodeIndex = (m_CurrentNodeIndex + 1) % PatrolPath.PathNodes.Count;
                }
                return;  // skip movement while waiting
            }

            // Get the world position of the current target waypoint
            Vector3 targetPosition = PatrolPath.GetPositionOfPathNode(m_CurrentNodeIndex);

            // Compute the horizontal direction vector toward the waypoint
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;  // ignore vertical component for rotation

            // Rotate smoothly to face the target direction, if there is one
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    RotationSpeed * Time.deltaTime);
            }

            // Move toward the target waypoint at the specified speed
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                MoveSpeed * Time.deltaTime);

            // Check if we've arrived (within a small threshold) to start waiting
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                m_IsWaiting = true;
            }
        }
    }
}

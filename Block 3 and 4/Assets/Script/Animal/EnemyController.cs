using UnityEngine;

namespace Unity.FPS.AI
{
    public class EnemyController : MonoBehaviour
    {
        [Tooltip("敌人的巡逻路径")]
        public PatrolPath PatrolPath;

        [Tooltip("敌人旋转朝向下一个节点的速度")]
        public float RotationSpeed = 2f;

        [Tooltip("敌人移动的速度")]
        public float MoveSpeed = 2f;

        [Tooltip("在每个点等待的时间（秒）")]
        public float WaitTimeAtEachNode = 2f;

        private int m_CurrentNodeIndex = 0;
        private float m_WaitTimer = 0f;
        private bool m_IsWaiting = false;

        void Update()
        {
            if (PatrolPath == null || PatrolPath.PathNodes.Count == 0)
                return;

            if (m_IsWaiting)
            {
                m_WaitTimer += Time.deltaTime;
                if (m_WaitTimer >= WaitTimeAtEachNode)
                {
                    m_WaitTimer = 0f;
                    m_IsWaiting = false;
                    m_CurrentNodeIndex = (m_CurrentNodeIndex + 1) % PatrolPath.PathNodes.Count;
                }
                return;
            }

            Vector3 targetPosition = PatrolPath.GetPositionOfPathNode(m_CurrentNodeIndex);

            // 计算方向（只水平）
            Vector3 direction = (targetPosition - transform.position);
            direction.y = 0f;

            // 旋转朝向目标
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
            }

            // 移动到目标
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, MoveSpeed * Time.deltaTime);

            // 到达判断
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                m_IsWaiting = true;
            }
        }
    }
}

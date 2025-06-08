// Assets/Scripts/Items/MagicWandItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/MagicWandItem")]
public class MagicWandItem : BaseItem
{
    [Header("Magic Wand Settings")]
    [Tooltip("吸引半径")]
    public float radius = 15f;

    [Tooltip("冷却时间")]
    public float cooldown = 3f;

    [Tooltip("吸引持续时间")]
    public float attractDuration = 8f;

    [Header("Audio")]
    [Tooltip("魔法棒使用音效")]
    public AudioClip useSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    // 运行时状态
    private Camera playerCamera;
    private AudioSource audioSource;
    private float nextUseTime = 0f;

    public override void OnSelect(GameObject model)
    {
        playerCamera = Camera.main;

        // 设置音频源
        if (playerCamera != null)
        {
            audioSource = playerCamera.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = playerCamera.gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
            }
        }

        Debug.Log("魔法棒已选中");
    }

    public override void OnReady()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText("左键使用魔法棒吸引动物");
        }
    }

    public override void OnUse()
    {
        UseMagicWand();
    }

    public override void HandleUpdate()
    {
        // 更新冷却状态
        if (Time.time < nextUseTime)
        {
            float cooldownRemaining = nextUseTime - Time.time;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"魔法棒冷却中: {cooldownRemaining:F1}s");
            }
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"魔法棒就绪 - 左键吸引动物 (范围: {radius}m)");
            }
        }
    }

    /// <summary>
    /// 使用魔法棒
    /// </summary>
    private void UseMagicWand()
    {
        if (playerCamera == null)
        {
            Debug.LogError("MagicWand: 找不到玩家相机");
            return;
        }

        // 检查冷却
        if (Time.time < nextUseTime)
        {
            float cooldownRemaining = nextUseTime - Time.time;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCameraDebugText($"冷却中: {cooldownRemaining:F1}s");
            }
            return;
        }

        Vector3 playerPosition = playerCamera.transform.position;

        // 找到范围内的所有动物
        AnimalBehavior[] allAnimals = Object.FindObjectsOfType<AnimalBehavior>();
        int attractedCount = 0;

        foreach (AnimalBehavior animal in allAnimals)
        {
            if (animal == null) continue;

            float distance = Vector3.Distance(playerPosition, animal.transform.position);
            if (distance <= radius)
            {
                // 吸引动物到玩家位置
                animal.Attract(playerCamera.transform, attractDuration);
                attractedCount++;

                Debug.Log($"魔法棒吸引了动物: {animal.name} (距离: {distance:F1}m)");
            }
        }

        // 设置冷却
        nextUseTime = Time.time + cooldown;

        // 播放音效
        PlayUseSound();

        // 显示结果
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCameraDebugText($"吸引了 {attractedCount} 只动物! 冷却 {cooldown}s");
        }

        Debug.Log($"魔法棒使用: 吸引了 {attractedCount} 只动物，半径 {radius}m");
    }

    /// <summary>
    /// 播放使用音效
    /// </summary>
    private void PlayUseSound()
    {
        if (useSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(useSound, soundVolume);
        }
    }

    // 调试可视化
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        if (playerCamera != null)
        {
            // 显示吸引范围
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(playerCamera.transform.position, radius);

            // 显示范围内的动物
            AnimalBehavior[] allAnimals = Object.FindObjectsOfType<AnimalBehavior>();
            foreach (AnimalBehavior animal in allAnimals)
            {
                if (animal == null) continue;

                float distance = Vector3.Distance(playerCamera.transform.position, animal.transform.position);
                if (distance <= radius)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(playerCamera.transform.position, animal.transform.position);
                    Gizmos.DrawWireSphere(animal.transform.position, 1f);
                }
            }
        }
    }
}
using UnityEngine;

/// <summary>
/// 滑板物品：
/// • 选为当前物品后，监听 Q 键切换“上/下板”
/// • 上板时调用 PlayerMove.ModifySpeed(2f)，并 2 s 内平滑达到最高速
/// </summary>
[CreateAssetMenu(menuName = "Items/SkateboardItem")]
public class SkateboardItem : BaseItem
{
    [Header("Settings")]
    public float speedMultiplier = 2f;
    public float accelTime = 2f;

    /* 运行态 */
    float targetMultiplier = 1f;
    float currentMultiplier = 1f;
    player_move2 mover;

    public override void OnSelect(GameObject model)
    {
        mover = Object.FindObjectOfType<player_move2>();
        currentMultiplier = 1f;
        targetMultiplier = 1f;
    }

    public override void OnDeselect()
    {
        mover?.ModifySpeed(1f);
    }

    public override void OnReady() { }   // 不使用 Ready/Use
    public override void OnUnready() { }

    public override void OnUse() { }      // Left-click 无操作

    void Update()
    {
        if (mover == null) return;

        if (Input.GetKeyDown(KeyCode.Q))
            targetMultiplier = Mathf.Approximately(targetMultiplier, 1f) ? speedMultiplier : 1f;

        currentMultiplier = Mathf.MoveTowards(currentMultiplier, targetMultiplier,
                        Time.deltaTime * (speedMultiplier - 1f) / accelTime);

        mover.ModifySpeed(currentMultiplier);
    }
}

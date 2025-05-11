// Assets/Scripts/Items/SkateboardItem.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/SkateboardItem")]
public class SkateboardItem : BaseItem
{
    [Header("滑板设置")]
    [Tooltip("滑行时玩家移动速度倍率")]
    public float speedMultiplier = 2f;
    [Tooltip("加速 / 减速 用时 (s)")]
    public float accelTime = 2f;
    [Tooltip("当输入停止时继续滑行的距离 (m)")]
    public float coastDistance = 3f;

    [Header("模型偏移 (挂载在 FootAnchor 下)")]
    public Vector3 holdPosition = Vector3.zero;
    public Vector3 holdRotation = Vector3.zero;
    public Vector3 modelScale = Vector3.one;

    // —— 内部状态 —— 
    float _currentMultiplier;
    float _targetMultiplier;
    float _coastRemaining;
    Vector3 _lastMoveDir;
    bool _isSkating;
    player_move2 _mover;
    CharacterController _cc;
    GameObject _model;

    public override void OnSelect(GameObject model)
    {
        _model = model;
        // 应用挂载偏移
        model.transform.localPosition = holdPosition;
        model.transform.localEulerAngles = holdRotation;
        model.transform.localScale = modelScale;

        // 查找移动控制
        _mover = Object.FindObjectOfType<player_move2>();
        if (_mover == null)
            Debug.LogError("SkateboardItem: 找不到 player_move2");
        else
            _cc = _mover.GetComponent<CharacterController>();

        // 初始化状态
        _currentMultiplier = 1f;
        _targetMultiplier = 1f;
        _coastRemaining = 0f;
        _isSkating = false;
    }

    public override void OnDeselect()
    {
        // 卸下滑板，立即恢复正常速度
        if (_mover != null) _mover.ModifySpeed(1f);
        _isSkating = false;
        _coastRemaining = 0f;
    }

    public override void HandleUpdate()
    {
        if (_mover == null || _cc == null) return;

        // Q 键切换滑板状态
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _isSkating = !_isSkating;
            if (_isSkating)
            {
                // 开始滑行
                _targetMultiplier = speedMultiplier;
                _coastRemaining = coastDistance;
            }
            else
            {
                // 立即停下
                _currentMultiplier = 1f;
                _targetMultiplier = 1f;
                _coastRemaining = 0f;
                _mover.ModifySpeed(1f);
                return;
            }
        }

        if (!_isSkating) return;

        // 读取输入方向
        Vector2 raw = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        Vector3 inputDir = Vector3.zero;
        if (raw.sqrMagnitude > 0.01f)
        {
            inputDir = (
                _mover.transform.right * raw.x +
                _mover.transform.forward * raw.y
            ).normalized;
        }

        // 平滑加速 / 减速
        _currentMultiplier = Mathf.MoveTowards(
            _currentMultiplier,
            _targetMultiplier,
            Time.deltaTime * (speedMultiplier - 1f) / accelTime
        );
        _mover.ModifySpeed(_currentMultiplier);

        if (inputDir.sqrMagnitude > 0.01f)
        {
            // 有输入：正常滑行
            _lastMoveDir = inputDir;
            _coastRemaining = coastDistance;
        }
        else
        {
            // 无输入：惯性滑行
            if (_coastRemaining > 0f)
            {
                float step = speedMultiplier * Time.deltaTime;
                _cc.Move(_lastMoveDir * step);
                _coastRemaining = Mathf.Max(0f, _coastRemaining - step);
            }
            else
            {
                // 惯性结束 → 停下
                _isSkating = false;
                _currentMultiplier = 1f;
                _targetMultiplier = 1f;
                _mover.ModifySpeed(1f);
            }
        }
    }
}

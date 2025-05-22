// Assets/Scripts/World/FoodSupplyCrate.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class FoodSupplyCrate : MonoBehaviour
{
    [Tooltip("被使用后刷新所需时间（秒），0 = 一次性箱子")]
    public float respawnTime = 120f;

    Collider _col;
    MeshRenderer[] _renderers;

    bool playerNearby;          // 玩家是否在范围内
    const KeyCode USE_KEY = KeyCode.F;

    void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
        _renderers = GetComponentsInChildren<MeshRenderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNearby = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNearby = false;
    }

    void Update()
    {
        if (playerNearby && Input.GetKeyDown(USE_KEY))
            ActivateCrate();
    }

    /* ────────── 补给逻辑 ────────── */
    void ActivateCrate()
    {
        var con = ConsumableManager.Instance;
        if (con == null) return;

        con.RefillFilm();
        con.RefillFood();

        UIManager.Instance?.ShowPopup("已补满胶卷与食物！");

        if (respawnTime <= 0f)
            Destroy(gameObject);
        else
            StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        SetCrateVisible(false);
        yield return new WaitForSeconds(respawnTime);
        SetCrateVisible(true);
    }

    void SetCrateVisible(bool show)
    {
        _col.enabled = show;
        foreach (var r in _renderers) r.enabled = show;
    }
}

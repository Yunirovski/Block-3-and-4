// Assets/Scripts/Player/TerrainFootstepSystem.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 地形材质脚步声系统
/// 检测玩家脚下的地形材质并播放对应的脚步声
/// </summary>
[System.Serializable]
public class TerrainSoundSet
{
    [Header("材质信息")]
    public string materialName;

    [Header("行走声音")]
    public AudioClip[] walkSounds;

    [Header("跑步声音")]
    public AudioClip[] runSounds;

    [Header("蹲伏声音")]
    public AudioClip[] crouchSounds;

    [Header("跳跃落地声音")]
    public AudioClip[] landSounds;

    [Header("音频设置")]
    [Range(0f, 1f)]
    public float volumeMultiplier = 1f;

    [Range(0.1f, 1f)]
    public float pitchVariation = 0.2f; // 音调变化范围

    [Header("播放频率")]
    [Tooltip("脚步声播放间隔（秒）")]
    public float stepInterval = 0.5f;
}

public class TerrainFootstepSystem : MonoBehaviour
{
    [Header("地形材质声音配置")]
    [Tooltip("为每种地形材质配置不同的脚步声")]
    public TerrainSoundSet[] terrainSounds;

    [Header("默认声音（当检测不到地形时使用）")]
    public TerrainSoundSet defaultSounds;

    [Header("检测设置")]
    [Tooltip("脚步声检测的射线长度")]
    public float raycastDistance = 2f;

    [Tooltip("射线起始点偏移（相对于角色位置）")]
    public Vector3 raycastOffset = new Vector3(0, 0.1f, 0);

    [Header("音频播放设置")]
    [Tooltip("脚步声音频源")]
    public AudioSource footstepAudioSource;

    [Tooltip("效果音频源（跳跃、落地等）")]
    public AudioSource effectAudioSource;

    [Range(0f, 1f)]
    public float masterVolume = 0.7f;

    [Header("地形材质映射")]
    [Tooltip("TerrainGrass对应的材质名称")]
    public string grassMaterialName = "TerrainGrass";

    [Tooltip("TerrainDirt对应的材质名称")]
    public string dirtMaterialName = "TerrainDirt";

    [Tooltip("Black_Sand_TerrainLayer对应的材质名称")]
    public string sandMaterialName = "Black_Sand_TerrainLayer";

    [Tooltip("Rock_TerrainLayer对应的材质名称")]
    public string rockMaterialName = "Rock_TerrainLayer";

    [Tooltip("Snow_TerrainLayer对应的材质名称")]
    public string snowMaterialName = "Snow_TerrainLayer";

    [Header("调试设置")]
    public bool showDebugRay = true;
    public bool showDebugInfo = false;

    // 内部变量
    private Terrain currentTerrain;
    private string currentMaterialName = "";
    private TerrainSoundSet currentSoundSet;

    // 脚步声播放状态
    private float lastStepTime = 0f;
    private bool isMoving = false;
    private Dictionary<string, TerrainSoundSet> materialLookup;

    void Start()
    {
        // 设置音频源
        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
            if (footstepAudioSource == null)
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (effectAudioSource == null)
        {
            effectAudioSource = gameObject.AddComponent<AudioSource>();
            effectAudioSource.playOnAwake = false;
        }

        // 配置音频源
        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.loop = false;

        // 创建材质查找字典
        CreateMaterialLookup();

        // 设置默认音效组
        currentSoundSet = defaultSounds;

        Debug.Log("地形脚步声系统初始化完成");
    }

    void Update()
    {
        DetectTerrainMaterial();
    }

    /// <summary>
    /// 创建材质名称到音效组的映射
    /// </summary>
    void CreateMaterialLookup()
    {
        materialLookup = new Dictionary<string, TerrainSoundSet>();

        foreach (var soundSet in terrainSounds)
        {
            if (!string.IsNullOrEmpty(soundSet.materialName))
            {
                materialLookup[soundSet.materialName] = soundSet;
            }
        }

        Debug.Log($"创建了 {materialLookup.Count} 个材质音效映射");
    }

    /// <summary>
    /// 检测玩家脚下的地形材质
    /// </summary>
    void DetectTerrainMaterial()
    {
        Vector3 rayStart = transform.position + raycastOffset;
        Vector3 rayDirection = Vector3.down;

        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, raycastDistance))
        {
            if (showDebugRay)
            {
                Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green, 0.1f);
            }

            // 检测地形
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                currentTerrain = terrain;
                string materialName = GetTerrainMaterialAtPosition(terrain, hit.point);
                UpdateCurrentSoundSet(materialName);
            }
            else
            {
                // 检测其他物体的材质
                string materialName = GetMaterialFromCollider(hit.collider);
                UpdateCurrentSoundSet(materialName);
            }

            if (showDebugInfo)
            {
                Debug.Log($"当前材质: {currentMaterialName} | 位置: {hit.point}");
            }
        }
        else
        {
            if (showDebugRay)
            {
                Debug.DrawRay(rayStart, rayDirection * raycastDistance, Color.red, 0.1f);
            }

            UpdateCurrentSoundSet("default");
        }
    }

    /// <summary>
    /// 获取地形在指定位置的主要材质
    /// </summary>
    string GetTerrainMaterialAtPosition(Terrain terrain, Vector3 worldPos)
    {
        if (terrain == null || terrain.terrainData == null)
            return "default";

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        // 将世界坐标转换为地形坐标（0-1范围）
        int mapX = Mathf.RoundToInt(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = Mathf.RoundToInt(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

        // 确保坐标在有效范围内
        mapX = Mathf.Clamp(mapX, 0, terrainData.alphamapWidth - 1);
        mapZ = Mathf.Clamp(mapZ, 0, terrainData.alphamapHeight - 1);

        // 获取该位置的材质权重
        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        // 找到权重最大的材质
        float maxWeight = 0f;
        int maxIndex = 0;

        for (int i = 0; i < splatmapData.GetLength(2); i++)
        {
            if (splatmapData[0, 0, i] > maxWeight)
            {
                maxWeight = splatmapData[0, 0, i];
                maxIndex = i;
            }
        }

        // 根据材质索引返回对应的材质名称
        if (maxIndex < terrainData.terrainLayers.Length)
        {
            TerrainLayer terrainLayer = terrainData.terrainLayers[maxIndex];
            if (terrainLayer != null)
            {
                string layerName = terrainLayer.name;

                // 映射到你的材质名称
                if (layerName.Contains("TerrainGrass")) return grassMaterialName;
                if (layerName.Contains("TerrainDirt")) return dirtMaterialName;
                if (layerName.Contains("Black_Sand")) return sandMaterialName;
                if (layerName.Contains("Rock")) return rockMaterialName;
                if (layerName.Contains("Snow")) return snowMaterialName;

                return layerName; // 返回原始名称
            }
        }

        return "default";
    }

    /// <summary>
    /// 从碰撞体获取材质信息（非地形物体）
    /// </summary>
    string GetMaterialFromCollider(Collider collider)
    {
        // 可以通过标签、组件或其他方式确定材质
        if (collider.CompareTag("Wood")) return "Wood";
        if (collider.CompareTag("Metal")) return "Metal";
        if (collider.CompareTag("Stone")) return "Stone";
        if (collider.CompareTag("Water")) return "Water";

        // 检查是否有MaterialType组件
        MaterialType materialType = collider.GetComponent<MaterialType>();
        if (materialType != null)
        {
            return materialType.materialName;
        }

        return "default";
    }

    /// <summary>
    /// 更新当前音效组
    /// </summary>
    void UpdateCurrentSoundSet(string materialName)
    {
        if (currentMaterialName == materialName) return;

        currentMaterialName = materialName;

        if (materialLookup.ContainsKey(materialName))
        {
            currentSoundSet = materialLookup[materialName];
        }
        else
        {
            currentSoundSet = defaultSounds;
        }

        if (showDebugInfo)
        {
            Debug.Log($"切换到材质: {materialName}, 音效组: {currentSoundSet.materialName}");
        }
    }

    /// <summary>
    /// 由player_move2调用，更新脚步声状态
    /// </summary>
    public void UpdateFootstepState(bool isWalking, bool isRunning, bool isCrouching, float speed)
    {
        bool shouldPlayFootsteps = isWalking || isRunning || (isCrouching && speed > 0.1f);

        if (shouldPlayFootsteps)
        {
            float currentTime = Time.time;
            float stepInterval = GetStepInterval(isRunning, isCrouching);

            if (currentTime - lastStepTime >= stepInterval)
            {
                PlayFootstepSound(isRunning, isCrouching);
                lastStepTime = currentTime;
            }
        }
    }

    /// <summary>
    /// 根据移动状态获取脚步声间隔
    /// </summary>
    float GetStepInterval(bool isRunning, bool isCrouching)
    {
        float baseInterval = currentSoundSet.stepInterval;

        if (isRunning)
            return baseInterval * 0.6f; // 跑步时脚步更快
        else if (isCrouching)
            return baseInterval * 1.5f; // 蹲伏时脚步更慢
        else
            return baseInterval; // 正常行走
    }

    /// <summary>
    /// 播放脚步声
    /// </summary>
    void PlayFootstepSound(bool isRunning, bool isCrouching)
    {
        AudioClip[] soundArray;

        if (isCrouching)
            soundArray = currentSoundSet.crouchSounds;
        else if (isRunning)
            soundArray = currentSoundSet.runSounds;
        else
            soundArray = currentSoundSet.walkSounds;

        PlayRandomSound(soundArray, footstepAudioSource);
    }

    /// <summary>
    /// 播放着陆音效
    /// </summary>
    public void PlayLandingSound()
    {
        PlayRandomSound(currentSoundSet.landSounds, effectAudioSource);
    }

    /// <summary>
    /// 从音效数组中随机播放一个
    /// </summary>
    void PlayRandomSound(AudioClip[] sounds, AudioSource audioSource)
    {
        if (sounds == null || sounds.Length == 0 || audioSource == null)
            return;

        AudioClip clip = sounds[Random.Range(0, sounds.Length)];
        if (clip == null) return;

        // 设置音量和音调
        float volume = masterVolume * currentSoundSet.volumeMultiplier;
        float pitch = 1f + Random.Range(-currentSoundSet.pitchVariation, currentSoundSet.pitchVariation);

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);

        if (showDebugInfo)
        {
            Debug.Log($"播放音效: {clip.name} | 材质: {currentMaterialName} | 音量: {volume:F2} | 音调: {pitch:F2}");
        }
    }

    /// <summary>
    /// 手动播放特定类型的脚步声（供外部调用）
    /// </summary>
    public void PlayStepSound(string soundType)
    {
        switch (soundType.ToLower())
        {
            case "walk":
                PlayRandomSound(currentSoundSet.walkSounds, footstepAudioSource);
                break;
            case "run":
                PlayRandomSound(currentSoundSet.runSounds, footstepAudioSource);
                break;
            case "crouch":
                PlayRandomSound(currentSoundSet.crouchSounds, footstepAudioSource);
                break;
            case "land":
                PlayRandomSound(currentSoundSet.landSounds, effectAudioSource);
                break;
        }
    }

    /// <summary>
    /// 获取当前材质信息（调试用）
    /// </summary>
    public string GetCurrentMaterialInfo()
    {
        return $"当前材质: {currentMaterialName} | 音效组: {currentSoundSet?.materialName ?? "null"}";
    }
}

/// <summary>
/// 可选：给非地形物体添加材质类型信息
/// </summary>
public class MaterialType : MonoBehaviour
{
    [Tooltip("材质名称，对应TerrainFootstepSystem中的配置")]
    public string materialName = "default";
}
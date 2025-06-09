using UnityEngine;

public class EasyFootstep : MonoBehaviour
{
    [Header("草地音效")]
    public AudioClip[] grassWalk;
    public AudioClip[] grassRun;

    [Header("泥土音效")]
    public AudioClip[] dirtWalk;
    public AudioClip[] dirtRun;

    [Header("沙地音效")]
    public AudioClip[] sandWalk;
    public AudioClip[] sandRun;

    [Header("岩石音效")]
    public AudioClip[] rockWalk;
    public AudioClip[] rockRun;

    [Header("雪地音效")]
    public AudioClip[] snowWalk;
    public AudioClip[] snowRun;

    [Header("默认音效")]
    public AudioClip[] defaultWalk;
    public AudioClip[] defaultRun;

    [Header("设置")]
    public float volume = 0.7f;
    public float walkStepTime = 0.5f;
    public float runStepTime = 0.3f;

    [Header("调试")]
    public bool showDebug = true;

    private AudioSource audio;
    private float timer = 0f;
    private string currentGround = "default";

    void Start()
    {
        audio = GetComponent<AudioSource>();
        if (audio == null)
            audio = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        CheckGround();
        CheckMovement();
    }

    void CheckGround()
    {
        // 向下发射射线检测地面
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 3f))
        {
            // 检查是否击中地形
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                currentGround = GetTerrainType(terrain, hit.point);
            }
            else
            {
                currentGround = "default";
            }

            if (showDebug)
            {
                Debug.DrawRay(transform.position, Vector3.down * 3f, Color.green);
            }
        }
        else
        {
            currentGround = "default";
            if (showDebug)
            {
                Debug.DrawRay(transform.position, Vector3.down * 3f, Color.red);
            }
        }
    }

    string GetTerrainType(Terrain terrain, Vector3 worldPos)
    {
        // 获取地形数据
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        // 计算在地形贴图上的位置
        int mapX = Mathf.RoundToInt(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = Mathf.RoundToInt(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

        // 确保在范围内
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

        // 根据材质索引返回地面类型
        switch (maxIndex)
        {
            case 0: return "grass";    // 第一个材质 = 草地
            case 1: return "dirt";     // 第二个材质 = 泥土
            case 2: return "sand";     // 第三个材质 = 沙地
            case 3: return "rock";     // 第四个材质 = 岩石
            case 4: return "snow";     // 第五个材质 = 雪地
            default: return "default";
        }
    }

    void CheckMovement()
    {
        // 检查是否在移动
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool moving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

        // 检查是否在跑步 (按住Shift键)
        bool running = Input.GetButton("Fire3");  // Left Shift

        if (moving)
        {
            timer += Time.deltaTime;
            float stepTime = running ? runStepTime : walkStepTime;

            if (timer >= stepTime)
            {
                PlayFootstep(running);
                timer = 0f;
            }
        }
        else
        {
            timer = 0f;
        }

        // 调试信息
        if (showDebug)
        {
            Debug.Log($"地面: {currentGround} | 移动: {moving} | 跑步: {running}");
        }
    }

    void PlayFootstep(bool isRunning)
    {
        AudioClip[] sounds = GetSounds(isRunning);

        if (sounds != null && sounds.Length > 0)
        {
            AudioClip sound = sounds[Random.Range(0, sounds.Length)];
            if (sound != null && audio != null)
            {
                audio.pitch = Random.Range(0.9f, 1.1f);
                audio.PlayOneShot(sound, volume);

                if (showDebug)
                {
                    Debug.Log($"播放: {currentGround} {(isRunning ? "跑步" : "走路")} 音效");
                }
            }
        }
    }

    AudioClip[] GetSounds(bool isRunning)
    {
        switch (currentGround)
        {
            case "grass": return isRunning ? grassRun : grassWalk;
            case "dirt": return isRunning ? dirtRun : dirtWalk;
            case "sand": return isRunning ? sandRun : sandWalk;
            case "rock": return isRunning ? rockRun : rockWalk;
            case "snow": return isRunning ? snowRun : snowWalk;
            default: return isRunning ? defaultRun : defaultWalk;
        }
    }
}
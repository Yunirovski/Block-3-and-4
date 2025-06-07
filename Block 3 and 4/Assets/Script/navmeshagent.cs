// 一键修复脚本 - 添加到任意GameObject上，运行一次即可
using UnityEngine;
using UnityEngine.AI;

public class AnimalSetupFixer : MonoBehaviour
{
    [Header("修复选项")]
    [Tooltip("移除所有动物的Rigidbody")]
    public bool removeRigidbody = true;

    [Tooltip("确保所有动物有NavMeshAgent")]
    public bool ensureNavMeshAgent = true;

    [Tooltip("自动配置NavMeshAgent设置")]
    public bool autoConfigureAgent = true;

    [ContextMenu("修复所有动物")]
    public void FixAllAnimals()
    {
        // 找到所有有AnimalBehavior的对象
        AnimalBehavior[] animals = FindObjectsOfType<AnimalBehavior>();

        int fixedCount = 0;

        foreach (AnimalBehavior animal in animals)
        {
            GameObject animalObj = animal.gameObject;
            bool wasFixed = false;

            // 1. 处理Rigidbody
            if (removeRigidbody)
            {
                Rigidbody rb = animalObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    DestroyImmediate(rb);
                    Debug.Log($"✅ {animalObj.name}: 已移除Rigidbody");
                    wasFixed = true;
                }
            }

            // 2. 确保有NavMeshAgent
            if (ensureNavMeshAgent)
            {
                NavMeshAgent agent = animalObj.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = animalObj.AddComponent<NavMeshAgent>();
                    Debug.Log($"✅ {animalObj.name}: 已添加NavMeshAgent");
                    wasFixed = true;
                }

                // 3. 自动配置Agent
                if (autoConfigureAgent && agent != null)
                {
                    ConfigureAgentForAnimal(agent, animalObj.name);
                    wasFixed = true;
                }
            }

            // 4. 修正Transform
            FixAnimalTransform(animalObj.transform);

            if (wasFixed)
            {
                fixedCount++;
            }
        }

        Debug.Log($"🎉 修复完成！共修复了 {fixedCount} 只动物");

        // 提示重新烘焙NavMesh
        Debug.Log("⚠️ 建议重新烘焙NavMesh：Window > AI > Navigation > Bake");
    }

    void ConfigureAgentForAnimal(NavMeshAgent agent, string animalName)
    {
        // 根据动物名字判断大小
        string name = animalName.ToLower();

        if (name.Contains("elephant") || name.Contains("giraffe"))
        {
            // 巨型动物
            agent.radius = 2.0f;
            agent.height = 4.0f;
            Debug.Log($"🐘 {animalName}: 配置为巨型动物");
        }
        else if (name.Contains("camel") || name.Contains("hippo") || name.Contains("rhino"))
        {
            // 大型动物
            agent.radius = 1.5f;
            agent.height = 3.0f;
            Debug.Log($"🐪 {animalName}: 配置为大型动物");
        }
        else if (name.Contains("goat") || name.Contains("donkey"))
        {
            // 中型动物
            agent.radius = 0.8f;
            agent.height = 2.0f;
            Debug.Log($"🐐 {animalName}: 配置为中型动物");
        }
        else if (name.Contains("pigeon") || name.Contains("bird"))
        {
            // 小型动物
            agent.radius = 0.3f;
            agent.height = 0.5f;
            Debug.Log($"🐦 {animalName}: 配置为小型动物");
        }
        else
        {
            // 默认中型
            agent.radius = 1.0f;
            agent.height = 2.5f;
            Debug.Log($"🦌 {animalName}: 配置为默认中型动物");
        }

        // 通用设置
        agent.speed = 3.5f;
        agent.acceleration = 8f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = agent.radius;
        agent.autoBraking = true;
        agent.updateRotation = false; // 让脚本控制旋转
    }

    void FixAnimalTransform(Transform animalTransform)
    {
        // 确保动物站立
        Vector3 rotation = animalTransform.eulerAngles;
        rotation.x = 0f;
        rotation.z = 0f;
        animalTransform.eulerAngles = rotation;
    }

    [ContextMenu("显示NavMesh建议设置")]
    public void ShowNavMeshRecommendations()
    {
        Debug.Log("🔧 推荐的NavMesh Surface设置：");
        Debug.Log("Agent Radius: 2.0");
        Debug.Log("Agent Height: 4.0");
        Debug.Log("Max Slope: 30");
        Debug.Log("Step Height: 0.4");
        Debug.Log("📍 设置路径：选择地形 > NavMesh Surface > 修改设置 > Bake");
    }

    [ContextMenu("检查当前状态")]
    public void CheckCurrentStatus()
    {
        AnimalBehavior[] animals = FindObjectsOfType<AnimalBehavior>();

        int withRigidbody = 0;
        int withNavMesh = 0;
        int withBoth = 0;

        foreach (AnimalBehavior animal in animals)
        {
            bool hasRB = animal.GetComponent<Rigidbody>() != null;
            bool hasNav = animal.GetComponent<NavMeshAgent>() != null;

            if (hasRB) withRigidbody++;
            if (hasNav) withNavMesh++;
            if (hasRB && hasNav) withBoth++;
        }

        Debug.Log($"📊 当前状态统计：");
        Debug.Log($"总动物数: {animals.Length}");
        Debug.Log($"有Rigidbody的: {withRigidbody}");
        Debug.Log($"有NavMeshAgent的: {withNavMesh}");
        Debug.Log($"⚠️ 同时有两者的: {withBoth} (需要修复)");
    }
}
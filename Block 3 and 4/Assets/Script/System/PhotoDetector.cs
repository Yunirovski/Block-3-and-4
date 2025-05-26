// Assets/Scripts/Systems/PhotoDetector.cs
using System.Collections.Generic;
using UnityEngine;

public struct PhotoResult { public int stars; }

public class PhotoDetector : MonoBehaviour
{
    private static PhotoDetector _instance;

    public static PhotoDetector Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PhotoDetector>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("PhotoDetector");
                    _instance = go.AddComponent<PhotoDetector>();
                    Debug.Log("PhotoDetector: Auto-created instance");
                }
            }
            return _instance;
        }
    }

    [Header("Area Score (Ten-thousandths)")]
    [Tooltip("Min area for 1★ (e.g. 30 = 0.3%)")]
    [Range(1, 1000)] public int star1MinArea = 30;

    [Tooltip("Min area for 2★ (e.g. 100 = 1%)")]
    [Range(1, 1000)] public int star2MinArea = 100;

    [Tooltip("Min area for 3★ (e.g. 500 = 5%)")]
    [Range(1, 2000)] public int star3MinArea = 500;

    [Tooltip("Min area for 4★ (e.g. 1500 = 15%)")]
    [Range(1, 3000)] public int star4MinArea = 1500;

    [Header("Distance Detection")]
    [Tooltip("Max detection distance")]
    public float maxDetectionDistance = 100f;

    [Header("Debug Mode")]
    [Tooltip("Show debug info")]
    public bool showDetailedDebug = true;

    [Header("Multi-target penalty")]
    [Tooltip("Penalty when multiple targets shown")]
    public int multiTargetPenalty = 1;

    [Header("Debug Display")]
    public bool showDebugInfo = true;
    public bool showAnimalBounds = true;
    public bool showAreaInfo = true;
    public bool showDistanceInfo = true;

    [Header("Debug Style")]
    public Color star1Color = Color.red;
    public Color star2Color = Color.yellow;
    public Color star3Color = Color.green;
    public Color star4Color = Color.cyan;
    public Color tooFarColor = Color.gray;
    public Color scoreTextColor = Color.white;
    public int debugFontSize = 16;

    private struct AnimalDebugInfo
    {
        public string animalName;
        public Rect screenBounds;
        public Vector2 center;
        public float areaPercent;
        public int areaPoints;
        public int stars;
        public float distanceToCamera;
        public bool isTooFar;
        public Color displayColor;
    }

    private List<AnimalDebugInfo> currentAnimals = new List<AnimalDebugInfo>();
    private Camera debugCamera;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            this.enabled = false;
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (showDebugInfo && Camera.main != null)
        {
            debugCamera = Camera.main;
            UpdateDebugInfo();
        }
    }

    void UpdateDebugInfo()
    {
        currentAnimals.Clear();

        var animals = FindObjectsOfType<AnimalEvent>();
        var planes = GeometryUtility.CalculateFrustumPlanes(debugCamera);

        List<AnimalDebugInfo> allAnimals = new List<AnimalDebugInfo>();

        foreach (var animal in animals)
        {
            if (!animal.gameObject.activeInHierarchy) continue;
            var collider = animal.GetComponent<Collider>();
            if (collider == null) continue;
            if (!GeometryUtility.TestPlanesAABB(planes, collider.bounds)) continue;

            var info = CalculateAnimalInfo(debugCamera, collider.bounds, animal.animalName);
            allAnimals.Add(info);
        }

        allAnimals.Sort((a, b) => a.distanceToCamera.CompareTo(b.distanceToCamera));
        currentAnimals.AddRange(allAnimals);
    }

    AnimalDebugInfo CalculateAnimalInfo(Camera cam, Bounds bounds, string animalName)
    {
        AnimalDebugInfo info = new AnimalDebugInfo();
        info.animalName = animalName;
        info.distanceToCamera = Vector3.Distance(cam.transform.position, bounds.center);
        info.isTooFar = info.distanceToCamera > maxDetectionDistance;

        List<Vector3> screenPoints = new List<Vector3>();
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                ((i & 1) == 0 ? bounds.min.x : bounds.max.x),
                ((i & 2) == 0 ? bounds.min.y : bounds.max.y),
                ((i & 4) == 0 ? bounds.min.z : bounds.max.z)
            );

            Vector3 screenPoint = cam.WorldToScreenPoint(corner);
            if (screenPoint.z > 0)
            {
                screenPoints.Add(screenPoint);
            }
        }

        if (screenPoints.Count == 0)
        {
            info.areaPercent = 0;
            info.stars = 0;
            info.displayColor = tooFarColor;
            return info;
        }

        Vector2 min = screenPoints[0];
        Vector2 max = screenPoints[0];
        foreach (var point in screenPoints)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        info.screenBounds = new Rect(min, max - min);
        info.center = info.screenBounds.center;

        float screenArea = Screen.width * Screen.height;
        float boundingBoxArea = info.screenBounds.width * info.screenBounds.height;
        info.areaPercent = boundingBoxArea / screenArea;
        info.areaPoints = Mathf.RoundToInt(info.areaPercent * 10000);

        if (info.isTooFar)
        {
            info.stars = 0;
            info.displayColor = tooFarColor;
        }
        else
        {
            info.stars = CalculateStars(info.areaPoints);
            info.displayColor = GetStarColor(info.stars);
        }

        return info;
    }

    int CalculateStars(int areaPoints)
    {
        if (areaPoints >= star4MinArea) return 4;
        if (areaPoints >= star3MinArea) return 3;
        if (areaPoints >= star2MinArea) return 2;
        if (areaPoints >= star1MinArea) return 1;
        return 0;
    }

    Color GetStarColor(int stars)
    {
        switch (stars)
        {
            case 1: return star1Color;
            case 2: return star2Color;
            case 3: return star3Color;
            case 4: return star4Color;
            default: return tooFarColor;
        }
    }

    public int ScoreSingle(Camera cam, Bounds bounds)
    {
        try
        {
            float distance = Vector3.Distance(cam.transform.position, bounds.center);
            if (distance > maxDetectionDistance) return 0;

            List<Vector3> screenPoints = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    ((i & 1) == 0 ? bounds.min.x : bounds.max.x),
                    ((i & 2) == 0 ? bounds.min.y : bounds.max.y),
                    ((i & 4) == 0 ? bounds.min.z : bounds.max.z)
                );

                Vector3 screenPoint = cam.WorldToScreenPoint(corner);
                if (screenPoint.z > 0) screenPoints.Add(screenPoint);
            }

            if (screenPoints.Count == 0) return 0;

            Vector2 min = screenPoints[0];
            Vector2 max = screenPoints[0];
            foreach (var point in screenPoints)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            float boundingBoxArea = (max.x - min.x) * (max.y - min.y);
            float screenArea = Screen.width * Screen.height;
            float areaPercent = boundingBoxArea / screenArea;
            int areaPoints = Mathf.RoundToInt(areaPercent * 10000);
            return CalculateStars(areaPoints);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PhotoDetector: Scoring error: {e.Message}");
            return 1;
        }
    }

    public float GetAreaPercent(Camera cam, Bounds bounds)
    {
        try
        {
            float distance = Vector3.Distance(cam.transform.position, bounds.center);
            if (distance > maxDetectionDistance) return 0f;

            List<Vector3> screenPoints = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    ((i & 1) == 0 ? bounds.min.x : bounds.max.x),
                    ((i & 2) == 0 ? bounds.min.y : bounds.max.y),
                    ((i & 4) == 0 ? bounds.min.z : bounds.max.z)
                );

                Vector3 screenPoint = cam.WorldToScreenPoint(corner);
                if (screenPoint.z > 0) screenPoints.Add(screenPoint);
            }

            if (screenPoints.Count == 0) return 0f;

            Vector2 min = screenPoints[0];
            Vector2 max = screenPoints[0];
            foreach (var point in screenPoints)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            float boundingBoxArea = (max.x - min.x) * (max.y - min.y);
            float screenArea = Screen.width * Screen.height;
            return boundingBoxArea / screenArea;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PhotoDetector: Area percent error: {e.Message}");
            return 0f;
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo || currentAnimals.Count == 0) return;

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = debugFontSize;
        textStyle.normal.textColor = scoreTextColor;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = CreateColorTexture(new Color(0, 0, 0, 0.7f));

        float yOffset = 10;

        foreach (var animal in currentAnimals)
        {
            if (showAnimalBounds)
            {
                DrawScreenRect(animal.screenBounds, animal.displayColor, 3f);
                DrawScreenPoint(animal.center, animal.displayColor, 8f);
            }

            if (showAreaInfo)
            {
                string infoText;
                if (animal.isTooFar)
                {
                    infoText = $"{animal.animalName}: TOO FAR!\n" +
                               $"Distance: {animal.distanceToCamera:F1}m (max: {maxDetectionDistance}m)";
                }
                else
                {
                    infoText = $"{animal.animalName}: {animal.stars}★\n";

                    if (showDetailedDebug)
                    {
                        float boundingArea = animal.screenBounds.width * animal.screenBounds.height;
                        float screenArea = Screen.width * Screen.height;
                        infoText += $"Box: {animal.screenBounds.width:F0}x{animal.screenBounds.height:F0}\n";
                        infoText += $"Screen: {Screen.width}x{Screen.height}\n";
                        infoText += $"Box Area: {boundingArea:F0}\n";
                        infoText += $"Screen Area: {screenArea:F0}\n";
                    }

                    infoText += $"Size Score: {animal.areaPoints}/10000\n";
                    infoText += $"Percent: {animal.areaPercent:P2}\n";

                    if (showDistanceInfo)
                    {
                        infoText += $"Distance: {animal.distanceToCamera:F1}m\n";
                    }

                    if (animal.stars < 4)
                    {
                        int nextStarRequirement = GetNextStarRequirement(animal.stars);
                        float nextStarPercent = nextStarRequirement / 10000f;
                        infoText += $"Next ★ needs: {nextStarRequirement}/10000 ({nextStarPercent:P2})";
                    }
                    else
                    {
                        infoText += "Wow! Best ★!";
                    }
                }

                Vector2 textSize = textStyle.CalcSize(new GUIContent(infoText));
                Rect textRect = new Rect(10, yOffset, textSize.x + 10, textSize.y + 10);

                GUI.Box(textRect, "", boxStyle);
                GUI.Label(new Rect(textRect.x + 5, textRect.y + 5, textRect.width, textRect.height), infoText, textStyle);

                yOffset += textRect.height + 10;
            }
        }

        if (showAreaInfo)
        {
            string standardText = "Score Rules:\n" +
                                  $"1★: {star1MinArea}/10000 ({star1MinArea / 100f:F1}%)\n" +
                                  $"2★: {star2MinArea}/10000 ({star2MinArea / 100f:F1}%)\n" +
                                  $"3★: {star3MinArea}/10000 ({star3MinArea / 100f:F1}%)\n" +
                                  $"4★: {star4MinArea}/10000 ({star4MinArea / 100f:F1}%)\n" +
                                  $"Max Distance: {maxDetectionDistance}m";

            Vector2 standardSize = textStyle.CalcSize(new GUIContent(standardText));
            Rect standardRect = new Rect(Screen.width - standardSize.x - 20, 10, standardSize.x + 10, standardSize.y + 10);

            GUI.Box(standardRect, "", boxStyle);
            GUI.Label(new Rect(standardRect.x + 5, standardRect.y + 5, standardRect.width, standardRect.height), standardText, textStyle);
        }
    }

    int GetNextStarRequirement(int currentStars)
    {
        switch (currentStars)
        {
            case 0: return star1MinArea;
            case 1: return star2MinArea;
            case 2: return star3MinArea;
            case 3: return star4MinArea;
            default: return 10000;
        }
    }

    void DrawScreenRect(Rect rect, Color color, float thickness)
    {
        rect.y = Screen.height - rect.y - rect.height;

        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawScreenPoint(Vector2 point, Color color, float size)
    {
        point.y = Screen.height - point.y;

        GUI.color = color;
        GUI.DrawTexture(new Rect(point.x - size / 2, point.y - size / 2, size, size), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnDisable()
    {
        if (_instance == this) _instance = null;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}

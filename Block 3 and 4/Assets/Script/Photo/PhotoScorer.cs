using UnityEngine;

/// <summary>
/// Represents detailed scoring metrics for a captured photo,
/// including visibility, rarity, centering, total score, and star rating.
/// </summary>
public struct ScoreResult
{
    /// <summary>Visibility score (0每3) based on how much of the target is in view.</summary>
    public int visibility;
    /// <summary>Clamped rarity level (1每3) applied as a score bonus.</summary>
    public int rarity;
    /// <summary>Centering score (0每3) based on how close the target is to screen center.</summary>
    public int centering;
    /// <summary>Sum of visibility, rarity, and centering scores.</summary>
    public int total;
    /// <summary>Final star rating (1每3) derived from total score thresholds.</summary>
    public int stars;
}

/// <summary>
/// Static utility for evaluating a photo of a Bounds volume from a given Camera.
/// Computes discrete visibility and centering scores, applies rarity,
/// and converts the total into a star rating.
/// </summary>
public static class PhotoScorer
{
    /// <summary>
    /// Evaluates the given object bounds as seen by the camera and returns a full ScoreResult.
    /// </summary>
    /// <param name="cam">Camera used for projecting world positions to screen space.</param>
    /// <param name="bounds">World坼space bounding volume of the object.</param>
    /// <param name="rarityLevel">Raw rarity parameter, clamped to [1,3].</param>
    /// <returns>A ScoreResult containing visibility, centering, rarity, total, and stars.</returns>
    public static ScoreResult Evaluate(Camera cam, Bounds bounds, int rarityLevel)
    {
        int visibilityScore = CalculateVisibilityScore(cam, bounds);
        int centeringScore = CalculateCenteringScore(cam, bounds);
        int clampedRarity = Mathf.Clamp(rarityLevel, 1, 3);

        int totalScore = visibilityScore + centeringScore + clampedRarity;
        int starRating = totalScore <= 3 ? 1
                        : totalScore <= 6 ? 2
                        : 3;

        return new ScoreResult
        {
            visibility = visibilityScore,
            rarity = clampedRarity,
            centering = centeringScore,
            total = totalScore,
            stars = starRating
        };
    }

    /// <summary>
    /// Projects each corner of the bounds into screen space and measures
    /// the fraction of screen area they cover, mapping to a 0每3 score.
    /// </summary>
    private static int CalculateVisibilityScore(Camera cam, Bounds bounds)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        // Iterate over the 8 corners of the bounding box
        for (int i = 0; i < 8; i++)
        {
            Vector3 localOffset = new Vector3(
                ((i & 1) == 0) ? -1 : 1,
                ((i & 2) == 0) ? -1 : 1,
                ((i & 4) == 0) ? -1 : 1
            );
            Vector3 worldCorner = bounds.center + Vector3.Scale(bounds.extents, localOffset);
            Vector3 screenPos = cam.WorldToScreenPoint(worldCorner);

            // Skip corners behind the camera
            if (screenPos.z < 0f) continue;

            min = Vector2.Min(min, screenPos);
            max = Vector2.Max(max, screenPos);
        }

        float areaFraction = (max.x - min.x) * (max.y - min.y)
                             / (Screen.width * Screen.height);

        if (areaFraction < 0.20f) return 0;
        if (areaFraction < 0.40f) return 1;
        if (areaFraction < 0.60f) return 2;
        return 3;
    }

    /// <summary>
    /// Projects the bounds center into screen space, measures its distance
    /// from the screen center, and maps that to a 0每3 centering score.
    /// </summary>
    private static int CalculateCenteringScore(Camera cam, Bounds bounds)
    {
        Vector3 screenCenterPos = cam.WorldToScreenPoint(bounds.center);
        if (screenCenterPos.z < 0f)
            return 0; // Center behind the camera

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float distance = Vector2.Distance(screenCenterPos, screenCenter);
        float maxDistance = Mathf.Min(Screen.width, Screen.height) * 0.5f;
        float percentOffCenter = (distance / maxDistance) * 100f;

        if (percentOffCenter >= 50f) return 0;
        if (percentOffCenter >= 20f) return 1;
        if (percentOffCenter >= 5f) return 2;
        return 3;
    }
}

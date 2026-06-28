using UnityEngine;

public static class SwipePlaneBuilder
{
    private const float NEAR_DEPTH = 1f;
    private const float FAR_DEPTH = 10f;
    private const float MIN_SCREEN_DELTA_SQR = 0.01f;

    /// <summary>
    /// Tries to build a cut plane from the swipe.
    /// Returns false when the swipe is degenerate (start close to end) or the camera is null.
    /// </summary>
    public static bool TryBuild(Camera camera, Vector2 startScreen, Vector2 endScreen, out Plane plane)
    {
        plane = default;

        if (camera == null)
        {
            return false;
        }

        if ((startScreen - endScreen).sqrMagnitude < MIN_SCREEN_DELTA_SQR)
        {
            // Both points are basically on top of each other 
            return false;
        }

        Ray rayStart = camera.ScreenPointToRay(startScreen);
        Ray rayEnd = camera.ScreenPointToRay(endScreen);

        // Three world-space points that together contain both rays.
        Vector3 p1 = rayStart.GetPoint(NEAR_DEPTH);
        Vector3 p2 = rayEnd.GetPoint(NEAR_DEPTH);
        Vector3 p3 = rayStart.GetPoint(FAR_DEPTH);

        plane = new Plane(p1, p2, p3);
        return true;
    }
}
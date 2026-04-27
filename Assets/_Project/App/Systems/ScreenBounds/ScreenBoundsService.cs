using UnityEngine;

public sealed class ScreenBoundsService : IScreenBounds
{
    public float Left { get; private set; }
    public float Right { get; private set; }
    public float Top { get; private set; }
    public float Bottom { get; private set; }

    public void Recalculate(Camera camera)
    {
        if (camera == null)
        {
            Debug.LogError("ScreenBoundsService: camera is null.");
            return;
        }

        Vector3 bottomLeft = camera.ViewportToWorldPoint(
            new Vector3(0f, 0f, camera.nearClipPlane));

        Vector3 topRight = camera.ViewportToWorldPoint(
            new Vector3(1f, 1f, camera.nearClipPlane));

        Left = bottomLeft.x;
        Bottom = bottomLeft.y;
        Right = topRight.x;
        Top = topRight.y;
    }
}
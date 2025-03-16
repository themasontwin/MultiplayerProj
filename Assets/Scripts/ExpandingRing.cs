using UnityEngine;

public class ExpandingRing : MonoBehaviour
{
    public float expandSpeed = 1.0f;  // Speed at which the ring expands
    public float maxSize = 10.0f;     // Maximum expansion size
    public float thickness = 0.5f;    // Thickness of the ring
    private float currentSize = 1.0f; // Initial ring size

    private Transform topWall, bottomWall, leftWall, rightWall;

    void Start()
    {
        // Find the four walls based on object names
        topWall = transform.Find("Top");
        bottomWall = transform.Find("Bottom");
        leftWall = transform.Find("Left");
        rightWall = transform.Find("Right");
    }

    void Update()
    {
        if (currentSize < maxSize)
        {
            currentSize += expandSpeed * Time.deltaTime;
            UpdateRing();
        }
    }

    void UpdateRing()
    {
        float halfSize = currentSize / 2;
        float halfThickness = thickness / 2;

        // Update horizontal walls (Top & Bottom)
        if (topWall && bottomWall)
        {
            topWall.localScale = new Vector3(currentSize + thickness, topWall.localScale.y, thickness);
            bottomWall.localScale = new Vector3(currentSize + thickness, bottomWall.localScale.y, thickness);

            topWall.localPosition = new Vector3(0, 0, halfSize);
            bottomWall.localPosition = new Vector3(0, 0, -halfSize);
        }

        // Update vertical walls (Left & Right)
        if (leftWall && rightWall)
        {
            leftWall.localScale = new Vector3(thickness, leftWall.localScale.y, currentSize + thickness);
            rightWall.localScale = new Vector3(thickness, rightWall.localScale.y, currentSize + thickness);

            leftWall.localPosition = new Vector3(-halfSize, 0, 0);
            rightWall.localPosition = new Vector3(halfSize, 0, 0);
        }
    }
}

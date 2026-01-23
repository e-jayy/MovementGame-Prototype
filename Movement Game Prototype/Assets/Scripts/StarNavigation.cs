using UnityEngine;

public class StarNavigation : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 180f; // degrees per second

    [Header("Direction Check")]
    [SerializeField] private int directionValue; // 0–360 target value
    [SerializeField] private float tolerance = 3f;

    private void Update()
    {
        float rotationAmount = 0f;

        if (Input.GetKey(KeyCode.E))
            rotationAmount -= rotationSpeed * Time.deltaTime; // clockwise

        if (Input.GetKey(KeyCode.Q))
            rotationAmount += rotationSpeed * Time.deltaTime; // counter-clockwise

        if (rotationAmount != 0f)
        {
            transform.Rotate(0f, 0f, rotationAmount);
        }

        CheckDirectionInput();
    }

    private void CheckDirectionInput()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        float currentZ = GetNormalizedZRotation();

        // Proper circular comparison (handles 0/360 wrap)
        float angleDiff = Mathf.DeltaAngle(currentZ, directionValue);

        if (Mathf.Abs(angleDiff) <= tolerance)
        {
            Debug.Log("congrats");
        }
        else
        {
            Debug.Log("fail");
        }
    }

    private float GetNormalizedZRotation()
    {
        float z = transform.eulerAngles.z;
        return (z + 360f) % 360f;
    }
}

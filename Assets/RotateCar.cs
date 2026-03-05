using UnityEngine;

public class RotateCar : MonoBehaviour
{
    public float speed = 20f;

    void Update()
    {
        transform.Rotate(0, speed * Time.deltaTime, 0);
    }
}

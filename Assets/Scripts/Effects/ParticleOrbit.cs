using UnityEngine;

public class ParticleOrbit : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 180f; // degrees per second

    void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}

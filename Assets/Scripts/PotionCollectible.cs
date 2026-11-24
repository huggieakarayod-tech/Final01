
using UnityEngine;

public class PotionCollectible : MonoBehaviour
{
    //MAKE IT SPINNNNNNNNNNN!
    [Header("Spinning")]
    public float rotationSpeed = 90f;

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}

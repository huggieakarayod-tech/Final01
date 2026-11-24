
using UnityEngine;

public class PotionPickup : MonoBehaviour
{
    private bool playerNearby = false;
    private GameObject player;

    void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            CollectPotion();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            player = other.gameObject;

            Debug.Log("Press E to collect potion!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            player = null;
        }
    }

    void CollectPotion()
    {
        PlayerHealth health = player.GetComponentInParent<PlayerHealth>();

        ChickenTransformation transformation = player.GetComponentInParent<ChickenTransformation>();

        if (health == null)
        {
            Debug.LogWarning("PlayerHealth not found!");
            return;
        }

        health.HealByPercentage(0.3f);

        if (transformation != null)
        {
            transformation.TransformIntoChicken();
        }
        else
        {
            Debug.LogWarning("ChickenTransformation not found!");
        }

        Debug.Log("Potion collected! Player healed.");

        Destroy(gameObject);
    }
}

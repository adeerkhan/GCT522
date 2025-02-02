using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Call the Game Over UI
            GameUI.Instance.ShowGameOver();

        }
    }
}

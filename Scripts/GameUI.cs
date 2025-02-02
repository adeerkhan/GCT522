// GameUI.cs
using UnityEngine;
using TMPro; // Import TextMeshPro namespace

public class GameUI : MonoBehaviour
{
    // Singleton instance for easy access
    public static GameUI Instance;

    [Header("UI Elements")]
    public GameObject messagePanel; // Assign via Inspector
    public TextMeshProUGUI initialMessageText; // Assign via Inspector
    public TextMeshProUGUI gameOverText; // Assign via Inspector
    public TextMeshProUGUI congratsText; // Assign via Inspector

    [Header("Message Durations")]
    public float initialMessageDuration = 5f; // Duration to display the initial message
    public float gameOverDisplayDuration = 3f; // Duration to display the Game Over message
    public float congratsDisplayDuration = 5f; // Duration to display the Congrats message

    void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes if needed
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Ensure the MessagePanel is active
        if (messagePanel != null)
        {
            messagePanel.SetActive(false); // Hide initially; will be shown when messages are triggered
        }
        else
        {
            Debug.LogError("MessagePanel is not assigned in the GameUI script.");
        }

        // Display the initial message at the start of the game
        ShowInitialMessage();
    }

    // Method to display the initial message
    public void ShowInitialMessage()
    {
        if (initialMessageText != null && messagePanel != null)
        {
            messagePanel.SetActive(true); // Show the panel
            initialMessageText.gameObject.SetActive(true); // Show the initial message
            initialMessageText.text = "It's raining heavily, find your way back home but be careful, the ground is slippery and wet, don't fall into the river or sea.";
            gameOverText.gameObject.SetActive(false); // Ensure Game Over text is hidden
            congratsText.gameObject.SetActive(false); // Ensure Congrats text is hidden
            Invoke(nameof(HideInitialMessage), initialMessageDuration);
        }
        else
        {
            Debug.LogError("InitialMessageText or MessagePanel is not assigned in the GameUI script.");
        }
    }

    // Method to hide the initial message and the panel
    void HideInitialMessage()
    {
        if (initialMessageText != null && messagePanel != null)
        {
            initialMessageText.gameObject.SetActive(false); // Hide the initial message
            messagePanel.SetActive(false); // Hide the panel
        }
    }

    // Method to display the Game Over message
    public void ShowGameOver()
    {
        if (gameOverText != null && messagePanel != null)
        {
            messagePanel.SetActive(true); // Show the panel
            gameOverText.gameObject.SetActive(true); // Show the Game Over message
            initialMessageText.gameObject.SetActive(false); // Ensure Initial Message is hidden
            congratsText.gameObject.SetActive(false); // Ensure Congrats text is hidden
            Invoke(nameof(HideGameOver), gameOverDisplayDuration);
        }
        else
        {
            Debug.LogError("GameOverText or MessagePanel is not assigned in the GameUI script.");
        }
    }

    // Method to hide the Game Over message and the panel
    void HideGameOver()
    {
        if (gameOverText != null && messagePanel != null)
        {
            gameOverText.gameObject.SetActive(false); // Hide the Game Over message
            messagePanel.SetActive(false); // Hide the panel
        }
    }

    // Method to display the Congrats message
    public void ShowCongrats()
    {
        if (congratsText != null && messagePanel != null)
        {
            messagePanel.SetActive(true); // Show the panel
            congratsText.gameObject.SetActive(true); // Show the Congrats message
            initialMessageText.gameObject.SetActive(false); // Ensure Initial Message is hidden
            gameOverText.gameObject.SetActive(false); // Ensure Game Over text is hidden
            Invoke(nameof(HideCongrats), congratsDisplayDuration);
        }
        else
        {
            Debug.LogError("CongratsText or MessagePanel is not assigned in the GameUI script.");
        }
    }

    // Method to hide the Congrats message and the panel
    void HideCongrats()
    {
        if (congratsText != null && messagePanel != null)
        {
            congratsText.gameObject.SetActive(false); // Hide the Congrats message
            messagePanel.SetActive(false); // Hide the panel
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);
    }

    private void Update()
    {
        if (IsGameOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }
}

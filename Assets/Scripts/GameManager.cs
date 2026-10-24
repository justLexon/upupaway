using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const string BestAltitudeKey = "BestAltitude";

    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private ShipController ship;
    [SerializeField] private TMP_Text resultText;

    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }

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

    // Freezes the world (pitstops). UI and shop animations use unscaled time.
    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        if (!IsGameOver)
            Time.timeScale = paused ? 0f : 1f;
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        gameOverPanel.SetActive(true);

        if (ship == null || resultText == null)
            return;

        float best = Mathf.Max(PlayerPrefs.GetFloat(BestAltitudeKey, 0f), ship.MaxAltitude);
        PlayerPrefs.SetFloat(BestAltitudeKey, best);
        PlayerPrefs.Save();
        resultText.text = $"Peak altitude: {ship.MaxAltitude:N0} m\nBest: {best:N0} m";
    }
}

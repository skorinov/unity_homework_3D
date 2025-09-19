using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    /// <summary>
    /// Main game manager that persists between scenes and manages game systems
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        private bool _isPaused = false;
        private bool _isGameStarted = false;
        
        // Events for other systems to subscribe to
        public System.Action OnGameStarted;
        public System.Action OnGameRestarted;
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;
        
        private void Awake()
        {
            // Persistent singleton across scenes
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeGame()
        {
            // Subscribe to scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Set initial game state
            Time.timeScale = 1f;
            SetCursorState(true);
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reset game state when scene reloads
            _isPaused = false;
            _isGameStarted = true;
            Time.timeScale = 1f;
            SetCursorState(true);
            
            // Notify systems that game restarted
            OnGameRestarted?.Invoke();
        }
        
        public void StartGame()
        {
            if (_isGameStarted) return;
            
            _isGameStarted = true;
            Time.timeScale = 1f;
            SetCursorState(true);
            
            OnGameStarted?.Invoke();
        }
        
        public void PauseGame()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            Time.timeScale = 0f;
            SetCursorState(false);
            
            // Disable player controls
            DisablePlayerControls();
            
            OnGamePaused?.Invoke();
        }
        
        public void ResumeGame()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            Time.timeScale = 1f;
            SetCursorState(true);
            
            // Enable player controls
            EnablePlayerControls();
            
            OnGameResumed?.Invoke();
        }
        
        public void RestartGame()
        {
            // Reset state before reloading
            _isPaused = false;
            _isGameStarted = false;
            Time.timeScale = 1f;
            
            // Reload current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        
        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        private void EnablePlayerControls()
        {
            var playerController = FindFirstObjectByType<Player.PlayerController>();
            if (playerController)
                playerController.enabled = true;
        }
        
        private void DisablePlayerControls()
        {
            var playerController = FindFirstObjectByType<Player.PlayerController>();
            if (playerController)
                playerController.enabled = false;
        }
        
        private void SetCursorState(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        
        // Public getters
        public bool IsPaused => _isPaused;
        public bool IsGameStarted => _isGameStarted;
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
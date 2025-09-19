using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using InputSystem = Core.InputSystem;

namespace UI
{
    /// <summary>
    /// UI manager with proper Input System integration
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Windows")]
        [SerializeField] private Canvas gameUICanvas;
        [SerializeField] private Canvas pauseMenuCanvas;
        [SerializeField] private Canvas settingsMenuCanvas;
        
        [Header("Pause Menu Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        
        [Header("Settings Menu Buttons")]
        [SerializeField] private Button backButton;
        
        [Header("Pause Menu")]
        [SerializeField] private TextMeshProUGUI title;
        
        private bool _isPlayerDead = false;
        private InputSystem _inputActions;
        
        private void Awake()
        {
            _inputActions = new InputSystem();
        }
        
        private void OnEnable()
        {
            _inputActions.UI.Enable();
            _inputActions.UI.Cancel.performed += OnCancelInput;
        }
        
        private void OnDisable()
        {
            _inputActions.UI.Cancel.performed -= OnCancelInput;
            _inputActions.UI.Disable();
        }
        
        private void Start()
        {
            if (!ValidateCanvasReferences()) return;
            
            SetupInitialState();
            SetupButtonEvents();
            SubscribeToEvents();
        }
        
        private void OnCancelInput(InputAction.CallbackContext context)
        {
            if (!ValidateCanvasReferences()) return;
            
            if (settingsMenuCanvas.gameObject.activeInHierarchy)
            {
                ShowPauseMenu();
            }
            else if (pauseMenuCanvas.gameObject.activeInHierarchy)
            {
                OnResumeClicked();
            }
            else if (!_isPlayerDead)
            {
                OnPauseRequested();
            }
        }
        
        private bool ValidateCanvasReferences()
        {
            if (!gameUICanvas || !pauseMenuCanvas || !settingsMenuCanvas)
            {
                Debug.LogError("[UIManager] Canvas references not set in inspector!");
                return false;
            }
            return true;
        }
        
        private void SetupInitialState()
        {
            gameUICanvas.gameObject.SetActive(true);
            pauseMenuCanvas.gameObject.SetActive(false);
            settingsMenuCanvas.gameObject.SetActive(false);
        }
        
        private void SetupButtonEvents()
        {
            if (resumeButton) resumeButton.onClick.AddListener(OnResumeClicked);
            if (restartButton) restartButton.onClick.AddListener(OnRestartClicked);
            if (settingsButton) settingsButton.onClick.AddListener(ShowSettings);
            if (quitButton) quitButton.onClick.AddListener(OnQuitClicked);
            if (backButton) backButton.onClick.AddListener(ShowPauseMenu);
        }
        
        private void SubscribeToEvents()
        {
            if (Managers.GameManager.Instance)
            {
                Managers.GameManager.Instance.OnGamePaused += OnGamePaused;
                Managers.GameManager.Instance.OnGameResumed += OnGameResumed;
            }
            
            var playerHealth = FindFirstObjectByType<Player.PlayerHealth>();
            if (playerHealth)
            {
                playerHealth.OnPlayerDied += OnPlayerDied;
            }
        }
        
        // Button event handlers
        public void OnResumeClicked()
        {
            if (_isPlayerDead)
            {
                OnRestartClicked();
                return;
            }
            
            if (Managers.GameManager.Instance)
                Managers.GameManager.Instance.ResumeGame();
        }
        
        public void OnRestartClicked()
        {
            if (Managers.GameManager.Instance)
                Managers.GameManager.Instance.RestartGame();
        }
        
        public void OnQuitClicked()
        {
            if (Managers.GameManager.Instance)
                Managers.GameManager.Instance.QuitGame();
        }
        
        public void OnPauseRequested()
        {
            if (Managers.GameManager.Instance)
                Managers.GameManager.Instance.PauseGame();
        }
        
        // GameManager event handlers
        private void OnGamePaused()
        {
            ShowPauseMenu();
        }
        
        private void OnGameResumed()
        {
            ShowGameUI();
        }
        
        private void OnPlayerDied()
        {
            _isPlayerDead = true;
            OnPauseRequested();
        }
        
        // UI state methods
        public void ShowSettings()
        {
            if (!ValidateCanvasReferences()) return;
            
            gameUICanvas.gameObject.SetActive(false);
            pauseMenuCanvas.gameObject.SetActive(false);
            settingsMenuCanvas.gameObject.SetActive(true);
            
            ClearSelectionAndSetFirst(backButton);
        }
        
        public void ShowPauseMenu()
        {
            if (!ValidateCanvasReferences()) return;
            
            gameUICanvas.gameObject.SetActive(false);
            pauseMenuCanvas.gameObject.SetActive(true);
            settingsMenuCanvas.gameObject.SetActive(false);
            
            UpdatePauseMenuContent();
            
            Button firstButton = _isPlayerDead ? restartButton : resumeButton;
            ClearSelectionAndSetFirst(firstButton);
        }
        
        public void ShowGameUI()
        {
            if (!ValidateCanvasReferences()) return;
            
            gameUICanvas.gameObject.SetActive(true);
            pauseMenuCanvas.gameObject.SetActive(false);
            settingsMenuCanvas.gameObject.SetActive(false);
            
            ClearAllSelection();
        }
        
        private void UpdatePauseMenuContent()
        {
            if (resumeButton && title)
            {
                if (_isPlayerDead)
                {
                    title.text = "You are dead!";
                    resumeButton.gameObject.SetActive(false);
                }
                else
                {
                    title.text = "Pause";
                    resumeButton.gameObject.SetActive(true);
                }
            }
        }
        
        private void ClearSelectionAndSetFirst(Button button)
        {
            if (!button) return;
            
            EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(SetSelectionNextFrame(button.gameObject));
        }
        
        private void ClearAllSelection()
        {
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(null);
        }
        
        private System.Collections.IEnumerator SetSelectionNextFrame(GameObject target)
        {
            yield return null;
            if (EventSystem.current && target)
                EventSystem.current.SetSelectedGameObject(target);
        }
        
        private void OnDestroy()
        {
            if (Managers.GameManager.Instance)
            {
                Managers.GameManager.Instance.OnGamePaused -= OnGamePaused;
                Managers.GameManager.Instance.OnGameResumed -= OnGameResumed;
            }
            
            var playerHealth = FindFirstObjectByType<Player.PlayerHealth>();
            if (playerHealth)
            {
                playerHealth.OnPlayerDied -= OnPlayerDied;
            }
            
            _inputActions?.Dispose();
        }
    }
}
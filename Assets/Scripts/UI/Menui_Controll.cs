
using UnityEngine;

[AddComponentMenu("UI/Menu Control")]
public class Menui_Controll : MonoBehaviour
{
    [Tooltip("Canvas (or menu GameObject) that will be shown/hidden when pressing Escape.")]
    [SerializeField] private GameObject menuCanvas;

    [Tooltip("Reference to the FPS controller to disable/enable when the menu is open/closed.")]
    [SerializeField] private FPS fpsController;

    [Tooltip("If true the menu starts closed.")]
    [SerializeField] private bool startClosed = true;

    private void Reset()
    {
        // Try auto-assign common cases to make setup easier
        if (menuCanvas == null)
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null) menuCanvas = canvas.gameObject;
        }

        if (fpsController == null)
            fpsController = FindObjectOfType<FPS>();
    }

    private void Awake()
    {
        if (fpsController == null)
            fpsController = FindObjectOfType<FPS>();

        if (menuCanvas == null)
            Debug.LogWarning("Menui_Controll: menuCanvas is not assigned.", this);
    }

    private void Start()
    {
        // Ensure initial state
        if (menuCanvas != null)
            menuCanvas.SetActive(!startClosed);

        if (fpsController != null)
            fpsController.enabled = startClosed;

        // If menu starts open, make sure cursor is visible
        if (!startClosed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // locked by FPS on start normally; try to enforce
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (menuCanvas == null)
            return;

        bool willOpen = !menuCanvas.activeSelf;
        menuCanvas.SetActive(willOpen);

        if (willOpen)
        {
            // Open menu: disable player controls and show cursor
            if (fpsController != null)
                fpsController.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Close menu: re-enable player controls and lock cursor via FPS (preferred)
            if (fpsController != null)
            {
                fpsController.enabled = true;
                fpsController.LockCursor();
            }
            else
            {
                // Fallback behaviour
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void OpenMenu() { if (menuCanvas != null && !menuCanvas.activeSelf) ToggleMenu(); }
    public void CloseMenu() { if (menuCanvas != null && menuCanvas.activeSelf) ToggleMenu(); }
}

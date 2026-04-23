using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class Menu_GameOver : MonoBehaviour
{
    private void Awake()
    {
        HabilitarInputMenu();
        AsegurarEventSystem();
    }

    private void OnEnable()
    {
        HabilitarInputMenu();
    }

    private void HabilitarInputMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }

    private void AsegurarEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();
    }

    public void Cargar_Singleplayer()
    {
        SceneManager.LoadScene(1);
    }
    public void Volver_inicio()
    {
        SceneManager.LoadScene(0);
    }
}

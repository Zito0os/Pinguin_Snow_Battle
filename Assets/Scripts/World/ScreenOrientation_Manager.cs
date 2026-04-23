using UnityEngine;

public class ScreenOrientation_Manager : MonoBehaviour
{
    [Header("Configuración de Orientación")]
    [Tooltip("Forzar orientación horizontal (Landscape)")]
    public bool forzarLandscape = true;
    
    [Header("Safe Area (para dispositivos con notch)")]
    [Tooltip("Aplicar Safe Area automáticamente al Canvas")]
    public bool aplicarSafeArea = false;
    
    [Tooltip("RectTransform del panel principal (vacío = busca automáticamente)")]
    public RectTransform panelSafeArea;
    
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    
    private void Awake()
    {
        ConfigurarOrientacion();
        
        if (aplicarSafeArea)
        {
            AplicarSafeArea();
        }
    }
    
    private void Start()
    {
        ConfigurarOrientacion();
    }
    
    private void Update()
    {
        // Verificar cambios en Safe Area (para cuando rota el dispositivo)
        if (aplicarSafeArea && lastSafeArea != Screen.safeArea)
        {
            AplicarSafeArea();
        }
    }
    
    private void ConfigurarOrientacion()
    {
        if (forzarLandscape)
        {
            // Forzar orientación horizontal
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            
            // Bloquear rotación automática - solo landscape
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            
            Debug.Log("[ScreenOrientation_Manager] Orientación forzada a Landscape");
        }
    }
    
    private void AplicarSafeArea()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        
        // Si no se especificó un panel, buscar el Canvas
        if (panelSafeArea == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }
            
            if (canvas != null)
            {
                panelSafeArea = canvas.GetComponent<RectTransform>();
            }
        }
        
        if (panelSafeArea == null)
        {
            Debug.LogWarning("[ScreenOrientation_Manager] No se encontró RectTransform para aplicar Safe Area");
            return;
        }
        
        // Convertir Safe Area a anchors
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        
        panelSafeArea.anchorMin = anchorMin;
        panelSafeArea.anchorMax = anchorMax;
        
        Debug.Log($"[ScreenOrientation_Manager] Safe Area aplicada - Screen: {Screen.width}x{Screen.height}, Safe: {safeArea}");
        Debug.Log($"[ScreenOrientation_Manager] Anchors: Min {anchorMin}, Max {anchorMax}");
    }
    
    // Por si la orientación cambia durante el juego
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && forzarLandscape)
        {
            ConfigurarOrientacion();
        }
    }
}

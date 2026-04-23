using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    // Referencias
    public Transform playerBody;
    public Transform cameraTarget; // punto hacia donde mira la cámara (generalmente la cabeza del jugador)

    // Sensibilidad y control del mouse
    public float mouseSensitivity = 80f;

    // Parámetros de órbita (SOLO VERTICAL, la cámara siempre está DETRÁS)
    public float minDistance = 1.5f;  // distancia mínima cuando apunta arriba/abajo
    public float baseDistance = 2.5f; // distancia cuando el pitch es 0 grados

    // Parámetros de rotación VERTICAL SOLAMENTE
    public float maxVerticalAngle = 85f;  // máximo ángulo hacia arriba
    public float minVerticalAngle = -60f; // máximo ángulo hacia abajo

    // Offset de altura para mirar un poco hacia arriba del jugador
    public float heightOffset = 0.6f;

    // Distancia detrás del jugador
    public float horizontalDistance = 2f; // distancia base cuando el pitch es 0 grados
    public float minHorizontalDistance = 1.2f; // distancia mínima cuando mira arriba/abajo

    // Suavizado de la cámara
    public float cameraSmoothness = 0.1f;

    // Variables internas
    private float currentPitch = 0f; // rotación vertical SOLAMENTE
    private Vector3 targetCameraPosition;
    private Vector3 cameraVelocity = Vector3.zero;

    void Start()
    {
        // Inicializar la cámara en una posición válida
        if (cameraTarget == null)
        {
            cameraTarget = playerBody; // si no está asignado, usar el jugador como target
        }

        Cursor.lockState = CursorLockMode.Locked;
        targetCameraPosition = transform.position;
    }

    void LateUpdate()
    {
        // Si el EmotePanel está abierto, no procesar input de cámara
        if (EmotePanel.isEmotePanelActive)
            return;

        // Obtener input del mouse (SOLO EJE Y para la cámara)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Actualizar SOLO el ángulo vertical
        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);

        // Calcular la distancia basada en el pitch (vertical)
        // En 0 grados mantiene baseDistance, y al mirar arriba/abajo se acerca
        float maxAbsAngle = Mathf.Max(Mathf.Abs(minVerticalAngle), Mathf.Abs(maxVerticalAngle));
        float pitchAbs = Mathf.Abs(currentPitch);
        float pitchAbsNormalized = maxAbsAngle > 0f ? Mathf.Clamp01(pitchAbs / maxAbsAngle) : 0f;
        float currentDistance = Mathf.Lerp(baseDistance, minDistance, pitchAbsNormalized);
        float currentHorizontalDistance = Mathf.Lerp(horizontalDistance, minHorizontalDistance, pitchAbsNormalized);

        // La cámara SIEMPRE está detrás del jugador
        // Posición relativa: detrás (eje Z negativo en el espacio local del jugador)
        Vector3 relativePosition = new Vector3(
            0f, // sin movimiento horizontal
            heightOffset + currentDistance * Mathf.Sin(currentPitch * Mathf.Deg2Rad),
            -currentHorizontalDistance // siempre detrás y se acerca con el pitch
        );

        // Transformar la posición relativa a espacio mundial usando la rotación del jugador
        Vector3 worldRelativePosition = playerBody.TransformDirection(relativePosition);
        targetCameraPosition = cameraTarget.position + worldRelativePosition;

        // Suavizar la posición de la cámara
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetCameraPosition,
            ref cameraVelocity,
            cameraSmoothness
        );

        // Hacer que la cámara mire al target
        transform.LookAt(cameraTarget.position + Vector3.up * heightOffset);

        // Rotar el cuerpo del jugador con el movimiento horizontal del mouse
        playerBody.Rotate(Vector3.up * mouseX);
    }

    // Método para resetear la cámara si es necesario
    public void ResetCamera()
    {
        currentPitch = 0f;
    }
}
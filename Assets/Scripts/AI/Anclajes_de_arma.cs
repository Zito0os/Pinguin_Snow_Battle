using UnityEngine;
[DefaultExecutionOrder(5000)] // Se ejecuta despu�s del IK y animaciones


public class Anclajes_de_arma : MonoBehaviour
{
    [Header("Character Targets (Bones o empties)")]
    [SerializeField] private Transform boneRight;
    [SerializeField] private Transform boneLeft;

    [Header("Weapon Grips (Inside Weapon)")]
    [SerializeField] private Transform gripRight;
    [SerializeField] private Transform gripLeft;
    [SerializeField] private bool bloquearGripEnLocal = true;

    [Header("Suavizado")]
    [SerializeField] private bool usarSuavizado = true;
    [SerializeField] private float suavizadoRotacion = 20f;
    [SerializeField] private float suavizadoPosicion = 20f;
    [SerializeField] private float distanciaMinimaParaCorregir = 0.0001f;

    [Header("Ajuste fino")]
    [SerializeField] private Vector3 offsetRotacionEuler = Vector3.zero;
    [SerializeField] private Vector3 offsetPosicionLocal = Vector3.zero;

    private const float MIN_DISTANCE = 0.000001f;

    private Vector3 gripRightLocalPos;
    private Quaternion gripRightLocalRot;
    private Vector3 gripLeftLocalPos;
    private Quaternion gripLeftLocalRot;

    private void Start()
    {
        if (gripRight != null)
        {
            gripRightLocalPos = gripRight.localPosition;
            gripRightLocalRot = gripRight.localRotation;
        }

        if (gripLeft != null)
        {
            gripLeftLocalPos = gripLeft.localPosition;
            gripLeftLocalRot = gripLeft.localRotation;
        }
    }


    void LateUpdate()
    {
        if (!boneRight || !boneLeft || !gripRight || !gripLeft) return;

        if (bloquearGripEnLocal)
        {
            gripRight.localPosition = gripRightLocalPos;
            gripRight.localRotation = gripRightLocalRot;
            gripLeft.localPosition = gripLeftLocalPos;
            gripLeft.localRotation = gripLeftLocalRot;
        }

        // Direcci�n que deben tener los grips (izq -> der)
        Vector3 targetDir = boneRight.position - boneLeft.position;
        if (targetDir.sqrMagnitude < MIN_DISTANCE) return;

        // Direcci�n actual del arma (Grip_left -> Grip_right)
        Vector3 weaponDir = gripRight.position - gripLeft.position;
        if (weaponDir.sqrMagnitude < MIN_DISTANCE) return;

        Quaternion rotationOffset = Quaternion.Euler(offsetRotacionEuler);
        Vector3 adjustedWeaponDir = rotationOffset * weaponDir.normalized;

        // 1) Rotar el arma para alinear weaponDir con targetDir
        Quaternion align = Quaternion.FromToRotation(adjustedWeaponDir, targetDir.normalized);
        Quaternion targetRotation = align * transform.rotation;

        if (usarSuavizado)
        {
            float rotLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, suavizadoRotacion) * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotLerp);
        }
        else
        {
            transform.rotation = targetRotation;
        }

        // 2) Mover el arma para que Grip_left caiga EXACTO en boneLeft
        Vector3 desiredGripLeftPosition = boneLeft.position;
        if (offsetPosicionLocal.sqrMagnitude > MIN_DISTANCE)
        {
            desiredGripLeftPosition += transform.TransformDirection(offsetPosicionLocal);
        }

        Vector3 offset = desiredGripLeftPosition - gripLeft.position;
        if (offset.sqrMagnitude < distanciaMinimaParaCorregir)
            return;

        if (usarSuavizado)
        {
            Vector3 targetPosition = transform.position + offset;
            float posLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, suavizadoPosicion) * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, posLerp);
        }
        else
        {
            transform.position += offset;
        }
    }
}

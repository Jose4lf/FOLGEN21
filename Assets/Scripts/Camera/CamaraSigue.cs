using UnityEngine;
using System.Collections;

public class CamaraSigue : MonoBehaviour
{
    // Forzamos a que empiece en true (Tercera Persona)
    public bool tPerson = true; 
    
    [Header("Objetivos de camara")]
    public Transform tpTarget;
    public Transform fpTarget;
    
    [Header("Visibilidad de Jugador")]
    public bool disablePlayerMesh = true;
    [Space(2)]
    public GameObject playerMesh;
    
    [Space(5)]
    private Vector2 angle = new Vector2(180 * Mathf.Deg2Rad, 0); 
    private new Camera camera;
    private Vector2 nearPlaneSize;
    private Transform follow;
    
    private float defaultDistance;
    private float newDistance;
    
    [Header("Ajustes de camara")]
    public float maxDistance = 7f;
    public float minDistance = 2f;
    
    [Header("Distancia Inicial (Zoom de inicio)")]
    [Tooltip("Qué tan cerca quieres que empiece la cámara al dar Play")]
    public float initialDistance = 3f; // <--- NUEVO AJUSTE AQUÍ

    [Space(6)]
    public int zoomVelocity = 300;
    public float zoomSmooth = 0.1f;
    public Vector2 sensitivity = new Vector2(1, 1);
    
    [Header ("tecla para cambiar de prepectiva")]
    public KeyCode keyCode = KeyCode.Q;
    public JugadorMovimiento playerScript;

    void Start()
    {
        // 1. Forzamos el inicio en tercera persona
        ChangePerspective(true);
        
        // 2. Configuramos la distancia inicial
        // En lugar del promedio, usamos la variable initialDistance
        defaultDistance = initialDistance; 
        newDistance = initialDistance;
        
        Cursor.lockState = CursorLockMode.Locked;
        camera = GetComponent<Camera>();
        CalculateNearPlaneSize();
    }

    void ChangePerspective(bool ThirdPerson)
    {
        if (ThirdPerson)
        {
            follow = tpTarget;
            if (disablePlayerMesh && playerMesh != null) playerMesh.SetActive(true);
            tPerson = true;
            // Avisamos al jugador que está en 3ra persona
            if (playerScript != null) playerScript.estaEnPrimeraPersona = false;
        }
        else
        {
            follow = fpTarget;
            if (disablePlayerMesh && playerMesh != null) playerMesh.SetActive(false);
            tPerson = false;
            // Avisamos al jugador que está en 1ra persona
            if (playerScript != null) playerScript.estaEnPrimeraPersona = true;
        }
    }

    private void CalculateNearPlaneSize()
    {
        float height = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2) * camera.nearClipPlane;
        float width = height * camera.aspect;
        nearPlaneSize = new Vector2(width, height);
    }

    private Vector3[] GetCameraCollisionPoints(Vector3 direction)
    {
        Vector3 position = follow.position;
        Vector3 center = position + direction * (camera.nearClipPlane + 0.4f);
        Vector3 right = transform.right * nearPlaneSize.x;
        Vector3 up = transform.up * nearPlaneSize.y;
        return new Vector3[]
        {
            center - right + up,
            center + right + up,
            center - right - up,
            center + right - up
        };
    }

    void Update()
    {
        float hor = Input.GetAxis("Mouse X");
        if (hor != 0)
        {
            angle.x += hor * Mathf.Deg2Rad * sensitivity.x;
        }
        float ver = Input.GetAxis("Mouse Y");
        if (ver != 0)
        {
            angle.y -= ver * Mathf.Deg2Rad * sensitivity.y;
            angle.y = Mathf.Clamp(angle.y, -80 * Mathf.Deg2Rad, 80 * Mathf.Deg2Rad);
        }

        if (tPerson)
        {
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (scrollDelta > 0)
            {
                newDistance += 0.1f *(Time.deltaTime * zoomVelocity);
            }
            else if (scrollDelta < 0)
            {
                newDistance -= 0.1f * (Time.deltaTime * zoomVelocity);
            }
            newDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
            defaultDistance = Mathf.Lerp(defaultDistance, newDistance, zoomSmooth);
        }
        else
        {
            defaultDistance = 0f; 
        }

        if (Input.GetKeyDown(keyCode))
        {
            ChangePerspective(!tPerson);
        }
    }

    void LateUpdate()
    {
        if (follow == null) return;

        float x = Mathf.Sin(angle.x) * Mathf.Cos(angle.y);
        float y = Mathf.Sin(angle.y);
        float z = Mathf.Cos(angle.x) * Mathf.Cos(angle.y);
        
        Vector3 direction = new Vector3(x, y, z);

        if (tPerson)
        {
            RaycastHit hit;
            float distance = defaultDistance;
            Vector3[] points = GetCameraCollisionPoints(direction);

            foreach (Vector3 point in points)
            {
                if (Physics.Raycast(point, direction, out hit, defaultDistance))
                {
                    distance = Mathf.Min((hit.point - follow.position).magnitude, distance);
                }
            }

            transform.position = follow.position + direction * distance;
            transform.rotation = Quaternion.LookRotation(follow.position - transform.position);
        }
        else
        {
            transform.position = follow.position;
            transform.rotation = Quaternion.LookRotation(-direction);
            // IMPORTANTE: En 1ra persona, la cámara manda. Rotamos al cuerpo del jugador.
            // Asumiendo que 'follow' (el fpTarget) es hijo del jugador.
            if (follow.parent != null)
            {
                // Rotamos el cuerpo del jugador en Y para que coincida con la vista
                follow.parent.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            }
        }
    }
}
using UnityEngine;

public class JugadorMovimiento : MonoBehaviour
{
    public float speed = 5f;
    public Animator animator;
    
    [Header("Referencias")]
    public Transform cameraTransform; // Arrastra aquí la Main Camera
    public bool estaEnPrimeraPersona = false; // El script de la cámara actualizará esto

    void Update()
    {
        // 1. Obtener Inputs
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // 2. Calcular dirección relativa a la cámara
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Aplanamos los vectores (ignorar eje Y) para que el personaje no vuele
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Dirección final del movimiento
        Vector3 moveDirection = (forward * z + right * x).normalized;

        // 3. Mover al personaje
        if (moveDirection.magnitude >= 0.1f)
        {
            transform.position += moveDirection * speed * Time.deltaTime;

            // 4. Rotar el personaje
            // Si estamos en 1ra persona, el cuerpo siempre mira hacia donde mira la cámara
            // Si estamos en 3ra persona, el personaje gira hacia donde camina
            if (estaEnPrimeraPersona)
            {
                // En 1ra persona, la cámara controla la rotación (lo hace el script CamaraSigue)
                // Pero aseguramos que el cuerpo esté alineado con la cámara
                transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
            }
            else
            {
                // En 3ra persona, el personaje gira suavemente hacia la dirección de movimiento
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }

        // 5. Animaciones (Usamos la magnitud del movimiento para saber si camina)
        // Nota: Tu animator usaba X y Z. Si el personaje se mueve en diagonal, 
        // los valores pueden ser raros. Lo ideal es usar un Blend Tree 2D.
        // Aquí mantengo tu lógica original pero normalizada.
        animator.SetFloat("Horizontal", x);
        animator.SetFloat("Vertical", z);
    }
}
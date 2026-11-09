using UnityEngine;


public class JugadorMovimiento : MonoBehaviour
{
    public float speed = 5f;
    public Animator animator;

  
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(x, 0, z);
        transform.Translate(direction * speed * Time.deltaTime);

        //ANIMACIONES
        animator.SetFloat("Horizontal", x);
        animator.SetFloat("Vertical", z);
    }
}

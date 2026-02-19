using UnityEngine;
using UnityEngine.InputSystem;

public class MovLadron : MonoBehaviour
{
    public float velocidad = 5f;

    private Rigidbody rb;
    private Vector2 inputMovimiento;

    private ControlesJugador controles;

    void Awake()
    {
        controles = new ControlesJugador();

        controles.Jugador.Mover.performed += ctx =>
        {
            inputMovimiento = ctx.ReadValue<Vector2>();
        };

        controles.Jugador.Mover.canceled += ctx =>
        {
            inputMovimiento = Vector2.zero;
        };
    }

    void OnEnable()
    {
        controles.Enable();
    }

    void OnDisable()
    {
        controles.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 movimiento = new Vector3(inputMovimiento.x, 0, inputMovimiento.y);
        rb.MovePosition(rb.position + movimiento * velocidad * Time.fixedDeltaTime);
    }
}

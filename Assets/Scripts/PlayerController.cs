using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    public InputAction movementAction;

    private Vector2 MoveDirection;
    
    private void OnEnable()
    {
        movementAction.Enable();
    }

    private void OnDisable()
    {
        movementAction.Disable();
    }

    void Update()
    {
        MoveDirection = movementAction.ReadValue<Vector2>();
    }
    private void FixedUpdate()
    {
        Vector3 displacement = new Vector3(MoveDirection.x * moveSpeed, MoveDirection.y * moveSpeed, 0f);
        transform.position += displacement * Time.deltaTime;
    }
}

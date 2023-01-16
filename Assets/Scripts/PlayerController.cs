using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    public InputAction movementAction;

    [SerializeField, Header("Multiplayer")]
    private Alteruna.Avatar avatar;

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
        if (avatar.IsMe)
        {
            MoveDirection = movementAction.ReadValue<Vector2>();
        }
        
    }
    private void FixedUpdate()
    {
        Vector3 displacement = new Vector3(MoveDirection.x * moveSpeed, MoveDirection.y * moveSpeed, 0f);
        transform.position += displacement * Time.deltaTime;
    }
}

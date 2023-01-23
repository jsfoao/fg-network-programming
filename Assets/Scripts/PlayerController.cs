using Alteruna;
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
        if (avatar.Possessor == Lobby.Instance.Multiplayer.Me)
        {
            MoveDirection = movementAction.ReadValue<Vector2>();
        }
        
    }
    private void FixedUpdate()
    {
        transform.position += transform.right * (MoveDirection.x * moveSpeed * Time.deltaTime);
    }
}

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

    private Player player;

    private void OnEnable()
    {
        player = GetComponent<Player>();
        movementAction.Enable();
    }

    private void OnDisable()
    {
        movementAction.Disable();
    }

    void Update()
    {
        if (avatar.Possessor == Lobby.Instance.Multiplayer.Me && player.Enabled)
        {
            MoveDirection = movementAction.ReadValue<Vector2>();
        }
        
    }
    private void FixedUpdate()
    {
        transform.position += transform.right * (MoveDirection.x * moveSpeed * Time.deltaTime);
    }
}

using Alteruna;
using Alteruna.Trinity;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour
{

    [SerializeField] private int health = 5;
    private int currentHealth;

    public delegate void PlayerDeath(ushort id);
    public static event PlayerDeath OnDeath;

    public Multiplayer Multiplayer { get; set; }


    private void Start()
    {
        Multiplayer = Lobby.Instance.Multiplayer;
        Multiplayer.RegisterRemoteProcedure("DecrementHealth", Decrement_Health);
        Lobby.Instance.OnStartMatch.AddListener(EnablePlayer);
    }

    public void DecrementHealth(Player player)
    {
        if (!Lobby.Instance.IsAdmin()) return;
        
        if (currentHealth > 0)
        {
            ushort user = player.Avatar.Possessor.Index;
            Debug.Log("decrementing health");
            currentHealth--;
            ProcedureParameters parameters = new ProcedureParameters();
            parameters.Set("updatedHealth", currentHealth);
            parameters.Set("User", user);
            Multiplayer.InvokeRemoteProcedure("DecrementHealth", UserId.All, parameters);
            
            if (currentHealth <= 0)
            {
                DisablePlayer();
                OnDeath?.Invoke(user);
            }
        }
        
    }

    private void EnablePlayer()
    {
        currentHealth = health;
        GetComponent<Player>().Enabled = true;
        GetComponent<Renderer>().enabled = true;
        GetComponent<Collider>().enabled = true;
    }
    
    public void DisablePlayer()
    {
        GetComponent<Player>().Enabled = false;
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }
    
    private void Decrement_Health(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
       currentHealth = parameters.Get("updatedHealth", 0);
    }

}

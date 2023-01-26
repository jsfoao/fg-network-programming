using Alteruna;
using Alteruna.Trinity;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour
{

    [SerializeField] private int health = 5;

    public delegate void PlayerDeath();
    public static event PlayerDeath OnDeath;

    public Multiplayer Multiplayer { get; set; }


    private void Start()
    {
        Multiplayer = Lobby.Instance.Multiplayer;
        Multiplayer.RegisterRemoteProcedure("DecrementHealth", Decrement_Health);
        Lobby.Instance.OnStartMatch.AddListener(EnablePlayer);
    }

    public void DecrementHealth()
    {
        if (health > 0)
        {
            health--;
            ProcedureParameters parameters = new ProcedureParameters();
            parameters.Set("updatedHealth", health);
            parameters.Set("User", Multiplayer.Me.Index);
            Multiplayer.InvokeRemoteProcedure("DecrementHealth", UserId.All, parameters);
            if (health <= 0)
            {
                DisablePlayer();
                OnDeath?.Invoke();
            }
        }
        
    }

    private void EnablePlayer()
    {
        GetComponent<Player>().Enabled = true;
        GetComponent<Renderer>().enabled = true;
        GetComponent<Collider>().enabled = true;
    }
    
    private void DisablePlayer()
    {
        GetComponent<Player>().Enabled = false;
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }
    
    private void Decrement_Health(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
       health = parameters.Get("updatedHealth", 0);
       ushort user = parameters.Get("User", (ushort)0);
    }

}

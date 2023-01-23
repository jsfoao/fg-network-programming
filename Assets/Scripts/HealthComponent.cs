using Alteruna;
using Alteruna.Trinity;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{

    [SerializeField] private int health = 5;

    public Multiplayer Multiplayer { get; set; }

    private void Start()
    {
        Multiplayer = Lobby.Instance.Multiplayer;
        Multiplayer.RegisterRemoteProcedure("DecrementHealth", Decrement_Health);
    }

    private void DecrementHealth()
    {
        health--;
        ProcedureParameters parameters = new ProcedureParameters();
        parameters.Set("updatedHealth", health);
        Multiplayer.InvokeRemoteProcedure("DecrementHealth", UserId.All, parameters);
    }
    private void Decrement_Health(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
    {
       health = parameters.Get("updatedHealth", 0);

    }

}

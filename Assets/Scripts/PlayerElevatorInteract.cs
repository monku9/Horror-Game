using UnityEngine;
using Mirror;

public class PlayerElevatorInteract : NetworkBehaviour
{
    [SerializeField] private float   interactRange = 3f;
    [SerializeField] private KeyCode activateKey   = KeyCode.E;

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (!Input.GetKeyDown(activateKey)) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);

        foreach (var hit in hits)
        {
            ElevatorController elevator = hit.GetComponentInParent<ElevatorController>();
            if (elevator != null)
            {
                CmdActivateElevator(elevator.netIdentity);
                break;
            }
        }
    }

    [Command]
    private void CmdActivateElevator(NetworkIdentity elevatorIdentity)
    {
        elevatorIdentity.GetComponent<ElevatorController>()?.Activate();
    }
}
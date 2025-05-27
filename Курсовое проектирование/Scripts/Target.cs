using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

public class Target : MonoBehaviourPun, IDamageable
{
    public float health = 100f;
    public List<Transform> positions;
    private bool isShielded = false;
    [SerializeField] private float shieldDuration = 10f;
    [SerializeField] private Material shieldedMaterial;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private ParticleSystem shieldParticles;

    private void Start()
    {
        photonView.RPC("RPC_ActivateShield", RpcTarget.All, false);
        if (photonView == null)
        {
            Debug.LogError("PhotonView is missing on Target!");
        }
        if (shieldParticles == null)
        {
            Debug.LogWarning("ShieldParticles not assigned on Target!");
        }
    }

    public void TakeDamage(float damage, Player attacker)
    {
        if (attacker == null)
        {
            Debug.LogWarning("TakeDamage called with null attacker!");
            return;
        }
        Debug.Log($"TakeDamage called: damage={damage}, attacker={attacker.NickName}, target={gameObject.name}");
        photonView.RPC("RPC_TakeDamage", RpcTarget.MasterClient, damage, attacker.ActorNumber);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage, int attackerActorNumber)
    {
        if (isShielded)
        {
            Debug.Log($"Damage ignored due to shield on {gameObject.name}");
            return;
        }

        Debug.Log($"RPC_TakeDamage: damage={damage}, attackerActorNumber={attackerActorNumber}, currentHealth={health}");

        health -= damage;

        if (health <= 0)
        {
            photonView.RPC("RPC_RespawnTarget", RpcTarget.AllBuffered);
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                Debug.Log($"Registering kill: killer={attackerActorNumber}, victim={photonView.Owner.ActorNumber}");
                gameManager.RegisterKill(attackerActorNumber, photonView.Owner.ActorNumber);
            }
            else
            {
                Debug.LogWarning("GameManager not found!");
            }
            photonView.RPC("RPC_RespawnTarget", RpcTarget.AllBuffered);
        }

        photonView.RPC("RPC_UpdateHealth", RpcTarget.AllBuffered, health);
    }

    [PunRPC]
    void RPC_UpdateHealth(float newHealth)
    {
        health = newHealth;
        Debug.Log($"Health updated to {health} on {gameObject.name}");
    }

    [PunRPC]
    void RPC_RespawnTarget()
    {
        if (positions == null || positions.Count == 0)
        {
            Debug.LogWarning("No positions defined for target respawn!");
            return;
        }

        Transform newPosition = positions[Random.Range(0, positions.Count)];
        health = 100f;
        isShielded = true;

        transform.position = newPosition.position;
        transform.rotation = newPosition.rotation;

        photonView.RPC("RPC_UpdateHealth", RpcTarget.All, health);
        photonView.RPC("RPC_ActivateShield", RpcTarget.All, true);

        if (photonView.IsMine)
        {
            StartCoroutine(ShieldTimer());
        }

        Debug.Log($"Respawned at {newPosition.position}, shield active");
    }

    [PunRPC]
    void RPC_ActivateShield(bool active)
    {
        isShielded = active;
        if (shieldParticles != null)
        {
            if (active) shieldParticles.Play();
            else shieldParticles.Stop();
        }
        Debug.Log($"Shield {(active ? "activated" : "deactivated")} on {gameObject.name}");
    }

    private IEnumerator ShieldTimer()
    {
        yield return new WaitForSeconds(shieldDuration);
        isShielded = false;
        photonView.RPC("RPC_ActivateShield", RpcTarget.All, false);
        Debug.Log($"Shield timer ended on {gameObject.name}");
    }
}
using UnityEngine;
using Photon.Pun;

public class SimpleMeleeWeapon : MonoBehaviourPunCallbacks
{
    [Header("Attack Settings")]
    public string weaponName;
    public float damage = 25f;
    public float attackRange = 1.5f;
    public float attackCooldown = 0.7f;
    public LayerMask enemyLayer;
    public bool isPicked = false;

    public GameObject model;

    public AudioClip swingClip;
    public AudioClip attackSound;

    private float lastAttackTime;
    private bool canAttack = true;
    private PhotonView parentPhotonView;

    private void Update()
    {
        parentPhotonView = GetComponentInParent<PhotonView>();
        // Debug.Log($"{!photonView.IsMine} and {!isPicked}");
        if (!photonView.IsMine || !isPicked) return;
       // Debug.Log($"{!photonView.IsMine} and {!isPicked} 2 2");
        if (Time.time - lastAttackTime > attackCooldown && !canAttack)
        {
            canAttack = true;
        }
        if (Input.GetButtonDown("Fire1"))
        {
            Attack();
        }
    }

    public void Attack()
    {
        if (!canAttack) return;

       // Debug.Log($"attack here");
        AudioSource.PlayClipAtPoint(swingClip, transform.position);
        photonView.RPC("RPC_Attack", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_Attack()
    {
        AudioSource.PlayClipAtPoint(swingClip, transform.position);
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        //Debug.Log($"{hitColliders.Length}");
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.gameObject != model) {
                if (TryDealDamage(hitCollider.gameObject))
                {
                    AudioSource.PlayClipAtPoint(attackSound, transform.position);
                }
            }
        }
        lastAttackTime = Time.time;
        canAttack = false;
    }

    private bool TryDealDamage(GameObject target)
    {
        //Debug.Log($"hfjkl;'kjhgvbnm,.lkjhb");
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage, parentPhotonView.Owner);
            Debug.Log($"Dealt {damage} damage to {target.name}");
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, attackRange);
    }
}
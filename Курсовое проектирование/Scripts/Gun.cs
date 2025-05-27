using UnityEngine;
using Photon.Pun;
using System.Collections;

public class Gun : MonoBehaviourPunCallbacks
{
    [Header("References")]
    public GunData gunData;
    private Transform cam;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;
    public bool isPicked = false;
    private float timeSinceLastShot;

    private PhotonView parentPhotonView; // PhotonView of the parent player
    private void Start()
    {
        parentPhotonView = GetComponentInParent<PhotonView>();
        gunData.currentAmmo = gunData.magSize;
    }

    private void Update()
    {
        //Debug.Log($"{photonView.IsMine} and {isPicked}");
        if (!photonView.IsMine || !isPicked) return;

        //Debug.Log($"{!photonView.IsMine} and {!isPicked} 22");
        timeSinceLastShot += Time.deltaTime;
        if (Input.GetMouseButton(0))
            Shoot();
        if (Input.GetKeyDown(reloadKey))
            StartReload();

        Debug.DrawRay(cam.position, cam.forward * gunData.maxDistance);
    }

    public void Picked(Transform camera)
    {
        Debug.Log("asfd");
        cam = camera;
        isPicked = true;
    }

    public void Unpicked()
    {
        cam = null;
        isPicked = false;
    }

    private void OnDisable() => gunData.reloading = false;

    public void StartReload()
    {
        if (!gunData.reloading && gameObject.activeSelf)
        {
            AudioSource.PlayClipAtPoint(reloadSound, transform.position);
            photonView.RPC("RPC_Reload", RpcTarget.All);
        }
    }

    [PunRPC]
    private IEnumerator RPC_Reload()
    {
        AudioSource.PlayClipAtPoint(reloadSound, transform.position);
        gunData.reloading = true;
        yield return new WaitForSeconds(gunData.reloadTime);
        gunData.currentAmmo = gunData.magSize;
        gunData.reloading = false;
    }

    private bool CanShoot() => !gunData.reloading && timeSinceLastShot > 1f / (gunData.fireRate / 60f);

    private void Shoot()
    {
        if (gunData.currentAmmo <= 0 || !CanShoot()) return;

        AudioSource.PlayClipAtPoint(audioClip, transform.position);
        photonView.RPC("RPC_Shoot", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_Shoot()
    {
        if (gunData.currentAmmo <= 0 || !isPicked || cam == null) return;

        AudioSource.PlayClipAtPoint(audioClip, transform.position);
        gunData.currentAmmo--;
        timeSinceLastShot = 0;

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hitInfo, gunData.maxDistance))
        {
            IDamageable damageable = hitInfo.transform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(gunData.damage, parentPhotonView.Owner);
                Debug.Log($"Dealt {gunData.damage} damage to {hitInfo.transform.name}");
            }
        }
    }
}
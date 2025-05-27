using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class SpawnPlayers : MonoBehaviour
{

    [SerializeField] private List<Transform> spawnPlaces = new List<Transform>();
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();
    private string weapon;
    public GameObject player;
    public float minX, minZ, maxX, maxZ;
    void Start()
    {
        IEnumerable<Transform> randomPlaces = spawnPlaces.OrderBy(x => Random.value).Take(1);
        IEnumerable<GameObject> randomWeapons = weapons.OrderBy(x => Random.value).Take(1);

        Vector3 randomPosition = randomPlaces.First().position;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("selectedWeapon", out object loaded_weapon))
        {
            weapon = loaded_weapon.ToString();
            Debug.Log($"Оружие: {weapon}");
        }
        else
        {
            weapon = randomWeapons.First().name;
            Debug.LogWarning("Сбой, выдача случайного оружия");
        }


        var c = PhotonNetwork.Instantiate(player.name.Replace("(Clone)", ""), randomPosition, Quaternion.identity);
        c.name = c.name.Replace("(Clone)", "");

        TMP_Text nicknameText = c.GetComponentInChildren<TMP_Text>();
        if (nicknameText != null)
        {
            nicknameText.text = PhotonNetwork.LocalPlayer.NickName;
        }
        else
        {
            Debug.LogWarning("Не найден компонент TMP_Text для отображения ника");
        }

        c.GetComponent<Target>().positions = spawnPlaces;

        GameObject instantiatedWeapon = PhotonNetwork.Instantiate(weapon, Vector3.zero, Quaternion.identity);
        instantiatedWeapon.transform.SetParent(c.transform.Find("Head/Camera/Hand"), false);

        instantiatedWeapon.GetComponent<WeaponSway>().Picked();
        if (instantiatedWeapon.GetComponent<SimpleMeleeWeapon>() != null) {
           // Debug.Log("1234567890");
            instantiatedWeapon.GetComponent<SimpleMeleeWeapon>().isPicked = true;
            instantiatedWeapon.GetComponent<SimpleMeleeWeapon>().model = c;
           // Debug.Log(instantiatedWeapon.GetComponent<SimpleMeleeWeapon>().isPicked);
        }
        else {
            //Debug.Log("xc,mnnxc,m,bxxm,n,");
            instantiatedWeapon.GetComponent<Gun>().Picked(c.transform.Find("Head/Camera"));
           // Debug.Log(instantiatedWeapon.GetComponent<Gun>().isPicked);
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPlaces.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (var place in spawnPlaces)
            {
                if (place != null)
                {
                    Gizmos.DrawWireSphere(place.position, 0.3f);
                    Gizmos.DrawLine(place.position, place.position + place.forward * 0.5f);
                }
            }
        }
    }
}

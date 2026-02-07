using UnityEngine;
using UnityEngine.UI;

public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] private Text ammoText;
    [SerializeField] private VRGun gun;

    [Header("Visuals")]
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color emptyColor = Color.red;

    private void Start()
    {
        if (gun == null) gun = GetComponentInParent<VRGun>();
    }

    private void Update()
    {
        if (gun == null || ammoText == null) return;

        int current = gun.AmmoCount;
        bool chambered = gun.HasChamberedRound;

        // Format: [Ammo] + [1 if chambered]
        ammoText.text = $"{current}{(chambered ? "+1" : "")}";

        // Color transition
        if (gun.HasMagazine)
        {
            float ratio = (float)current / 15f; // Assuming 15 is standard, could be dynamic
            ammoText.color = Color.Lerp(emptyColor, fullColor, ratio);
        }
        else
        {
            ammoText.color = emptyColor;
            ammoText.text = chambered ? "+1" : "EMPTY";
        }
    }
}

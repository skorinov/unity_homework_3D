using UnityEngine;
using TMPro;
using Player;

namespace UI
{
    /// <summary>
    /// UI for weapon pickup prompts
    /// </summary>
    public class PickupUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private WeaponOwner weaponOwner;
        [SerializeField] private TextMeshProUGUI pickupText;
        [SerializeField] private string interactKey = "[E]";
        
        private bool isShowing;
        
        private void Start()
        {
            pickupText.gameObject.SetActive(false);
        }
        
        private void Update()
        {
            UpdatePickupPrompt();
        }
        
        private void UpdatePickupPrompt()
        {
            if (!weaponOwner) return;
            
            bool shouldShow = weaponOwner.HighlightedWeapon != null;
            
            if (shouldShow != isShowing)
            {
                isShowing = shouldShow;
                pickupText.gameObject.SetActive(shouldShow);
                
                if (shouldShow && pickupText && weaponOwner.HighlightedWeapon)
                {
                    string weaponName = weaponOwner.HighlightedWeapon.WeaponComponent.WeaponName;
                    pickupText.text = $"{interactKey} Pick up {weaponName}";
                }
            }
        }
    }
}
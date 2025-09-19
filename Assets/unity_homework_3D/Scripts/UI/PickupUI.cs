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
        
        private bool _isShowing;
        
        private void Start()
        {
            if (!weaponOwner)
                weaponOwner = FindFirstObjectByType<WeaponOwner>();
            
            if (pickupText)
                pickupText.gameObject.SetActive(false);
        }
        
        private void Update()
        {
            if (weaponOwner)
                UpdatePickupPrompt();
        }
        
        private void UpdatePickupPrompt()
        {
            bool shouldShow = weaponOwner.HighlightedWeapon;
            
            if (shouldShow != _isShowing)
            {
                _isShowing = shouldShow;
                
                if (pickupText)
                {
                    pickupText.gameObject.SetActive(shouldShow);
                    
                    if (shouldShow && weaponOwner.HighlightedWeapon?.WeaponComponent)
                    {
                        string weaponName = weaponOwner.HighlightedWeapon.WeaponComponent.WeaponName;
                        pickupText.text = $"{interactKey} Pick up {weaponName}";
                    }
                }
            }
        }
    }
}
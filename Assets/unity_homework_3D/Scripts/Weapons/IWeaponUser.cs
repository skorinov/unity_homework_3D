using UnityEngine;

namespace Weapons
{
    /// <summary>
    /// Interface for entities that can use weapons (players, AI)
    /// </summary>
    public interface IWeaponUser
    {
        Transform FirePoint { get; }
        Camera UserCamera { get; }
        bool CanUseWeapon { get; }
        LayerMask TargetLayers { get; }
        ITargetProvider TargetProvider { get; }
        bool HasInfiniteAmmo { get; }
        
        void OnWeaponFired();
        void OnWeaponReloaded();
    }
}
using UnityEngine;

namespace Weapons
{
    /// <summary>
    /// Interface for providing target information to weapons
    /// </summary>
    public interface ITargetProvider
    {
        Transform GetTarget();
        Vector3 GetAimPoint();
        bool HasValidTarget();
    }
}
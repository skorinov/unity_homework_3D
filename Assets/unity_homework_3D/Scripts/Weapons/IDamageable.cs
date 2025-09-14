namespace Weapons
{
    /// <summary>
    /// Interface for objects that can take damage
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage);
        bool IsAlive { get; }
    }
}
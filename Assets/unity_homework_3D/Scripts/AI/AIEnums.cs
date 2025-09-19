namespace AI
{
    public enum PatrolType
    {
        Loop,       // Cycle through patrol points
        PingPong,   // Back and forth movement
        Random      // Random point selection
    }
    
    public enum EnemyState
    {
        Patrolling, // Normal patrol behavior
        Chasing,    // Pursuing player
        Attacking,  // Engaging player in combat
        Searching,  // Looking for lost player
        Dead        // Eliminated
    }
}
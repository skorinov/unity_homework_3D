# 3D FPS Shooter

Unity-based first-person shooter game with AI enemies, weapon systems, and object pooling.

## Features

### Core Gameplay
- **First-Person Controller** - Movement, jumping, sprinting with physics-based controls
- **Weapon System** - Universal weapon framework supporting single-fire, full-auto, and shotgun types
- **Health System** - Player health with regeneration and visual damage feedback
- **Weapon Pickup** - Interactive weapon collection system with highlight effects

### AI System
- **Enemy Controller** - Patrol, chase, attack, and search behaviors
- **Dynamic Spawning** - Automatic enemy spawning with configurable parameters
- **AI Weapons** - Burst-fire weapon controller for enemies
- **Line of Sight** - Realistic enemy detection and targeting

### Visual Effects
- **Bullet Trails** - Animated projectile visualization
- **Impact Effects** - Particle effects and decals on surfaces
- **Muzzle Flash** - Weapon firing visual feedback
- **Object Pooling** - Performance-optimized effect management

### Audio System
- **3D Audio** - Positional weapon sounds and impact effects
- **Settings** - Volume controls and audio preferences
- **Background Music** - Persistent audio across game sessions

### UI System
- **Game HUD** - Health bar, crosshair, and pickup prompts
- **Pause Menu** - Game state management with restart/quit options
- **Settings Menu** - Audio and graphics configuration
- **Input System** - Modern Unity Input System integration

## Technical Architecture

### Managers
- **GameManager** - Persistent singleton handling game state and scene transitions
- **ObjectPool** - Performance-optimized object reuse system
- **AudioManager** - Centralized audio control with mixer integration
- **DecalManager** - Surface decal lifecycle management

### Weapon Framework
- **IWeaponUser** - Interface for entities that can use weapons (Player/AI)
- **ITargetProvider** - Interface for weapon targeting systems
- **IDamageable** - Interface for objects that can take damage
- **Universal Weapon** - Configurable weapon system supporting multiple fire modes

### AI Framework
- **State Machine** - Enum-based AI states (Patrolling, Chasing, Attacking, Searching)
- **Patrol System** - Loop, PingPong, and Random patrol patterns
- **Detection System** - Field of view and line of sight calculations

## Performance Optimizations

### Object Pooling
All frequently spawned objects use pooling:
- Bullets and projectiles
- Visual effects (muzzle flash, impacts)
- Decals and particle systems
- Audio sources for 3D sound

### Caching
- Animation parameter hashes
- Screen center coordinates
- Layer masks and collision queries
- Material references for highlighting

### Memory Management
- Automatic cleanup on game restart
- Proper event unsubscription
- Material destruction for highlights
- Coroutine management

## Setup Instructions

### Required Components
1. **Unity 6000.0.45f** with Input System package
2. **NavMesh** baked for AI pathfinding
3. **Audio Mixer** with "Music" and "SFX" groups
4. **Layer Setup**:
    - Player layer
    - Enemy layer
    - Environment/Ground/Wall layers

### Scene Setup
1. Add **GameManager** prefab to scene
2. Configure **ObjectPool** with required prefabs:
    - Bullet
    - BulletDecal
    - BulletTrail
    - ImpactEffect
    - MuzzleFlash
3. Set up **AudioManager** with audio clips
4. Place **EnemySpawner** with spawn points
5. Configure **UIManager** with canvas references

### Input Mapping
- **WASD** - Movement
- **Mouse** - Look around
- **Space** - Jump
- **Shift** - Sprint
- **Mouse1** - Fire weapon
- **R** - Reload
- **E** - Interact/Pickup
- **ESC** - Pause menu

## Configuration

### Weapon Data
```csharp
WeaponData weaponConfig = new WeaponData {
    weaponName = "Assault Rifle",
    damage = 25f,
    fireRate = 600f,
    range = 100f,
    maxAmmo = 30,
    reloadTime = 2f,
    bulletSpeed = 50f,
    spread = 0.02f,
    isFullAuto = true
};
```

### Game Constants
All game parameters are centralized in `GameConstants.cs`:
- Weapon settings
- Movement parameters
- AI behavior constants
- Performance thresholds

## Code Architecture

### Interfaces
- **IPooledObject** - Object pooling lifecycle
- **IWeaponUser** - Weapon system integration
- **ITargetProvider** - AI targeting system
- **IDamageable** - Damage system

### Events
- Game state changes (pause/resume/restart)
- Health changes and player death
- Weapon firing and reloading

### Namespaces
- `Weapons` - Weapon system components
- `Player` - Player-specific components
- `AI` - Enemy AI system
- `Managers` - Core game managers
- `UI` - User interface components
- `Constants` - Configuration constants

## Performance Targets

- **60 FPS** at 1080p with multiple enemies
- **Object pooling** prevents garbage collection spikes
- **Efficient raycasting** with layer masks
- **Batched audio** for multiple simultaneous sounds

## Development Notes

- Uses Unity's new Input System for modern input handling
- Implements proper singleton patterns for managers
- Event-driven architecture for loose coupling
- Interface-based design for extensibility
- Performance-first approach with caching and pooling
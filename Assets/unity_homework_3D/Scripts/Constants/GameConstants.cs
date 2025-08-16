namespace Constants
{
    public static class GameConstants
    {
        public static class Bullets
        {
            public const float DEFAULT_LIFETIME = 5f;
            public const int DEFAULT_MAX_BOUNCES = 2;
            public const float DEFAULT_BOUNCE_FORCE = 0.5f;
            public const float DEFAULT_SPEED = 50f;
            public const float RAYCAST_DISTANCE_OFFSET = 0.1f;
            public const float HIT_DISTANCE_THRESHOLD = 0.2f;
            public const float STOP_DELAY = 1f;
            public const float DECAL_SIZE_MIN = 0.08f;
            public const float DECAL_SIZE_MAX = 0.12f;
            public const float DECAL_POSITION_OFFSET = 0.01f;
            public const float IMPACT_EFFECT_OFFSET = 0.05f;
            public const float IMPACT_EFFECT_LIFETIME = 3f;
            public const float ANGULAR_VELOCITY_MULTIPLIER = 5f;
        }
        
        public static class Weapons
        {
            public const float DEFAULT_DAMAGE = 25f;
            public const float DEFAULT_FIRE_RATE = 600f;
            public const float DEFAULT_RANGE = 100f;
            public const int DEFAULT_MAX_AMMO = 30;
            public const float DEFAULT_RELOAD_TIME = 2f;
            public const float DEFAULT_SPREAD = 0.02f;
            public const float SCREEN_CENTER_X = 0.5f;
            public const float SCREEN_CENTER_Y = 0.5f;
            public const float MUZZLE_FLASH_LIFETIME = 0.5f;
        }
        
        public static class Collectibles
        {
            public const float BOB_SPEED = 1f;
            public const float BOB_HEIGHT = 0.05f;
            public const float HEIGHT_OFFSET = 0.3f;
            public const float HIGHLIGHT_INTENSITY = 1.5f;
        }
        
        public static class Trails
        {
            public const float DEFAULT_SPEED = 500f;
            public const float DEFAULT_LIFETIME = 0.25f;
            public const float START_WIDTH = 0.02f;
            public const float END_WIDTH = 0.01f;
        }
        
        public static class Decals
        {
            public const float DEFAULT_FADE_TIME = 2f;
            public const float FADE_CHECK_INTERVAL = 0.3f;
        }
        
        public static class Pools
        {
            public const string BULLET = "Bullet";
            public const string BULLET_DECAL = "BulletDecal";
            public const string BULLET_TRAIL = "BulletTrail";
            public const string IMPACT_EFFECT = "ImpactEffect";
        }
        
        public static class Layers
        {
            public const string PLAYER = "Player";
        }
        
        public static class Movement
        {
            public const float SLOPE_SLIDE_MULTIPLIER = 0.5f;
            public const float VELOCITY_RESET = -2f;
            public const float GROUND_CHECK_OFFSET = 0.1f;
            public const float GROUND_CHECK_SIDE_OFFSET = 0.3f;
            public const float GROUND_CHECK_SIDE_MULTIPLIER = 0.8f;
            public const float MOUSE_SENSITIVITY_MULTIPLIER = 0.01f;
            public const float LOOK_INPUT_THRESHOLD = 0.0001f;
            public const float LANDING_TIME_THRESHOLD = 0.3f;
            public const float MOVEMENT_INPUT_THRESHOLD = 0.1f;
        }
    }
}
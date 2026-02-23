/// <summary>
/// Central location for all magic strings and constant numbers in SHIFT.
/// Never hardcode strings or numbers directly — reference this file instead.
/// </summary>
public static class Constants
{
    // ─── Tags ────────────────────────────────────────────────────────────────────
    public const string TAG_PLAYER       = "Player";
    public const string TAG_SHIFT_OBJECT = "ShiftObject";
    public const string TAG_GOAL_ZONE    = "GoalZone";

    // ─── Layers ──────────────────────────────────────────────────────────────────
    public const string LAYER_INTERACTABLE = "Interactable";
    public const string LAYER_SURFACE      = "Surface";

    // ─── Scenes ──────────────────────────────────────────────────────────────────
    public const string SCENE_BOOTSTRAP   = "Bootstrap";
    public const string SCENE_MAIN_MENU   = "MainMenu";
    public const string SCENE_GAME        = "Game";

    // ─── Daily Seed ──────────────────────────────────────────────────────────────
    public const string SEED_PREFIX        = "SHIFT_";        // "SHIFT_YYYYMMDD_UTC"
    public const string SEED_DATE_FORMAT   = "yyyyMMdd";

    // ─── PlayerPrefs Keys ────────────────────────────────────────────────────────
    public const string PREF_SOUND_VOLUME   = "SoundVolume";
    public const string PREF_HAPTICS        = "Haptics";
    public const string PREF_LAST_STREAK    = "LastStreak";
    public const string PREF_STREAK_DATE    = "StreakDate";
    public const string PREF_SHARD_COUNT    = "ShardCount";

    // ─── Gameplay ────────────────────────────────────────────────────────────────
    public const float DEFAULT_PICKUP_RANGE = 5f;
    public const float DEFAULT_HOLD_DIST    = 2f;
    public const float MIN_SCALE_MULT       = 0.1f;
    public const float MAX_SCALE_MULT       = 5f;

    // ─── Rewards ─────────────────────────────────────────────────────────────────
    public const int SHARDS_DAILY_WIN       = 10;
    public const int SHARDS_BRONZE_CHEST    = 25;
    public const int SHARDS_SILVER_CHEST    = 75;
    public const int SHARDS_GOLD_CHEST      = 200;
    public const int STREAK_SILVER_CHEST    = 7;   // Days needed for silver chest
    public const int STREAK_GOLD_CHEST      = 30;

    // ─── IAP Product IDs ─────────────────────────────────────────────────────────
    public const string IAP_SHARDS_SMALL    = "com.polabathina.shift.shards_small";
    public const string IAP_SHARDS_MEDIUM   = "com.polabathina.shift.shards_medium";
    public const string IAP_SHARDS_LARGE    = "com.polabathina.shift.shards_large";
    public const string IAP_CREATOR_PASS    = "com.polabathina.shift.creator_pass";

    // ─── Firebase ────────────────────────────────────────────────────────────────
    public const string FS_COLLECTION_USERS      = "users";
    public const string FS_COLLECTION_LEADERBOARD = "leaderboard";
    public const string FS_COLLECTION_DAILY_SEED  = "daily_seeds";
    public const string FS_FIELD_SHARD_COUNT      = "shardCount";
    public const string FS_FIELD_STREAK           = "streak";
    public const string FS_FIELD_LAST_PLAY        = "lastPlayDate";

    // ─── Remote Config Keys ──────────────────────────────────────────────────────
    public const string RC_WEEKEND_BONUS        = "weekend_bonus_active";
    public const string RC_CREATOR_PASS_PRICE   = "creator_pass_price";
    public const string RC_MAX_ARCHIVE_FREE     = "max_archive_free";
    public const string RC_NEW_OBJECTS_ENABLED  = "new_objects_enabled";
}

// Stores persistent player choices that need to survive scene transitions.
public static class GameState
{
    public enum CreatureChoice { None, Dragon, Siren, Unicorn }

    // Set when the player picks a creature-specific option in Level 2.
    // Defaults to None so DragonCipherController can fall back to its inspector value.
    public static CreatureChoice Level2CreatureChoice { get; set; } = CreatureChoice.None;
}

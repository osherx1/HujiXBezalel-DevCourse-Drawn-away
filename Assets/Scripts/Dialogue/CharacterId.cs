using System;

/// <summary>
/// Add your NPC/player character identifiers here.
/// Used to pick which speech bubble/dialogue belongs to which character.
/// </summary>
[Serializable]
public enum CharacterId
{
    None = 0,

    // Example IDs (rename/add as needed)
    Player = 1,
    Old_Man = 10,
    Illana = 11,
    NPC_3 = 12,
}


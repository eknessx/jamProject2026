using System.Collections.Generic;
using UnityEngine;

namespace Environment
{
    /// <summary>What a room is for in gameplay. Assigned by the DungeonGenerator after the layout is built.</summary>
    public enum RoomType
    {
        Start,
        Combat,
        Shop,
        Treasure,
        Boss
    }

    public enum RoomPlacement
    {
        Random,
        /// <summary>As far from the start as possible.</summary>
        Farthest,
        /// <summary>As close to the start as possible.</summary>
        Closest
    }

    /// <summary>Where one room type is allowed to show up and how many of it the level gets.</summary>
    [System.Serializable]
    public class RoomTypeRule
    {
        public RoomType type;

        [Min(0)] public int minCount = 1;
        [Min(0)] public int maxCount = 1;

        [Tooltip("Only rooms with a single connection (end of a branch).")]
        public bool deadEndOnly = true;

        [Tooltip("Rooms away from the start. The start room is depth 0.")]
        [Min(0)] public int minDepth;
        [Tooltip("0 = no limit.")]
        [Min(0)] public int maxDepth;

        public RoomPlacement placement = RoomPlacement.Random;

        [Tooltip("Room types this one must not connect to directly.")]
        public List<RoomType> notNextTo = new List<RoomType>();

        public bool AllowsDepth(int depth) => depth >= minDepth && (maxDepth == 0 || depth <= maxDepth);
    }

    /// <summary>A prefab spawned inside every room of a given type (enemy spawner, shopkeeper, chest, boss...).</summary>
    [System.Serializable]
    public class RoomTypeContent
    {
        public RoomType type;
        public GameObject prefab;
    }
}

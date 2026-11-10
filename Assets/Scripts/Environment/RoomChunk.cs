using System.Collections.Generic;
using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Root component of a handmade room prefab. Defines the room's footprint (used to stop
    /// rooms overlapping) and exposes the Doorway children the generator connects through.
    /// Keep the root unrotated and unscaled.
    /// </summary>
    public class RoomChunk : MonoBehaviour
    {
        [Tooltip("Footprint centre, relative to this object's position.")]
        [SerializeField] private Vector2 boundsCenter;
        [Tooltip("Footprint size in world units. Should match the room's outer walls.")]
        [SerializeField] private Vector2 boundsSize = new Vector2(16f, 10f);

        [Header("Room Types")]
        [Tooltip("Types this room shape is unsuitable for, e.g. Boss for a narrow vent.")]
        [SerializeField] private List<RoomType> excludedTypes = new List<RoomType>();
        [Tooltip("Where type content (enemies, shopkeeper...) spawns. Defaults to the footprint centre.")]
        [SerializeField] private Transform contentAnchor;

        private Doorway[] doorways;

        /// <summary>Set by the DungeonGenerator. Other systems can read it to decide what happens here.</summary>
        public RoomType Type { get; private set; }

        public Doorway[] Doorways
        {
            get
            {
                if (doorways == null) doorways = GetComponentsInChildren<Doorway>(true);
                return doorways;
            }
        }

        public Vector3 ContentPosition =>
            contentAnchor != null ? contentAnchor.position : transform.position + (Vector3)boundsCenter;

        public bool AllowsType(RoomType type) => !excludedTypes.Contains(type);

        public bool HasType { get; private set; }

        public void SetType(RoomType type)
        {
            Type = type;
            HasType = true;
        }

        /// <summary>Footprint as if the room's root were at <paramref name="rootPosition"/>.</summary>
        public Rect GetRectAt(Vector2 rootPosition)
        {
            Vector2 center = rootPosition + boundsCenter;
            return new Rect(center - boundsSize * 0.5f, boundsSize);
        }

        /// <summary>A doorway's position relative to the room root. Works on prefab assets too.</summary>
        public Vector2 GetLocalDoorPosition(Doorway door)
        {
            return door.transform.position - transform.position;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = HasType ? GizmoColor(Type) : new Color(0.3f, 0.7f, 1f, 0.8f);
            Gizmos.DrawWireCube(transform.position + (Vector3)boundsCenter, boundsSize);
        }

        private static Color GizmoColor(RoomType type)
        {
            switch (type)
            {
                case RoomType.Start: return Color.green;
                case RoomType.Shop: return Color.cyan;
                case RoomType.Treasure: return Color.yellow;
                case RoomType.Boss: return Color.magenta;
                default: return new Color(1f, 0.4f, 0.3f);
            }
        }
    }
}

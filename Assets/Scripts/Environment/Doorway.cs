using UnityEngine;

namespace Environment
{
    public enum DoorDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// A connection point on the edge of a room chunk. Place it exactly on the room's
    /// boundary, in the middle of the opening, and set which way it faces.
    /// </summary>
    public class Doorway : MonoBehaviour
    {
        [SerializeField] private DoorDirection direction;

        [Header("Optional Visuals")]
        [Tooltip("Enabled when this doorway connects to another room (e.g. an open passage).")]
        [SerializeField] private GameObject openVisual;
        [Tooltip("Enabled when nothing connects here (e.g. wall tiles plugging the gap).")]
        [SerializeField] private GameObject closedVisual;

        public DoorDirection Direction => direction;
        public bool IsConnected { get; private set; }

        public static DoorDirection Opposite(DoorDirection dir)
        {
            switch (dir)
            {
                case DoorDirection.Up: return DoorDirection.Down;
                case DoorDirection.Down: return DoorDirection.Up;
                case DoorDirection.Left: return DoorDirection.Right;
                default: return DoorDirection.Left;
            }
        }

        public void SetConnected(bool connected)
        {
            IsConnected = connected;
            if (openVisual != null) openVisual.SetActive(connected);
            if (closedVisual != null) closedVisual.SetActive(!connected);
        }

        private void OnDrawGizmos()
        {
            Vector3 dir;
            switch (direction)
            {
                case DoorDirection.Up: dir = Vector3.up; break;
                case DoorDirection.Down: dir = Vector3.down; break;
                case DoorDirection.Left: dir = Vector3.left; break;
                default: dir = Vector3.right; break;
            }

            Gizmos.color = IsConnected ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + dir * 0.75f);
        }
    }
}

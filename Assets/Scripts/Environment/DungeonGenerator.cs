using System.Collections.Generic;
using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Builds a level in two steps:
    /// 1. Layout: stitches handmade RoomChunk prefabs together through their doorways.
    /// 2. Room types: hands out Boss, Shop, Treasure etc. by the type rules (in list order),
    ///    and every room left over becomes the fill type (Combat).
    /// Both steps are planned as data; if a plan can't satisfy the rules it retries with a new
    /// seed, and rooms are only instantiated once a plan passes.
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Rooms")]
        [SerializeField] private RoomChunk startRoom;
        [SerializeField] private List<RoomChunk> roomPrefabs = new List<RoomChunk>();

        [Header("Generation")]
        [Tooltip("Total rooms including the start room.")]
        [SerializeField, Min(1)] private int roomCount = 12;
        [Tooltip("0 picks a random seed each run. If a seed's layout breaks a room type rule, " +
                 "later attempts derive new seeds from it, so the result is still reproducible.")]
        [SerializeField] private int seed;
        [Tooltip("How many layouts to try before giving up on the room type rules.")]
        [SerializeField, Min(1)] private int maxAttempts = 25;
        [SerializeField] private bool generateOnStart = true;

        [Header("Room Types")]
        [Tooltip("Applied top to bottom, so earlier types get first pick of rooms.")]
        [SerializeField] private List<RoomTypeRule> typeRules = new List<RoomTypeRule>
        {
            new RoomTypeRule
            {
                type = RoomType.Boss, minCount = 1, maxCount = 1,
                deadEndOnly = true, placement = RoomPlacement.Farthest
            },
            new RoomTypeRule
            {
                type = RoomType.Shop, minCount = 1, maxCount = 1,
                deadEndOnly = true, minDepth = 2, placement = RoomPlacement.Random,
                notNextTo = new List<RoomType> { RoomType.Boss }
            },
            new RoomTypeRule
            {
                type = RoomType.Treasure, minCount = 1, maxCount = 2,
                deadEndOnly = true, placement = RoomPlacement.Random,
                notNextTo = new List<RoomType> { RoomType.Boss, RoomType.Treasure }
            }
        };
        [Tooltip("Every room no rule claimed becomes this type.")]
        [SerializeField] private RoomType fillType = RoomType.Combat;
        [Tooltip("Spawned at each room's content anchor, by type.")]
        [SerializeField] private List<RoomTypeContent> typeContents = new List<RoomTypeContent>();

        // Lets rooms share an edge without counting as overlapping.
        private const float OverlapTolerance = 0.01f;

        private readonly List<PlannedRoom> plan = new List<PlannedRoom>();
        private readonly List<OpenDoor> openDoors = new List<OpenDoor>();
        private System.Random rng;

        /// <summary>The seed of the attempt that produced the current layout.</summary>
        public int LastSeed { get; private set; }
        public List<RoomChunk> PlacedRooms { get; } = new List<RoomChunk>();

        private class PlannedRoom
        {
            public RoomChunk Prefab;
            public Vector2 Position;
            public Rect Rect;
            public int Depth;
            public RoomType? Type;
            public readonly HashSet<int> ConnectedDoors = new HashSet<int>();
            public readonly List<int> Neighbours = new List<int>();
            public bool IsDeadEnd => Neighbours.Count == 1;
        }

        private struct OpenDoor
        {
            public int Room;
            public int Door;
        }

        private void Start()
        {
            if (generateOnStart) Generate();
        }

        [ContextMenu("Generate")]
        public void Generate()
        {
            Clear();

            if (startRoom == null)
            {
                Debug.LogError("DungeonGenerator: no start room assigned.", this);
                return;
            }

            int baseSeed = seed != 0 ? seed : Random.Range(int.MinValue, int.MaxValue);
            System.Random seedSource = new System.Random(baseSeed);
            List<string> problems = new List<string>();

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                LastSeed = attempt == 0 ? baseSeed : seedSource.Next(int.MinValue, int.MaxValue);
                rng = new System.Random(LastSeed);
                PlanLayout();
                problems = AssignTypes();
                if (problems.Count == 0) break;
            }

            if (problems.Count > 0)
                Debug.LogWarning($"DungeonGenerator: no layout met every room type rule after {maxAttempts} " +
                                 $"attempts (using seed {LastSeed}):\n- {string.Join("\n- ", problems)}\n" +
                                 "Try more rooms, room shapes with fewer doorways (more dead ends), or looser rules.", this);
            else if (plan.Count < roomCount)
                Debug.LogWarning($"DungeonGenerator: placed {plan.Count}/{roomCount} rooms (seed {LastSeed}). " +
                                 "Add more room shapes or doorway directions so rooms have more ways to fit.", this);

            BuildLayout();
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            PlacedRooms.Clear();
        }

        // ---------- Step 1: layout ----------

        private void PlanLayout()
        {
            plan.Clear();
            openDoors.Clear();

            AddToPlan(startRoom, transform.position, -1, 0);

            while (plan.Count < roomCount && openDoors.Count > 0 && roomPrefabs.Count > 0)
            {
                int index = rng.Next(openDoors.Count);
                OpenDoor open = openDoors[index];
                openDoors.RemoveAt(index);

                // A doorway nothing fits becomes a dead end.
                List<RoomChunk> candidates = new List<RoomChunk>(roomPrefabs);
                Shuffle(candidates);
                foreach (RoomChunk prefab in candidates)
                {
                    if (prefab != null && TryAttach(prefab, open)) break;
                }
            }
        }

        private bool TryAttach(RoomChunk prefab, OpenDoor open)
        {
            PlannedRoom parent = plan[open.Room];
            Doorway parentDoor = parent.Prefab.Doorways[open.Door];
            Vector2 targetPos = parent.Position + parent.Prefab.GetLocalDoorPosition(parentDoor);
            DoorDirection needed = Doorway.Opposite(parentDoor.Direction);

            List<int> matching = new List<int>();
            Doorway[] doors = prefab.Doorways;
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i].Direction == needed) matching.Add(i);
            }
            Shuffle(matching);

            foreach (int doorIndex in matching)
            {
                Vector2 rootPos = targetPos - prefab.GetLocalDoorPosition(doors[doorIndex]);
                if (Overlaps(prefab.GetRectAt(rootPos))) continue;

                parent.ConnectedDoors.Add(open.Door);
                int child = AddToPlan(prefab, rootPos, doorIndex, parent.Depth + 1);
                parent.Neighbours.Add(child);
                plan[child].Neighbours.Add(open.Room);
                return true;
            }
            return false;
        }

        private int AddToPlan(RoomChunk prefab, Vector2 rootPos, int entryDoor, int depth)
        {
            PlannedRoom room = new PlannedRoom
            {
                Prefab = prefab,
                Position = rootPos,
                Rect = prefab.GetRectAt(rootPos),
                Depth = depth
            };
            if (entryDoor >= 0) room.ConnectedDoors.Add(entryDoor);

            int roomIndex = plan.Count;
            plan.Add(room);

            for (int i = 0; i < prefab.Doorways.Length; i++)
            {
                if (i != entryDoor) openDoors.Add(new OpenDoor { Room = roomIndex, Door = i });
            }
            return roomIndex;
        }

        private bool Overlaps(Rect rect)
        {
            Rect shrunk = new Rect(
                rect.xMin + OverlapTolerance, rect.yMin + OverlapTolerance,
                rect.width - OverlapTolerance * 2f, rect.height - OverlapTolerance * 2f);

            foreach (PlannedRoom placed in plan)
            {
                if (placed.Rect.Overlaps(shrunk)) return true;
            }
            return false;
        }

        // ---------- Step 2: room types ----------

        /// <returns>Rules that couldn't be met; empty if the plan is valid.</returns>
        private List<string> AssignTypes()
        {
            List<string> problems = new List<string>();
            plan[0].Type = RoomType.Start;

            foreach (RoomTypeRule rule in typeRules)
            {
                if (rule.type == RoomType.Start) continue;

                List<int> candidates = new List<int>();
                for (int i = 1; i < plan.Count; i++)
                {
                    if (plan[i].Type == null && FitsRule(i, rule)) candidates.Add(i);
                }
                OrderCandidates(candidates, rule.placement);

                int placed = 0;
                foreach (int i in candidates)
                {
                    if (placed >= rule.maxCount) break;
                    // Re-check: rooms assigned earlier in this loop can block their neighbours.
                    if (!FitsNeighbours(i, rule.type)) continue;
                    plan[i].Type = rule.type;
                    placed++;
                }

                if (placed < rule.minCount)
                    problems.Add($"{rule.type}: only {placed}/{rule.minCount} rooms fit its rules");
            }

            foreach (PlannedRoom room in plan)
            {
                if (room.Type == null) room.Type = fillType;
            }
            return problems;
        }

        private bool FitsRule(int roomIndex, RoomTypeRule rule)
        {
            PlannedRoom room = plan[roomIndex];
            if (rule.deadEndOnly && !room.IsDeadEnd) return false;
            if (!rule.AllowsDepth(room.Depth)) return false;
            if (!room.Prefab.AllowsType(rule.type)) return false;
            return FitsNeighbours(roomIndex, rule.type);
        }

        /// <summary>Checks notNextTo in both directions against neighbours that already have a type.</summary>
        private bool FitsNeighbours(int roomIndex, RoomType type)
        {
            RoomTypeRule rule = FindRule(type);
            foreach (int n in plan[roomIndex].Neighbours)
            {
                RoomType? neighbourType = plan[n].Type;
                if (neighbourType == null) continue;
                if (rule != null && rule.notNextTo.Contains(neighbourType.Value)) return false;

                RoomTypeRule neighbourRule = FindRule(neighbourType.Value);
                if (neighbourRule != null && neighbourRule.notNextTo.Contains(type)) return false;
            }
            return true;
        }

        private RoomTypeRule FindRule(RoomType type) => typeRules.Find(r => r.type == type);

        private void OrderCandidates(List<int> candidates, RoomPlacement placement)
        {
            // Shuffle first so ties in depth are broken randomly.
            Shuffle(candidates);
            if (placement == RoomPlacement.Farthest)
                candidates.Sort((a, b) => plan[b].Depth.CompareTo(plan[a].Depth));
            else if (placement == RoomPlacement.Closest)
                candidates.Sort((a, b) => plan[a].Depth.CompareTo(plan[b].Depth));
        }

        // ---------- Build ----------

        private void BuildLayout()
        {
            foreach (PlannedRoom planned in plan)
            {
                RoomChunk room = Instantiate(planned.Prefab, planned.Position, Quaternion.identity, transform);
                RoomType type = planned.Type ?? fillType;
                room.name = $"{PlacedRooms.Count:00}_{type}_{planned.Prefab.name}";
                room.SetType(type);
                PlacedRooms.Add(room);

                Doorway[] doors = room.Doorways;
                for (int i = 0; i < doors.Length; i++)
                {
                    doors[i].SetConnected(planned.ConnectedDoors.Contains(i));
                }

                foreach (RoomTypeContent content in typeContents)
                {
                    if (content.type == type && content.prefab != null)
                        Instantiate(content.prefab, room.ContentPosition, Quaternion.identity, room.transform);
                }
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

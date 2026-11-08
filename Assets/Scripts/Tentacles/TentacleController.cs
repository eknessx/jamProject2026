using UnityEngine;

namespace Tentacles
{
    public class TentacleController : MonoBehaviour
    {
        [Header("Tentacle Physics & Rigging")]
        [SerializeField] private Transform targetPoint;

        private void LateUpdate()
        {
            // TODO: Implement inverse kinematics / tentacle tracking logic
        }
    }
}

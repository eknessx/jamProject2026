using UnityEngine;

namespace Core
{
    public enum GameState
    {
        MainMenu,
        Gameplay,
        Paused,
        GameOver,
        Victory
    }

    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }
        public GameState CurrentState { get; private set; }

        public delegate void OnStateChanged(GameState newState);
        public static event OnStateChanged StateChangedEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            
            CurrentState = newState;
            StateChangedEvent?.Invoke(newState);
        }
    }
}

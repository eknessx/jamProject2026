#!/usr/bin/env bash
# ==========================================
# Unity Game Jam 2026 Project Scaffolder (Bash)
# ==========================================

set -e

echo "Creating Unity asset directory structure..."

# Define base directories
directories=(
    "Assets/Animations/Creature"
    "Assets/Animations/UI"
    "Assets/Art/Characters"
    "Assets/Art/Environment"
    "Assets/Art/UI"
    "Assets/Audio/Music"
    "Assets/Audio/SFX"
    "Assets/Prefabs/Creature"
    "Assets/Prefabs/Environment"
    "Assets/Prefabs/UI"
    "Assets/Scenes"
    "Assets/ScriptableObjects"
    "Assets/Scripts/Core"
    "Assets/Scripts/Creature"
    "Assets/Scripts/Environment"
    "Assets/Scripts/Interaction"
    "Assets/Scripts/Tentacles"
    "Assets/Scripts/Utilities"
    "Assets/Settings"
    "Assets/UI"
)

# Create directories if they don't exist
for dir in "${directories[@]}"; do
    mkdir -p "$dir"
    echo "  [+] Created: $dir"
done

echo -e "\nGenerating starter C# script templates..."

# 1. GameManager.cs
cat << 'EOF' > "Assets/Scripts/Core/GameManager.cs"
using UnityEngine;

namespace Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
EOF

# 2. GameStateManager.cs
cat << 'EOF' > "Assets/Scripts/Core/GameStateManager.cs"
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
EOF

# 3. AudioManager.cs
cat << 'EOF' > "Assets/Scripts/Core/AudioManager.cs"
using UnityEngine;

namespace Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource.clip == clip) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
EOF

# 4. IInteractable.cs
cat << 'EOF' > "Assets/Scripts/Interaction/IInteractable.cs"
namespace Interaction
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact();
    }
}
EOF

# 5. CreatureController.cs
cat << 'EOF' > "Assets/Scripts/Creature/CreatureController.cs"
using UnityEngine;

namespace Creature
{
    public class CreatureController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;

        private void Update()
        {
            // TODO: Implement movement logic
        }
    }
}
EOF

# 6. TentacleController.cs
cat << 'EOF' > "Assets/Scripts/Tentacles/TentacleController.cs"
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
EOF

# 7. Extensions.cs
cat << 'EOF' > "Assets/Scripts/Utilities/Extensions.cs"
using UnityEngine;

namespace Utilities
{
    public static class Extensions
    {
        // Add custom extension methods here
    }
}
EOF

echo "  [+] C# template scripts generated successfully."

# Initialize Git repository
echo -e "\nInitializing Git and LFS..."
if [ ! -d ".git" ]; then
    git init
    echo "  [+] Initialized empty Git repository."
fi

# Create Unity .gitignore if missing
if [ ! -f ".gitignore" ]; then
    cat << 'EOF' > ".gitignore"
# Unity generated
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Mm]emoryCaptures/

# Asset meta data should be tracked
!/[Aa]ssets/**/*.meta

# IDE files
*.csproj
*.unityproj
*.sln
*.suo
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# OS files
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db
EOF
    echo "  [+] Generated Unity .gitignore file."
fi

# Initialize Git LFS for binaries
git lfs install
git lfs track "*.mp3"
git lfs track "*.wav"
git lfs track "*.png"
git lfs track "*.psd"
git lfs track "*.asset"

# Stage and commit
git add .
git commit -m "chore: initialize complete 2D project structure, Git LFS tracking, and core C# scripts"
echo -e "\nProject setup complete and committed safely!"
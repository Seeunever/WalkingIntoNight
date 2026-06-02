using UnityEngine;

namespace AnimalCafe.TRPG.Core
{
    public static class TRPGRuntimeInit
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureGameState()
        {
            if (GameStateManager.Instance != null) return;

            var go = new GameObject(nameof(GameStateManager));
            go.AddComponent<GameStateManager>();
        }
    }
}

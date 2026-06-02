using WalkingIntoNight.TRPG.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WalkingIntoNight.TRPG.Core
{
    public static class AutoSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UIRoot.Clear();

            switch (scene.name)
            {
                case SceneNames.MainMenu:
                    MainMenuUI.Build();
                    break;
                case SceneNames.CharacterCreate:
                    CharacterCreateUI.Build();
                    break;
                case SceneNames.Gameplay:
                    GameplayUI.Build();
                    break;
            }
        }
    }
}

using UnityEngine.SceneManagement;

namespace AnimalCafe.TRPG.Core
{
    public static class SceneLoader
    {
        public static void LoadMainMenu()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        public static void LoadCharacterCreate()
        {
            SceneManager.LoadScene(SceneNames.CharacterCreate);
        }

        public static void LoadGameplay()
        {
            SceneManager.LoadScene(SceneNames.Gameplay);
        }
    }
}

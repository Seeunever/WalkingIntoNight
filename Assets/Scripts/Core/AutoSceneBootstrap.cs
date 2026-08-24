using WalkingIntoNight.TRPG.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WalkingIntoNight.TRPG.Core
{
    public static class AutoSceneBootstrap
    {
        static bool s_legacyMigrationAttemptedThisSession;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSessionState()
        {
            s_legacyMigrationAttemptedThisSession = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!s_legacyMigrationAttemptedThisSession)
            {
                s_legacyMigrationAttemptedThisSession = true;
                if (!SaveSystem.TryMigrateLegacySaves(
                        out var migration,
                        out var migrationError))
                {
                    Debug.LogWarning(migrationError);
                }
                else
                {
                    if (migration.MigratedSlots > 0)
                    {
                        Debug.Log(
                            $"已安全迁移 {migration.MigratedSlots} 个旧存档槽位；" +
                            "原文件仍保留，可用于回退。");
                    }

                    if (migration.InvalidLegacySlots > 0)
                    {
                        Debug.LogWarning(
                            $"发现 {migration.InvalidLegacySlots} 个无效旧存档槽位，" +
                            "未复制也未删除原文件。");
                    }
                }
            }

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

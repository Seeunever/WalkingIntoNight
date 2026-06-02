using UnityEngine;

namespace WalkingIntoNight.TRPG.Steam
{
    /// <summary>
    /// Steamworks 占位：在 Package Manager 或导入 Steamworks.NET 后定义 STEAMWORKS 符号并接入 SDK。
    /// 构建 Win64 前请在 Steamworks 伙伴后台创建 AppID 并放置 steam_appid.txt（开发用）。
    /// </summary>
    public class SteamBootstrap : MonoBehaviour
    {
        public uint steamAppId = 480; // Spacewar 测试用；发行前替换为正式 AppID

        static bool s_initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (s_initialized) return;
            var go = new GameObject(nameof(SteamBootstrap));
            go.AddComponent<SteamBootstrap>();
            DontDestroyOnLoad(go);
            s_initialized = true;
        }

        void Start()
        {
#if STEAMWORKS
            if (!Steamworks.SteamAPI.Init())
            {
                Debug.LogWarning("[Steam] SteamAPI.Init failed.");
                return;
            }
            Debug.Log($"[Steam] Initialized AppId={steamAppId}");
#else
            Debug.Log("[Steam] STEAMWORKS 未定义 — 跳过 SDK。导入 Steamworks.NET 后在 Player Settings 添加 Scripting Define: STEAMWORKS");
#endif
        }

        void Update()
        {
#if STEAMWORKS
            Steamworks.SteamAPI.RunCallbacks();
#endif
        }

        void OnDestroy()
        {
#if STEAMWORKS
            Steamworks.SteamAPI.Shutdown();
#endif
        }
    }
}

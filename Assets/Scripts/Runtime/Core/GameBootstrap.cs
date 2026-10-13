using UnityEngine;

namespace BlackHeart
{
    // Позволяет запускать игру на пустой сцене: если GameManager нет — создаём.
    static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureGame()
        {
            if (Application.isMobilePlatform)
                Screen.orientation = ScreenOrientation.LandscapeLeft;

            if (Object.FindAnyObjectByType<GameManager>() == null)
                new GameObject("Game").AddComponent<GameManager>();
        }
    }
}

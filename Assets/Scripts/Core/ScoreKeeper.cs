using UnityEngine;

namespace BlackHeart
{
    // Очки и правила их начисления.
    public class ScoreKeeper : MonoBehaviour
    {
        [Header("Очки")]
        public int whiteBullet = 10;
        public int purpleFragment = 15;
        public int blueBulletPenalty = -25;
        public int enemyDefeated = 100;
        public int cleanDefense = 50;     // фаза защиты пройдена без урона

        public int Score { get; private set; }
        public int Best { get; private set; }

        const string BestKey = "blackheart.best";

        void Awake() => Best = PlayerPrefs.GetInt(BestKey, 0);

        public void ResetScore()
        {
            Score = 0;
            GameEvents.RaiseScoreChanged(0, 0, Vector2.zero);
        }

        public void Add(int delta, Vector2 worldPos)
        {
            Score = Mathf.Max(0, Score + delta);
            GameEvents.RaiseScoreChanged(delta, Score, worldPos);
        }

        public bool CommitBest()
        {
            if (Score <= Best) return false;
            Best = Score;
            PlayerPrefs.SetInt(BestKey, Best);
            PlayerPrefs.Save();
            return true;
        }
    }
}

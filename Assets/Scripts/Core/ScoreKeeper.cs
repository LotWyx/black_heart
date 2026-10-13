using UnityEngine;

namespace BlackHeart
{
    // Очки забега и рекорд. Сколько очков за что даётся — в ассете GameBalance, рекорд хранит SaveSystem.
    public class ScoreKeeper : MonoBehaviour
    {
        static GameBalance B => GameBalance.Current;

        public int whiteBullet => B.whiteBullet;
        public int purpleFragment => B.purpleFragment;
        public int blueBulletPenalty => B.blueBulletPenalty;
        public int enemyDefeated => B.enemyDefeated;
        public int cleanDefense => B.cleanDefense;

        public int Score { get; private set; }
        public int Best => SaveSystem.Data.bestScore;

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
            SaveSystem.Data.bestScore = Score;
            SaveSystem.Save();
            return true;
        }
    }
}

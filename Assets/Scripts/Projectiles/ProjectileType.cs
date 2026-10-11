using UnityEngine;

namespace BlackHeart
{
    public enum ProjectileType
    {
        White,   // обычная: урон при попадании, очки за уничтожение
        Purple,  // при первом ударе раскалывается на две
        Blue     // рубить нельзя: урон и штраф; в игрока попадает без урона
    }

    public static class ProjectileTypes
    {
        // Спрайт у всех типов один (чёрно-белый), тип задаёт только цвет
        public static Color ColorOf(ProjectileType type)
        {
            switch (type)
            {
                case ProjectileType.Purple: return new Color(0.75f, 0.35f, 1f);
                case ProjectileType.Blue: return new Color(0.2f, 0.65f, 1f);
                default: return Color.white;
            }
        }
    }
}

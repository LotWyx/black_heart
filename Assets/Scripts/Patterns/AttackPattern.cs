using System.Collections;
using UnityEngine;

namespace BlackHeart
{
    // Атака врага: определяет, откуда и как летят пули. Каждая атака — префаб в Assets/Prefabs/Patterns
    // с одним таким компонентом; все числа атаки правятся в инспекторе префаба.
    // Врагу атаки назначаются списком префабов (EnemyDefinition.attacks).
    // Префаб в сцену не ставится: бой читает параметры прямо из него, поэтому правки видны уже
    // в следующей атаке, даже в Play Mode.
    // Новая атака без кода: дубликат готового префаба с другими числами или префаб с CompositePattern.
    // Новая атака с кодом: наследник AttackPattern в своём файле (имя файла = имя класса) и префаб с ним.
    public abstract class AttackPattern : MonoBehaviour
    {
        // Что делает атака — показывается в инспекторе над параметрами
        public abstract string Summary { get; }

        public abstract IEnumerator Run(AttackContext ctx);
    }
}

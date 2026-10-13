# Как работать над Black Heart

Короткие правила, чтобы проект у всех открывался одинаково и ничего не ломалось после `git pull`.

## Окружение

- **Unity 6000.6.5f1** (точная версия — в `ProjectSettings/ProjectVersion.txt`). Модули: *Windows Build Support (Mono)*.
- **Aseprite** — только если правите графику (генераторы в `Art/Source`).
- IDE — Rider или Visual Studio; стиль кода задан в `.editorconfig`.

## Ветки и Pull Request

`main` — всегда рабочая версия игры. **Напрямую в `main` не пушим.**

1. Обновить `main` и создать ветку под задачу:
   ```bash
   git switch main
   git pull --rebase
   git switch -c feat/zigzag-dart
   ```
   Префиксы веток: `feat/` — новое, `fix/` — исправление, `chore/` — порядок в проекте, `art/` — графика и звук.
2. Работать, коммитить небольшими шагами (см. «Как коммитить»).
3. Запушить ветку и открыть **Pull Request** в `main`:
   ```bash
   git push -u origin feat/zigzag-dart
   ```
4. Дождаться зелёного CI и ревью второго участника → **Squash and merge**.
5. Удалить ветку после слияния.

## Как коммитить (главное для Unity)

1. В Unity: **File → Save Project** — иначе изменённые префабы и ассеты не попадут на диск.
2. Посмотреть, что изменилось:
   ```bash
   git status
   ```
   **Каждый новый ассет должен идти вместе со своим `.meta`.** Без `.meta` у других участников ссылки
   превратятся в *Missing* (так сломался коммит «optimize2»: ассеты врагов ссылались на префабы,
   которые не попали в коммит).
3. Добавить **все** изменения, включая новые файлы (`commit -a` новые файлы пропускает!):
   ```bash
   git add -A
   git diff --cached --stat
   ```
4. Проверить проект и закоммитить:
   ```bash
   python3 tools/ci/check_project.py
   git commit -m "feat: зигзаг-пуля на средней сложности"
   ```

### Сообщения коммитов

`тип: что сделано` — по-русски, в повелительном или прошедшем времени, без точки в конце.

| Тип | Когда |
|---|---|
| `feat` | новая механика, атака, экран |
| `fix` | исправление бага |
| `balance` | правка чисел баланса |
| `art` | графика, музыка, звуки |
| `refactor` | переработка кода без изменения поведения |
| `chore` | структура проекта, настройки, CI |
| `docs` | README, CONTRIBUTING, комментарии |

## Структура

```
Assets/
├── Scenes/Game.unity           единственная сцена: GameManager собирает всё остальное кодом
├── Scripts/
│   ├── Runtime/                код игры — сборка BlackHeart.Runtime
│   │   ├── Core/               GameManager, камера, события, баланс, сохранения
│   │   ├── Battle/             цикл боя, сложность, обучение
│   │   ├── Patterns/           атаки врагов (AttackPattern, PatternLibrary)
│   │   ├── Projectiles/        пули, пул, траектории
│   │   └── Enemies/ UI/ Audio/ FX/ Input/ Player/ Arena/ Art/
│   └── Editor/                 правила импорта — сборка BlackHeart.Editor
├── Tests/EditMode/             автотесты — сборка BlackHeart.Tests.EditMode
├── Resources/                  грузится по имени из кода
│   ├── Data/                   GameBalance.asset и враги — баланс меняется здесь, не в коде
│   ├── Art/  Audio/  Fonts/
├── Art/Atlases/                атласы спрайтов
└── Settings/                   URP
Art/Source/                     Lua-генераторы пиксель-арта (Aseprite) и исходники .aseprite
tools/ci/                       проверки для CI
.github/                        CI, шаблоны PR и задач
```

- Новый скрипт игры — в `Assets/Scripts/Runtime/<раздел>/`, пространство имён `BlackHeart`.
- Новые числа баланса — полем в `GameBalance` (с `[Tooltip]`) и значением в `GameBalance.asset`.
- Музыка — `Assets/Resources/Audio/Music/<Где>_<Название>.ogg` (`Menu_`, `Battle_`, …).

## Тесты

*Window → General → Test Runner → EditMode → Run All.* Тесты проверяют:
- данные: баланс загружается, у врагов есть атаки, все они существуют;
- траектории: зигзаг-пуля не задевает сердце во время форы, фора считается с входа в рамку;
- порядок: у каждого ассета есть `.meta`.

Новая механика с проверяемым правилом («пуля долетает не быстрее…») — повод добавить тест.

## CI (GitHub Actions)

Файл `.github/workflows/ci.yml`. На каждый Pull Request и пуш в `main`:

| Задача | Что делает | Нужно |
|---|---|---|
| Проверка проекта | `.meta` у всех ассетов, нет ссылок на забытые файлы | ничего |
| Тесты | EditMode-тесты в Unity (GameCI) | секреты Unity |
| Билд Windows | архив `BlackHeart_Windows.zip`; на тег `vX.Y.Z` — релиз на GitHub | секреты Unity |

**Секреты Unity** (*Settings → Secrets and variables → Actions*): `UNITY_LICENSE`, `UNITY_EMAIL`,
`UNITY_PASSWORD`. Как получить файл лицензии для личной (Personal) лицензии — в документации GameCI:
<https://game.ci/docs/github/activation>. Пока секретов нет, тесты и билд в CI пропускаются.

## Релиз

1. Обновить версию: *Project Settings → Player → Version* (например `0.2.0`) и раздел в `CHANGELOG.md`.
2. Слить в `main`, затем поставить тег:
   ```bash
   git tag -a v0.2.0 -m "Black Heart v0.2.0"
   git push origin v0.2.0
   ```
3. CI соберёт билд и создаст релиз с архивом (при настроенных секретах).

## Слияние ассетов Unity

Ассеты хранятся текстом (*Force Text*), поэтому конфликты в сценах и префабах можно сливать
инструментом Unity **UnityYAMLMerge**. Один раз на компьютер:

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'<путь к Unity>/Editor/Data/Tools/UnityYAMLMerge' merge -p %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
```

`<путь к Unity>` — папка редактора, например `~/Unity/Hub/Editor/6000.6.5f1` (Linux) или
`C:/Program Files/Unity/Hub/Editor/6000.6.5f1` (Windows).
Лучший способ избежать конфликтов — не править одну и ту же сцену или префаб вдвоём одновременно.

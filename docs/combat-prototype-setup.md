# Прототип боя — сборка сцены

Этап roadmap «Прототип боя»: движение, атаки, 1 враг с ИИ.
Стек по GDD п.14: Unity 6 + URP, Unity Input System, Cinemachine, NavMesh.

## Подготовка проекта

1. Открыть папку репозитория через Unity Hub (Unity 6). Пакеты из
   `Packages/manifest.json` подтянутся автоматически: URP, Input System,
   Cinemachine, AI Navigation.
2. **Edit → Project Settings → Player → Active Input Handling** →
   `Input System Package (New)` (или `Both`).
3. Создать URP-ассеты: **Assets → Create → Rendering → URP Asset (with Universal Renderer)**
   и назначить в **Project Settings → Graphics**.
4. Сохранить сцену как `Assets/Scenes/CombatPrototype.unity`.

## Арена

1. Plane (масштаб 5×5) — пол.
2. Несколько Cube — препятствия для проверки NavMesh.
3. На пол добавить компонент **NavMeshSurface** (пакет AI Navigation) и нажать **Bake**.

## Игрок

1. Capsule, имя `Player`, позиция `(0, 1, 0)`. Удалить Capsule Collider
   (его заменит CharacterController).
2. Добавить компоненты:
   - `CharacterController` (height 2, radius 0.4)
   - `PlayerInputReader`
   - `PlayerStamina`
   - `PlayerController`
   - `Health` (Max Health 100)
   - `PlayerCombat`
3. В `PlayerCombat → Hittable Layers` оставить Everything (или создать слой `Enemy`).

## Камера

1. **GameObject → Cinemachine → Third Person Aim Camera** (или Follow Camera).
2. Follow / Look At → `Player`.
3. Дистанция ~5 м, высота ~2 м — референс Valheim.

## Враг

1. Capsule, имя `Infected`, поставить на NavMesh в 15+ м от игрока.
2. Добавить компоненты:
   - `NavMeshAgent` (speed 3.5, stopping distance 1.5)
   - `Health` (Max Health 60)
   - `EnemyAI`
3. Сохранить как префаб `Assets/Prefabs/Infected.prefab`, расставить 2–3 штуки.

## HUD

Пустой GameObject `Debug HUD` с компонентом `DebugHud`.

## Управление

| Действие | Клавиатура/мышь | Геймпад |
|----------|-----------------|---------|
| Движение | WASD | Левый стик |
| Спринт | Shift (удерж.) | Нажатие стика |
| Атака (комбо ×3) | ЛКМ | X / Квадрат |
| Парирование | ПКМ | LB / L1 |
| Уклонение | Пробел | B / Круг |

## Что проверять

- Комбо из трёх ударов: третий удар сильнее, ввод буферизуется во время замаха.
- Уклонение даёт i-фреймы первые 0.3 с — удар врага в этот момент не проходит.
- Парирование в окно 0.18 с прерывает атаку врага и ошеломляет его на 1.5 с.
- Замах врага (0.6 с, разворот к цели) читается — есть время среагировать.
- Стамина: спам атак/перекатов упирается в ресурс, реген после паузы 0.8 с.

# Кооп 2 игрока — сборка сцены

Этап roadmap «Кооп 2 игрока»: FishNet, синхронизация позиций.
GDD §14: FishNet + Steam Relay; прототип — Tugboat (локальный UDP).

## Установка FishNet

Unity подтянет пакет `com.firstgeargames.fishnet` из OpenUPM автоматически
при открытии проекта (scoped registry прописан в `Packages/manifest.json`).

Если OpenUPM недоступен — скачать FishNet с Asset Store или
`https://github.com/FirstGearGames/FishNet/releases` и импортировать .unitypackage.

## Настройка NetworkManager

1. Создать пустой GameObject `NetworkManager`.
2. Добавить компоненты:
   - **NetworkManager** (FishNet)
   - **Tugboat** (транспорт — `FishNet.Transporting.Tugboat`)
   - **PlayerSpawner** (скрипт Ashveil)
   - **GameNetworkManager** (скрипт Ashveil)
3. В **NetworkManager → Spawnable Prefabs** добавить Network Player Prefab (см. ниже).

## Сетевой префаб игрока

Взять капсулу-игрока из прошлого этапа, добавить компоненты:
- **NetworkObject** (FishNet — главный)
- **NetworkTransform** (FishNet — позиция/ротация реплицируется)
- **NetworkPlayer**
- **NetworkHealth**

Дочерний объект `LocalCameraRig` с Cinemachine-камерой —
назначить в поле `Local Camera Rig` компонента `NetworkPlayer`.

Сохранить как `Assets/Prefabs/NetworkPlayer.prefab`.
Зарегистрировать в `NetworkManager → Spawnable Prefabs`.

## Сетевой префаб врага

Враг из прошлого этапа + компоненты:
- **NetworkObject**
- **NetworkTransform** (server authoritative)
- **NetworkEnemy**
- **NetworkHealth**

Сохранить как `Assets/Prefabs/NetworkInfected.prefab`.
Зарегистрировать в `NetworkManager → Spawnable Prefabs`.

## Точки спауна

Создать пустые GameObject-ы `SpawnPoint_1`, `SpawnPoint_2` в разных точках арены.
Назначить в `PlayerSpawner → Spawn Points`.

## Лобби-панель (опционально для прототипа)

Простой Canvas с двумя кнопками:
- **Host** → `GameNetworkManager.StartHost()`
- **Join** → `GameNetworkManager.StartClient()` (IP хоста менять в Tugboat)

## Тест локально (два редактора)

1. Открыть два экземпляра Unity (ParrelSync → Clone Manager).
2. В клоне: **Edit → Preferences → ParrelSync** → отметить Clone.
3. В основном: Play → Host.
4. В клоне: Play → Join.
5. Оба персонажа видят друг друга. Атаки проходят через сервер.

## Что синхронизировано

| Данные | Механизм |
|--------|----------|
| Позиция / ротация игроков | NetworkTransform (unreliable, интерполяция) |
| HP игроков и врагов | SyncVar в NetworkHealth |
| Урон | ServerRpc → хост считает, результат через SyncVar |
| ИИ врагов | Работает только у хоста (EnemyAI отключён у клиентов) |
| Смерть врага | SyncVar `_dead` → клиенты скрывают объект |
| Спаун игроков | PlayerSpawner.OnClientReady → ServerManager.Spawn |

## Следующее: Steam Relay

Когда прототип стабилен, заменить Tugboat на FishySteamworks:
- Установить `com.fishlabs.fishysteamworks` и `com.unity.steamworks-net`.
- Транспорт: Tugboat → FishySteamworks.
- Инвайт: `SteamFriends.InviteUserToGame` + Steam Overlay.
- Порт-форвардинг не нужен.

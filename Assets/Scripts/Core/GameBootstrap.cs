using System.Collections.Generic;
using Ashveil.Building;
using Ashveil.Combat;
using Ashveil.Crafting;
using Ashveil.Enemies;
using Ashveil.Enemies.Bosses;
using Ashveil.Items;
using Ashveil.Player;
using Ashveil.SaveLoad;
using Ashveil.UI;
using Ashveil.World;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Ashveil.Core
{
    /// <summary>
    /// Собирает всю прототипную сцену из кода при нажатии Play:
    /// процедурный рельеф, NavMesh, игрок, камера, враги, босс,
    /// ресурсы, база (очаг + кузница), строительство, HUD, сейвы.
    ///
    /// Запуск: пустая сцена → пустой GameObject → добавить этот компонент → Play.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Мир")]
        [SerializeField] private int enemyCount = 8;
        [SerializeField] private int treeCount = 60;
        [SerializeField] private int rockCount = 30;
        [SerializeField] private int ironNodeCount = 6;

        private TerrainGenerator _terrain;
        private int _seed;

        // Шаблоны (неактивные объекты вместо префабов).
        private EnemyAI _enemyTemplate;
        private ItemPickup _pickupTemplate;
        private PoisonCloud _poisonTemplate;

        // Предметы.
        private ItemDefinition _rustedBlade, _bogTrinket, _ritualStaff, _shepherdMask;
        private ItemDefinition _boneSword, _leatherChest, _ironBlade;

        private void Awake()
        {
            SaveSystem.SaveData save = SaveSystem.LoadOrNull();
            _seed = save?.seed ?? Random.Range(1, 99999);

            CreateLighting();
            CreateTerrain();
            CreateItems();
            CreateTemplates();
            SpawnResourceNodes();
            BakeNavMesh();

            PlayerController player = SpawnPlayer();
            CreateCamera(player.transform);
            CreateBase(player.transform.position);
            SpawnEnemies();
            SpawnBoss();
            CreateHud(player);

            // Восстановление прогресса.
            var saveSystem = FindFirstObjectByType<SaveSystem>();
            saveSystem.CurrentSeed = _seed;
            SaveSystem.Restore(save, player, player.GetComponent<BuildPlacer>());

            if (save == null)
            {
                // Стартовые ресурсы для проверки строительства.
                var wallet = player.GetComponent<ResourceWallet>();
                wallet.Add(ResourceType.Wood, 30);
                wallet.Add(ResourceType.Stone, 20);
            }
        }

        // ---------- Мир ----------

        private void CreateLighting()
        {
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.95f, 0.85f);
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
        }

        private void CreateTerrain()
        {
            var terrainGo = new GameObject("Terrain");
            _terrain = terrainGo.AddComponent<TerrainGenerator>();
            _terrain.Generate(_seed);

            // У созданного с нуля MeshRenderer нет материала — берём дефолтный
            // у временного примитива (работает и в Built-in, и в URP).
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Material defaultMat = temp.GetComponent<Renderer>().sharedMaterial;
            terrainGo.GetComponent<MeshRenderer>().material = new Material(defaultMat)
            {
                color = new Color(0.35f, 0.42f, 0.3f) // болотная зелень
            };
            Destroy(temp);
        }

        private float GroundY(float x, float z) => _terrain.GetHeight(x, z);

        private void SpawnResourceNodes()
        {
            Random.InitState(_seed);
            float half = _terrain.WorldSize / 2f - 10f;

            for (int i = 0; i < treeCount; i++)
            {
                Vector3 pos = RandomPoint(half, minDistFromCenter: 10f);
                GameObject tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tree.name = "Tree";
                tree.transform.position = pos + Vector3.up * 2f;
                tree.transform.localScale = new Vector3(0.6f, 2f, 0.6f);
                Tint(tree, new Color(0.35f, 0.25f, 0.15f));
                tree.AddComponent<HarvestableNode>().Configure(ResourceType.Wood, 2, 40f);
            }

            for (int i = 0; i < rockCount; i++)
            {
                Vector3 pos = RandomPoint(half, minDistFromCenter: 10f);
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "Rock";
                rock.transform.position = pos + Vector3.up * 0.5f;
                rock.transform.localScale = Vector3.one * 1.4f;
                Tint(rock, new Color(0.5f, 0.5f, 0.52f));
                rock.AddComponent<HarvestableNode>().Configure(ResourceType.Stone, 2, 50f);
            }

            for (int i = 0; i < ironNodeCount; i++)
            {
                Vector3 pos = RandomPoint(half, minDistFromCenter: 30f);
                GameObject vein = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vein.name = "IronVein";
                vein.transform.position = pos + Vector3.up * 0.5f;
                vein.transform.localScale = Vector3.one * 1.2f;
                Tint(vein, new Color(0.55f, 0.35f, 0.25f));
                vein.AddComponent<HarvestableNode>().Configure(ResourceType.Iron, 1, 60f);
            }
        }

        private Vector3 RandomPoint(float half, float minDistFromCenter)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float x = Random.Range(-half, half);
                float z = Random.Range(-half, half);
                if (new Vector2(x, z).magnitude >= minDistFromCenter)
                    return new Vector3(x, GroundY(x, z), z);
            }
            return new Vector3(half / 2f, GroundY(half / 2f, half / 2f), half / 2f);
        }

        private void BakeNavMesh()
        {
            var surface = _terrain.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }

        // ---------- Предметы ----------

        private ItemDefinition MakeItem(string name, ItemRarity rarity, EquipSlot slot,
            float physical = 0, float spell = 0, float armor = 0, string unique = "")
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.displayName = name;
            item.rarity = rarity;
            item.slot = slot;
            item.physicalPower = physical;
            item.spellPower = spell;
            item.armor = armor;
            item.uniqueEffect = unique;
            ItemDatabase.Register(item);
            return item;
        }

        private void CreateItems()
        {
            _rustedBlade = MakeItem("Ржавый клинок", ItemRarity.Common, EquipSlot.Weapon, physical: 5);
            _bogTrinket = MakeItem("Болотный оберег", ItemRarity.Rare, EquipSlot.Amulet, spell: 4, armor: 2);
            _ritualStaff = MakeItem("Посох Гниющего Ритуала", ItemRarity.Epic, EquipSlot.Weapon,
                spell: 14, unique: "Усиливает призванных существ");
            _shepherdMask = MakeItem("Маска Пастыря", ItemRarity.Legendary, EquipSlot.Head,
                armor: 6, unique: "Яд вокруг лечит союзников");

            _boneSword = MakeItem("Костяной меч", ItemRarity.Common, EquipSlot.Weapon, physical: 7);
            _leatherChest = MakeItem("Кожаный доспех", ItemRarity.Common, EquipSlot.Chest, armor: 5);
            _ironBlade = MakeItem("Железный клинок", ItemRarity.Rare, EquipSlot.Weapon, physical: 12);
        }

        // ---------- Шаблоны ----------

        private void CreateTemplates()
        {
            // Пикап предмета.
            GameObject pickupGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickupGo.name = "ItemPickupTemplate";
            pickupGo.transform.localScale = Vector3.one * 0.4f;
            Tint(pickupGo, new Color(1f, 0.85f, 0.3f));
            _pickupTemplate = pickupGo.AddComponent<ItemPickup>();
            pickupGo.SetActive(false);

            // Ядовитое облако.
            GameObject cloudGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloudGo.name = "PoisonCloudTemplate";
            Destroy(cloudGo.GetComponent<Collider>());
            Tint(cloudGo, new Color(0.3f, 0.8f, 0.2f, 0.5f));
            _poisonTemplate = cloudGo.AddComponent<PoisonCloud>();
            cloudGo.SetActive(false);

            // Заражённый.
            GameObject enemyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyGo.name = "InfectedTemplate";
            Tint(enemyGo, new Color(0.7f, 0.2f, 0.2f));
            var agent = enemyGo.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.stoppingDistance = 1.5f;
            var enemyHealth = enemyGo.AddComponent<Health>();
            enemyHealth.SetMax(60f, refill: true);
            _enemyTemplate = enemyGo.AddComponent<EnemyAI>();
            enemyGo.AddComponent<LootDropper>().Configure(_pickupTemplate,
                new LootDropper.LootEntry { item = _rustedBlade, chance = 0.3f },
                new LootDropper.LootEntry { item = _bogTrinket, chance = 0.05f });
            enemyGo.SetActive(false);
        }

        // ---------- Игрок и камера ----------

        private PlayerController SpawnPlayer()
        {
            GameObject playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerGo.name = "Player";
            DestroyImmediate(playerGo.GetComponent<Collider>());
            Tint(playerGo, new Color(0.25f, 0.5f, 0.9f));

            float y = GroundY(0f, 0f);
            playerGo.transform.position = new Vector3(0f, y + 1.2f, 0f);

            var cc = playerGo.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;

            playerGo.AddComponent<PlayerInputReader>();
            playerGo.AddComponent<PlayerStamina>();
            var controller = playerGo.AddComponent<PlayerController>();
            playerGo.AddComponent<Health>();
            playerGo.AddComponent<Inventory>();
            playerGo.AddComponent<ResourceWallet>();
            playerGo.AddComponent<PlayerCombat>();

            var placer = playerGo.AddComponent<BuildPlacer>();
            placer.Configure(BuildCatalog());

            return controller;
        }

        private void CreateCamera(Transform target)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<ThirdPersonCamera>().SetTarget(target);
        }

        // ---------- База ----------

        private List<BuildPlacer.PieceDef> BuildCatalog()
        {
            return new List<BuildPlacer.PieceDef>
            {
                new()
                {
                    Id = "WoodWall", DisplayName = "Деревянная стена",
                    Template = MakeBuildTemplate("WoodWall", new Vector3(2f, 2f, 0.2f),
                        new Color(0.5f, 0.35f, 0.2f)),
                    Cost = new[] { (ResourceType.Wood, 4) }, MaxHealth = 100f
                },
                new()
                {
                    Id = "WoodFloor", DisplayName = "Деревянный пол",
                    Template = MakeBuildTemplate("WoodFloor", new Vector3(2f, 0.15f, 2f),
                        new Color(0.55f, 0.4f, 0.25f)),
                    Cost = new[] { (ResourceType.Wood, 2) }, MaxHealth = 80f
                },
                new()
                {
                    Id = "StoneWall", DisplayName = "Каменная стена",
                    Template = MakeBuildTemplate("StoneWall", new Vector3(2f, 2f, 0.3f),
                        new Color(0.45f, 0.45f, 0.48f)),
                    Cost = new[] { (ResourceType.Stone, 4) }, MaxHealth = 250f
                }
            };
        }

        private GameObject MakeBuildTemplate(string name, Vector3 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"{name}Template";
            go.transform.localScale = size;
            Tint(go, color);
            go.SetActive(false);
            return go;
        }

        private void CreateBase(Vector3 playerPos)
        {
            // Очаг Искры.
            GameObject hearthGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hearthGo.name = "Hearth";
            hearthGo.transform.position = playerPos + new Vector3(3f, -0.7f, 0f);
            hearthGo.transform.localScale = new Vector3(1.5f, 0.5f, 1.5f);
            Tint(hearthGo, new Color(1f, 0.5f, 0.15f));
            var hearth = hearthGo.AddComponent<Hearth>();

            // Кузница (первая крафт-станция из десяти).
            GameObject forgeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            forgeGo.name = "Forge";
            float fx = playerPos.x - 3f, fz = playerPos.z;
            forgeGo.transform.position = new Vector3(fx, GroundY(fx, fz) + 0.5f, fz);
            Tint(forgeGo, new Color(0.3f, 0.3f, 0.35f));
            forgeGo.AddComponent<CraftingStation>().Configure("Кузница (Рорн Угольный)",
                new List<CraftingStation.Recipe>
                {
                    new() { Result = _boneSword, Cost = new[] { (ResourceType.Wood, 5), (ResourceType.Stone, 3) } },
                    new() { Result = _leatherChest, Cost = new[] { (ResourceType.Wood, 8) } },
                    new() { Result = _ironBlade, Cost = new[] { (ResourceType.Iron, 4), (ResourceType.Wood, 2) } }
                });

            // Налёты целятся в очаг.
            var raid = new GameObject("RaidDirector").AddComponent<RaidDirector>();
            raid.Configure(_enemyTemplate, hearth.transform);
        }

        // ---------- Враги и босс ----------

        private void SpawnEnemies()
        {
            float half = _terrain.WorldSize / 2f - 15f;
            for (int i = 0; i < enemyCount; i++)
            {
                Vector3 pos = RandomPoint(half, minDistFromCenter: 35f);
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
                {
                    EnemyAI enemy = Instantiate(_enemyTemplate, hit.position + Vector3.up, Quaternion.identity);
                    enemy.name = "Infected";
                    enemy.gameObject.SetActive(true);
                }
            }
        }

        private void SpawnBoss()
        {
            float half = _terrain.WorldSize / 2f - 25f;
            Vector3 pos = new(half, 0f, half);
            pos.y = GroundY(pos.x, pos.z);
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
                pos = hit.position;

            GameObject bossGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bossGo.name = "RottenShepherd";
            bossGo.transform.position = pos + Vector3.up * 2f;
            bossGo.transform.localScale = new Vector3(2f, 2f, 2f);
            Tint(bossGo, new Color(0.5f, 0.2f, 0.5f));

            var agent = bossGo.AddComponent<NavMeshAgent>();
            agent.speed = 2.5f;
            agent.stoppingDistance = 2f;
            agent.radius = 1f;

            var health = bossGo.AddComponent<Health>();
            health.SetMax(600f, refill: true);

            var boss = bossGo.AddComponent<RottenShepherd>();
            boss.Configure(_poisonTemplate, _enemyTemplate);

            bossGo.AddComponent<LootDropper>().Configure(_pickupTemplate,
                new LootDropper.LootEntry { item = _ritualStaff, guaranteed = true },
                new LootDropper.LootEntry { item = _shepherdMask, chance = 0.5f });
        }

        // ---------- UI и сервисы ----------

        private void CreateHud(PlayerController player)
        {
            var hud = new GameObject("HUD");
            hud.AddComponent<DebugHud>();
            hud.AddComponent<InventoryHud>();
            hud.AddComponent<BossHealthBar>();
            var fog = hud.AddComponent<FogOfWar>();
            fog.Configure(_terrain.WorldSize);
            hud.AddComponent<SaveSystem>();
        }

        private static void Tint(GameObject go, Color color)
        {
            if (go.TryGetComponent(out Renderer renderer))
                renderer.material.color = color;
        }
    }
}

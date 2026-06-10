using System;
using System.Collections.Generic;
using System.IO;
using Ashveil.Building;
using Ashveil.Crafting;
using Ashveil.Items;
using Ashveil.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashveil.SaveLoad
{
    /// <summary>
    /// Сохранения (GDD §8): мир хранится в JSON у хоста, прогресс базы — всегда.
    /// F5 — сохранить. Загрузка автоматическая при старте (если сейв есть).
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [Serializable]
        public class PieceData
        {
            public string id;
            public Vector3 position;
            public float yaw;
        }

        [Serializable]
        public class SaveData
        {
            public int seed;
            public int wood;
            public int stone;
            public int iron;
            public int bossEssence;
            public List<string> bagItems = new();
            public List<PieceData> pieces = new();
        }

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "ashveil_world.json");

        private string _toast = string.Empty;
        private float _toastUntil;

        public static SaveData LoadOrNull()
        {
            if (!File.Exists(SavePath))
                return null;

            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Не удалось прочитать сейв: {e.Message}");
                return null;
            }
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }

        public void Save(int seed)
        {
            var data = new SaveData { seed = seed };

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                if (player.TryGetComponent(out ResourceWallet wallet))
                {
                    data.wood = wallet.Get(ResourceType.Wood);
                    data.stone = wallet.Get(ResourceType.Stone);
                    data.iron = wallet.Get(ResourceType.Iron);
                    data.bossEssence = wallet.Get(ResourceType.BossEssence);
                }

                if (player.TryGetComponent(out Inventory inventory))
                {
                    foreach (ItemDefinition item in inventory.Items)
                        data.bagItems.Add(item.displayName);
                }
            }

            foreach (BuiltPiece piece in BuiltPiece.All)
            {
                data.pieces.Add(new PieceData
                {
                    id = piece.PieceId,
                    position = piece.transform.position,
                    yaw = piece.transform.eulerAngles.y
                });
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
            ShowToast($"Сохранено: {SavePath}");
        }

        /// <summary>Восстановление состояния игрока и базы (вызывает GameBootstrap).</summary>
        public static void Restore(SaveData data, PlayerController player, BuildPlacer placer)
        {
            if (data == null || player == null)
                return;

            if (player.TryGetComponent(out ResourceWallet wallet))
            {
                wallet.Add(ResourceType.Wood, data.wood);
                wallet.Add(ResourceType.Stone, data.stone);
                wallet.Add(ResourceType.Iron, data.iron);
                wallet.Add(ResourceType.BossEssence, data.bossEssence);
            }

            if (player.TryGetComponent(out Inventory inventory))
            {
                foreach (string itemName in data.bagItems)
                {
                    ItemDefinition item = ItemDatabase.Find(itemName);
                    if (item != null)
                        inventory.Add(item);
                }
            }

            if (placer != null)
            {
                foreach (PieceData piece in data.pieces)
                    placer.PlaceFromSave(piece.id, piece.position, piece.yaw);
            }
        }

        public int CurrentSeed { get; set; }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
                Save(CurrentSeed);
        }

        private void ShowToast(string message)
        {
            _toast = message;
            _toastUntil = Time.time + 3f;
        }

        private void OnGUI()
        {
            if (Time.time < _toastUntil)
                GUI.Label(new Rect(20, Screen.height - 30, Screen.width - 40, 24), _toast);
        }
    }
}

using System.Collections.Generic;
using Ashveil.Combat;
using UnityEngine;

namespace Ashveil.Building
{
    /// <summary>
    /// Установленный элемент базы. Имеет прочность (налёты могут ломать постройки),
    /// регистрируется в общем списке для сохранения мира (GDD §8: мир у хоста, JSON).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class BuiltPiece : MonoBehaviour
    {
        public static readonly List<BuiltPiece> All = new();

        public string PieceId { get; private set; }

        public void Init(string pieceId)
        {
            PieceId = pieceId;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            GetComponent<Health>().Died += () => Destroy(gameObject);
        }
    }
}

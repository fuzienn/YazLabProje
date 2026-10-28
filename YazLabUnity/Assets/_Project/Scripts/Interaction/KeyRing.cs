using System;
using System.Collections.Generic;

namespace DungeonEscape.Interaction
{
    /// <summary>
    /// IKeyHolder'in MonoBehaviour'dan bagimsiz uygulamasi. Oyuncu, muhafiz ve fare bunu icinde tutar.
    /// </summary>
    public class KeyRing : IKeyHolder
    {
        private readonly HashSet<string> _keys = new();

        public event Action<string> KeyAdded;
        public event Action<string> KeyRemoved;

        public int Count => _keys.Count;

        public bool HasKey(string keyId) => !string.IsNullOrEmpty(keyId) && _keys.Contains(keyId);

        public void AddKey(string keyId)
        {
            if (string.IsNullOrEmpty(keyId) || !_keys.Add(keyId)) return;
            KeyAdded?.Invoke(keyId);
        }

        public bool RemoveKey(string keyId)
        {
            if (string.IsNullOrEmpty(keyId) || !_keys.Remove(keyId)) return false;
            KeyRemoved?.Invoke(keyId);
            return true;
        }

        /// <summary>Anahtari bir sahipten digerine aktarir (calma, teslim etme).</summary>
        public static bool Transfer(IKeyHolder from, IKeyHolder to, string keyId)
        {
            if (from == null || to == null || !from.RemoveKey(keyId)) return false;
            to.AddKey(keyId);
            return true;
        }
    }
}

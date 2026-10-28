using System;

namespace DungeonEscape.Interaction
{
    /// <summary>Anahtar tasiyabilen her sey: oyuncu envanteri, muhafiz, fare.</summary>
    public interface IKeyHolder
    {
        bool HasKey(string keyId);
        void AddKey(string keyId);
        bool RemoveKey(string keyId);
        event Action<string> KeyAdded;
        event Action<string> KeyRemoved;
    }
}

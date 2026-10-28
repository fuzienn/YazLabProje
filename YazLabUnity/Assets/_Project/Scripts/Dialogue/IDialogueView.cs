using System;

namespace DungeonEscape.Dialogue
{
    public enum Speaker
    {
        Player,
        Warden,
        System
    }

    /// <summary>Diyalog ekrani. Mantik icermez; controller ne gosterecegini soyler.</summary>
    public interface IDialogueView
    {
        event Action<string> Submitted;
        event Action CloseRequested;

        void Open();
        void Close();
        void AddLine(Speaker speaker, string text);
        void SetThinking(bool thinking);
        void SetFear(int level, int threshold);
        void ShowError(string message);
        void SetInputEnabled(bool enabled);
    }
}

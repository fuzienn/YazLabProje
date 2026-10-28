namespace DungeonEscape.Companion
{
    /// <summary>Oyuncudan emir alabilen yoldas.</summary>
    public interface ICommandable
    {
        bool CanAcceptCommand { get; }
        string Status { get; }
        void Command();
    }
}

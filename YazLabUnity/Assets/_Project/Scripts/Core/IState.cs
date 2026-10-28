namespace DungeonEscape.Core
{
    /// <summary>Sonlu durum makinesindeki tek bir durum.</summary>
    public interface IState
    {
        string Name { get; }
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}

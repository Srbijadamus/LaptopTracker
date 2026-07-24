namespace LaptopTracker.Services
{
    public interface ICurrentUser
    {
        string? Kid { get; }
        bool IsIdentified { get; }
    }
}

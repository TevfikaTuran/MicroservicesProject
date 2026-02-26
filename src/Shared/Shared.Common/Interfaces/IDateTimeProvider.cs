namespace Shared.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

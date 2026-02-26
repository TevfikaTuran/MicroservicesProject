using Shared.Common.Interfaces;

namespace Shared.Common.Extensions;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

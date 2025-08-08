namespace Domain.Enums
{
    /// <summary>
    /// Enumeration of possible order statuses.
    /// </summary>
    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }
}
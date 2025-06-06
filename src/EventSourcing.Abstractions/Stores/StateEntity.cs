namespace EventSourcing.Contexts;

public class StateEntity
{
    public Guid StreamId { get; set; }
    public string? Data { get; set; }
}
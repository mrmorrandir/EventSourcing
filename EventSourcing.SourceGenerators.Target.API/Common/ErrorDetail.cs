namespace EventSourcing.SourceGenerators.Target.API.Common;

public class ErrorDetail
{
    public string Message { get; set; } = "Error";
    public List<ErrorDetail> CausedBy { get; set; } = new();
}
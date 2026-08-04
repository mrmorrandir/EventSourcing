namespace EventSourcing.SourceGenerators.Target.API.Common;


public class ErrorResponse
{
    public List<ErrorDetail> Errors { get; set; } = new();
}
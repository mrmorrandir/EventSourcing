using FluentResults;

namespace EventSourcing.SourceGenerators.Target.API.Common;

public static class ResultExtensions
{
    public static async Task<IResult> ToWebResult<T>(this Task<Result<T>> resultTask)
    {
        var result = await resultTask;
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return Results.BadRequest(result.GetResponse());
    }
    
    public static async Task<IResult> ToWebResult(this Task<Result> resultTask)
    {
        var result = await resultTask;
        if (result.IsSuccess)
            return Results.Ok();

        return Results.BadRequest(result.GetResponse());
    }
   
    public static async ValueTask<IResult> ToWebResult<T>(this ValueTask<Result<T>> resultValueTask)
    {
        var result = await resultValueTask;
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return Results.BadRequest(result.GetResponse());
    }
    
    public static async ValueTask<IResult> ToWebResult(this ValueTask<Result> resultValueTask)
    {
        var result = await resultValueTask;
        if (result.IsSuccess)
            return Results.Ok();

        return Results.BadRequest(result.GetResponse());
    }

    public static ErrorResponse GetResponse(this ResultBase resultBase)
    {
        return new ErrorResponse
        {
            Errors = resultBase.Errors.Select(e => e.GetErrorDetail()).ToList()
        };
    }

    public static ErrorDetail GetErrorDetail(this IError error)
    {
        return new ErrorDetail
        {
            Message = error.Message,
            CausedBy = error.Reasons.Select(r => r.GetErrorDetail()).ToList()
        };
    }
    
}
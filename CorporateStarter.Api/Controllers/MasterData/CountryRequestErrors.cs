using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace CorporateStarter.Api.Controllers.MasterData;

public sealed class CountryRequestErrors : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var status = context.Exception switch { UnauthorizedAccessException => 403, ArgumentException => 400, _ => 0 };
        if (status == 0) return;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = status == 403 ? "Operation is not permitted." : context.Exception.Message
        })
        { StatusCode = status };
        context.ExceptionHandled = true;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CorporateStarter.Api.Infrastructure.Auth
{
    public sealed class AuthOperationContentionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not AuthOperationContentionException ||
                context.HttpContext.Response.HasStarted)
            {
                return;
            }

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Authentication service is temporarily busy."
            };

            problem.Extensions["errorCode"] = "auth.concurrent_conflict";
            problem.Extensions["traceId"] =
                context.HttpContext.TraceIdentifier;

            var result = new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };

            result.ContentTypes.Add("application/problem+json");

            context.HttpContext.Response.Headers.RetryAfter = "1";
            context.Result = result;
            context.ExceptionHandled = true;
        }
    }
}

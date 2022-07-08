using СourseworkBackend.CustomAttributes;
using СourseworkBackend.Models;

namespace СourseworkBackend.Middlewares
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;
        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {

            Endpoint? endpoint = context.GetEndpoint();
            if (endpoint != null && endpoint.Metadata.OfType<ValidSessionRequiredAttribute>().FirstOrDefault() != null)
            {
                if (!context.Request.Headers.ContainsKey("SessionToken"))
                {
                    context.Response.StatusCode = 401;
                    return;
                }

                var authToken = context.Request.Headers["SessionToken"];

                Session? session = GlobalScope.GetSession(authToken);

                SessionStatus sessionStatus = GlobalScope.GetSessionStatus(session);


                if (session == null || sessionStatus.HasFlag(SessionStatus.Invalid))
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                if (sessionStatus.HasFlag(SessionStatus.Expired))
                {
                    context.Response.StatusCode = 419;
                    return;
                }

                context.Request.RouteValues.Add("Session", session);
            }

            await _next(context);
        }

    }
}

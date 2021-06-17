using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployee.Filters.ActionFilters
{
    public class ValidationFilterAttribute : IActionFilter
    {
        private readonly ILoggerManager _loggerManager;

        public ValidationFilterAttribute(ILoggerManager loggerManager)
        {
            _loggerManager = loggerManager;
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var action = context.RouteData.Values["action"];
            var controller = context.RouteData.Values["controller"];

            var parameterTypes = context.ActionArguments.Values;
            var param = parameterTypes.SingleOrDefault(x => x.GetType().Name.Contains("Dto"));
            if (param == null)
            {
                _loggerManager.LogError($"Object sent from client is null. Controller: {controller}, action: {action}");
                context.Result = new BadRequestObjectResult($"Object is null. Controller: {controller}, action: {action}"); return;
            }
            if (context.ModelState.IsValid) return;
            
            _loggerManager.LogError($"Invalid model state for the object. Controller: {controller}, action: {action}");
            context.Result = new UnprocessableEntityObjectResult(context.ModelState);
        }
    }
}

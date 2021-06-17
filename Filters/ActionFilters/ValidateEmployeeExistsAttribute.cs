using System;
using System.Threading.Tasks;
using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompanyEmployee.Filters.ActionFilters
{
    public class ValidateEmployeeExistsAttribute : IAsyncActionFilter
    {
        private readonly ILoggerManager _logger;
        private readonly IRepositoryManager _repositoryManager;

        public ValidateEmployeeExistsAttribute(ILoggerManager logger,
            IRepositoryManager repositoryManager)
        {
            _logger = logger;
            _repositoryManager = repositoryManager;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpMethod = context.HttpContext.Request.Method;
            var trackChanges = httpMethod.Equals("put", StringComparison.InvariantCultureIgnoreCase) 
                               || httpMethod.Equals("patch", StringComparison.InvariantCultureIgnoreCase);
            var companyId = (Guid) context.ActionArguments["companyId"];
            var employeeId = (Guid) context.ActionArguments["employeeId"];

            var company = await _repositoryManager.CompanyRepository.GetCompany(companyId, false);

            if (company == null) 
            { 
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database."); 
                context.Result = new NotFoundResult();
                return;
            }

            var employee = await _repositoryManager.EmployeeRepository
                .GetEmployee(companyId, employeeId, trackChanges);

            if (employee == null)
            {
                _logger.LogInfo($"Employee with id: {employeeId} doesn't exist in the database.");
                context.Result = new NotFoundResult();
            }
            else
            {
                context.HttpContext.Items.Add("employee", employee); 
                await next();
            }

        }
    }
}

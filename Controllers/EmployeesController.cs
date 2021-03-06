using AutoMapper;
using CompanyEmployee.ModelBinders;
using Contracts;
using Entities.DTOs;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyEmployee.Filters.ActionFilters;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;

namespace CompanyEmployee.Controllers
{
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "v1")]
    [Route("/api/companies/{companyId:guid}/employees")]
    [ApiController]
    [ResponseCache(CacheProfileName = "120secondsProfile")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IDataShaper<EmployeeDto> _dataShaper;

        public EmployeesController(ILoggerManager logger, IMapper mapper,
            IRepositoryManager repositoryManager, IDataShaper<EmployeeDto> dataShaper)
        {
            _logger = logger;
            _mapper = mapper;
            _repositoryManager = repositoryManager;
            _dataShaper = dataShaper;
        }

        [HttpGet]
        [HttpHead]
        [ServiceFilter(typeof(ValidateCompanyExistsAttribute))]
        public async Task<IActionResult> GetEmployees(Guid companyId,
            [FromQuery]EmployeeRequestParameters requestParameters)
        {
            if (!requestParameters.ValidAgeRange) return BadRequest("Max age can't be less than min age");

            var companyEmployees = await _repositoryManager
                .EmployeeRepository.GetEmployeesAsync(companyId, requestParameters, false);

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(companyEmployees.MetaData));

            var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(companyEmployees);

            return Ok(_dataShaper.ShapeData(employeeDtos, requestParameters.Fields));
        }

        [HttpGet("{employeeId:guid}", Name = "GetEmployeeForCompany")]
        [ResponseCache(Duration = 80)]
        [ServiceFilter(typeof(ValidateEmployeeExistsAttribute))]
        public IActionResult GetEmployee(Guid companyId, Guid employeeId)
        {
            var employee = HttpContext.Items["employeeId"] as Employee;

            return Ok(_mapper.Map<EmployeeDto>(employee));

        }

        [HttpGet("collection/({employeeIds})", Name = "GetEmployeesByIds")]
        [ServiceFilter(typeof(ValidateCompanyExistsAttribute))]
        public async Task<IActionResult> GetEmployeesById(
            [ModelBinder(BinderType = typeof(IdsModelBinder))] IEnumerable<Guid> employeeIds, Guid companyId)
        {
            var ids = employeeIds as Guid[] ?? employeeIds.ToArray();
            var employees = await _repositoryManager.EmployeeRepository.GetEmployeesByIds(companyId, false, ids);

            if (ids.Length != employees.Count())
            {
                _logger.LogInfo("Some of the employee ids are invalid");
                return NotFound();
            }

            var employeesToReturn = _mapper.Map<IEnumerable<EmployeeDto>>(employees);

            return Ok(employeesToReturn);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidateCompanyExistsAttribute), Order = 1)]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> CreateEmployeeForCompany(Guid companyId, [FromBody] EmployeeForCreationDto employee)
        {
            var employeeEntity = _mapper.Map<Employee>(employee);
            _repositoryManager.EmployeeRepository.CreateEmployeeForCompany(companyId, employeeEntity);
            await _repositoryManager.SaveChanges();
            var employeeToReturn = _mapper.Map<EmployeeDto>(employeeEntity);

            return CreatedAtRoute("GetEmployeeForCompany", new { companyId, employeeId = employeeToReturn.Id },
                employeeToReturn);
        }

        [HttpPost("collection")]
        [ServiceFilter(typeof(ValidateCompanyExistsAttribute), Order = 1)]
        public async Task<IActionResult> CreateCollectionOfEmployeesForCompany(Guid companyId,
           [FromBody] IEnumerable<EmployeeForCreationDto> employeeForCreationDtos)
        {
            if (!employeeForCreationDtos.Any())
            {
                _logger.LogError("the employee collection provided is either empty or null");
                return BadRequest("the employee collection provided is either empty or null");
            }
            var employeesToBeCreated = _mapper.Map<IEnumerable<Employee>>(employeeForCreationDtos);

            foreach (var employee in employeesToBeCreated)
            {
                TryValidateModel(employee);
                if (!ModelState.IsValid)
                    return UnprocessableEntity(ModelState);
                _repositoryManager.EmployeeRepository.CreateEmployeeForCompany(companyId, employee);
            }

            await _repositoryManager.SaveChanges();

            var employeesToReturn = _mapper.Map<IEnumerable<EmployeeDto>>(employeesToBeCreated);

            var employeeIds = string.Join(",", employeesToReturn.Select(e => e.Id));

            return CreatedAtRoute("GetEmployeesByIds",
                new { companyId, employeeIds }, employeesToReturn);
        }

        [HttpDelete("{employeeId:guid}")]
        [Authorize(Roles = "Administrator")]
        [ServiceFilter(typeof(ValidateEmployeeExistsAttribute))]
        public async Task<IActionResult> DeleteEmployee(Guid companyId, Guid employeeId)
        {
            var employeeToDelete = HttpContext.Items["employee"] as Employee;

            _repositoryManager.EmployeeRepository.DeleteEmployeeFromCompany(employeeToDelete);
            await _repositoryManager.SaveChanges();

            return NoContent();
        }

        [HttpPut("{employeeId:guid}")]
        [Authorize(Roles = "Administrator")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ServiceFilter(typeof(ValidateEmployeeExistsAttribute))]
        public async Task<IActionResult> UpdateEmployee(Guid employeeId,
            Guid companyId, [FromBody] EmployeeForUpdatingDto employeeForUpdatingDto)
        {
            var employeeToUpdate = HttpContext.Items["employee"] as Employee;
                _mapper.Map(employeeForUpdatingDto, employeeToUpdate);
            await _repositoryManager.SaveChanges();

            return NoContent();
        }

        [HttpPatch("{employeeId:guid}")]
        [Authorize(Roles = "Administrator")]
        [ServiceFilter(typeof(ValidateEmployeeExistsAttribute))]
        public async Task<IActionResult> PartiallyUpdateEmployee(Guid companyId, Guid employeeId,
            [FromBody]JsonPatchDocument<EmployeeForUpdatingDto> patchDoc)
        {
            if (patchDoc == null)
            {
                _logger.LogError("patchDoc object sent from client is null.");
                return BadRequest("patchDoc object is null");
            }

            var employeeEntity = HttpContext.Items["employee"] as Employee;
            var empToPatch = _mapper.Map<EmployeeForUpdatingDto>(employeeEntity);
            
            patchDoc.ApplyTo(empToPatch);
            TryValidateModel(empToPatch);
            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid model state for the patch document");
                return UnprocessableEntity(ModelState);
            }
            _mapper.Map(empToPatch, employeeEntity);
            await _repositoryManager.SaveChanges();
            return NoContent();
        }
    }
}
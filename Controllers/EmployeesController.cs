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
using Microsoft.AspNetCore.JsonPatch;

namespace CompanyEmployee.Controllers
{
    [Route("/api/companies/{companyId:guid}/employees")]
    [ApiController]
    [ServiceFilter(typeof(ValidateCompanyExistsAttribute))]
    public class EmployeesController : ControllerBase
    {
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        private readonly IRepositoryManager _repositoryManager;

        public EmployeesController(ILoggerManager logger, IMapper mapper,
            IRepositoryManager repositoryManager)
        {
            _logger = logger;
            _mapper = mapper;
            _repositoryManager = repositoryManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees(Guid companyId)
        {
            var companyEmployees = await _repositoryManager
                .EmployeeRepository.GetEmployees(companyId, false);

            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(companyEmployees));
        }

        [HttpGet("{employeeId:guid}", Name = "GetEmployeeForCompany")]
        public async Task<IActionResult> GetEmployee(Guid companyId, Guid employeeId)
        {
            var company = HttpContext.Items["company"] as Company;
            var employee = await _repositoryManager.EmployeeRepository.GetEmployee(companyId, employeeId, false);
            if (employee != null) return Ok(_mapper.Map<EmployeeDto>(employee));

            _logger.LogInfo($"the employee with id {employeeId} does not exist in the database");
            return NotFound();

        }

        [HttpGet("collection/({employeeIds})", Name = "GetEmployeesByIds")]
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
        [ServiceFilter(typeof(ValidationFilterAttribute))]
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
        public async Task<IActionResult> DeleteEmployee(Guid companyId, Guid employeeId)
        {
            var employeeToDelete = await _repositoryManager.EmployeeRepository
                .GetEmployee(companyId, employeeId, false);

            if (employeeToDelete == null)
            {
                _logger.LogError($"The employee with the id {employeeId} does not exist");
                return NotFound($"the employee with the {employeeId} does not exist");
            }

            _repositoryManager.EmployeeRepository.DeleteEmployeeFromCompany(employeeToDelete);
            await _repositoryManager.SaveChanges();

            return NoContent();
        }

        [HttpPut("{employeeId:guid}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> UpdateEmployee(Guid employeeId,
            Guid companyId, [FromBody] EmployeeForUpdatingDto employeeForUpdatingDto)
        {
            var employeeToUpdate = await _repositoryManager.EmployeeRepository.GetEmployee(companyId, employeeId, true);

            if (employeeToUpdate == null)
            {
                _logger.LogInfo($"employee with id {employeeId} does not exist");
                return NotFound();
            }

            _mapper.Map(employeeForUpdatingDto, employeeToUpdate);
            await _repositoryManager.SaveChanges();

            return NoContent();
        }

        [HttpPatch("{employeeId:guid}")]
        public async Task<IActionResult> PartiallyUpdateEmployee(Guid companyId, Guid employeeId,
            [FromBody]JsonPatchDocument<EmployeeForUpdatingDto> patchDoc)
        {
            if (patchDoc == null)
            {
                _logger.LogError("patchDoc object sent from client is null.");
                return BadRequest("patchDoc object is null");
            } 
            var employeeEntity = await _repositoryManager.EmployeeRepository.GetEmployee(companyId, employeeId, true);
            if (employeeEntity == null)
            {
                _logger.LogInfo($"Employee with id: {employeeId} doesn't exist in the database.");
                return NotFound();
            }

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
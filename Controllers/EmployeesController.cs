using AutoMapper;
using CompanyEmployee.ModelBinders;
using Contracts;
using Entities.DTOs;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompanyEmployee.Controllers
{
    [Route("/api/companies/{companyId:guid}/employees")]
    [ApiController]
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
        public IActionResult GetEmployees(Guid companyId)
        {
            var company = _repositoryManager.CompanyRepository.GetCompany(companyId, false);
            if (company == null)
            {
                _logger.LogInfo($"company with the id {companyId} does not exist in the database");
                return NotFound();
            }

            var companyEmployees = _repositoryManager
                .EmployeeRepository.GetEmployees(companyId, false);

            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(companyEmployees));
        }

        [HttpGet("{employeeId:guid}", Name = "GetEmployeeForCompany")]
        public IActionResult GetEmployee(Guid companyId, Guid employeeId)
        {
            var company = _repositoryManager.CompanyRepository.GetCompany(companyId, false);
            if (company == null)
            {
                _logger.LogInfo($"company with the id {companyId} does not exist in the database");
                return NotFound();
            }

            var employee = _repositoryManager.EmployeeRepository.GetEmployee(companyId, employeeId, false);
            if (employee != null) return Ok(_mapper.Map<EmployeeDto>(employee));

            _logger.LogInfo($"the employee with id {employeeId} does not exist in the database");
            return NotFound();

        }

        [HttpGet("collection/({employeeIds})", Name = "GetEmployeesByIds")]
        public IActionResult GetEmployeesById(
            [ModelBinder(BinderType = typeof(IdsModelBinder))] IEnumerable<Guid> employeeIds, Guid companyId)
        {
            var company = _repositoryManager.CompanyRepository.GetCompany(companyId, trackChanges: false);

            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }

            var ids = employeeIds as Guid[] ?? employeeIds.ToArray();

            var employees = _repositoryManager.EmployeeRepository.GetEmployeesByIds(companyId, false, ids);

            if (ids.Length != employees.Count())
            {
                _logger.LogInfo("Some of the employee ids are invalid");
                return NotFound();
            }

            var employeesToReturn = _mapper.Map<IEnumerable<EmployeeDto>>(employees);

            return Ok(employeesToReturn);
        }

        [HttpPost]
        public IActionResult CreateEmployeeForCompany(Guid companyId, [FromBody] EmployeeForCreationDto employee)
        {
            if (employee == null)
            {
                _logger.LogError("EmployeeForCreationDto object sent from client is null.");
                return BadRequest("EmployeeForCreationDto object is null");
            }

            var company = _repositoryManager.CompanyRepository.GetCompany(companyId, trackChanges: false);

            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }

            var employeeEntity = _mapper.Map<Employee>(employee);
            _repositoryManager.EmployeeRepository.CreateEmployeeForCompany(companyId, employeeEntity);
            _repositoryManager.SaveChanges();
            var employeeToReturn = _mapper.Map<EmployeeDto>(employeeEntity);

            return CreatedAtRoute("GetEmployeeForCompany", new { companyId, employeeId = employeeToReturn.Id },
                employeeToReturn);
        }

        [HttpPost("collection")]
        public IActionResult CreateCollectionOfEmployeesForCompany(Guid companyId,
           [FromBody] IEnumerable<EmployeeForCreationDto> employeeForCreationDtos)
        {
            if (!employeeForCreationDtos.Any())
            {
                _logger.LogError("the employee collection provided is either empty or null");
                return BadRequest("the employee collection provided is either empty or null");
            }

            var company = _repositoryManager.CompanyRepository.GetCompany(companyId, false);
            if (company == null)
            {
                _logger.LogError($"The company with the id {companyId} does not exist");
                return NotFound($"The company with the id {companyId} does not exist");
            }

            var employeesToBeCreated = _mapper.Map<IEnumerable<Employee>>(employeeForCreationDtos);

            foreach (var employee in employeesToBeCreated)
                _repositoryManager.EmployeeRepository.CreateEmployeeForCompany(companyId, employee);

            _repositoryManager.SaveChanges();

            var employeesToReturn = _mapper.Map<IEnumerable<EmployeeDto>>(employeesToBeCreated);

            var employeeIds = string.Join(",", employeesToReturn.Select(e => e.Id));

            return CreatedAtRoute("GetEmployeesByIds",
                new {companyId, employeeIds}, employeesToReturn);
        }

        [HttpDelete("{employeeId:guid}")]
        public IActionResult DeleteEmployee(Guid companyId, Guid employeeId)
        {
            var company = _repositoryManager.CompanyRepository.GetCompany(companyId, false);
            if (company == null)
            {
                _logger.LogError($"the company with id {companyId} does not exist in the database");
                return NotFound($"the company with the id {companyId} does not exist");
            }

            var employeeToDelete = _repositoryManager.EmployeeRepository
                .GetEmployee(companyId, employeeId, false);

            if (employeeToDelete == null)
            {
                _logger.LogError($"The employee with the id {employeeId} does not exist");
                return NotFound($"the employee with the {employeeId} does not exist");
            }

            _repositoryManager.EmployeeRepository.DeleteEmployeeFromCompany(employeeToDelete);
            _repositoryManager.SaveChanges();

            return NoContent();
        }
    }
}
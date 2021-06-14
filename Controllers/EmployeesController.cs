using System;
using System.Collections.Generic;
using AutoMapper;
using Contracts;
using Entities.DTOs;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("{employeeId:guid}")]
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
    }
}
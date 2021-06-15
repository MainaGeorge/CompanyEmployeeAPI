using AutoMapper;
using Contracts;
using Entities.DTOs;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace CompanyEmployee.Controllers
{
    [Route("api/companies")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly ILoggerManager _loggerManager;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IMapper _mapper;

        public CompaniesController(ILoggerManager loggerManager,
            IRepositoryManager repositoryManager, IMapper mapper)
        {
            _loggerManager = loggerManager;
            _repositoryManager = repositoryManager;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetCompanies()
        {
            var companies = _repositoryManager.CompanyRepository.GetAllCompanies(false);
            var companiesDto = _mapper.Map<IEnumerable<CompanyDto>>(companies);

            return Ok(companiesDto);
        }

        [HttpGet("{companyId:guid}", Name = "CompanyById")]
        public IActionResult GetCompany(Guid companyId)
        {
            var company = _repositoryManager.CompanyRepository.GetCompany(companyId, false);
            if (company != null) return Ok(_mapper.Map<CompanyDto>(company));

            _loggerManager.LogInfo($"company with id {companyId} does not exist in the database");
            return NotFound();

        }

        [HttpPost]
        public IActionResult CreateCompany([FromBody]CompanyForCreationDto companyForCreationDto)
        {
            if (companyForCreationDto == null)
            {
                _loggerManager.LogError("The company object provided is null");
                return BadRequest("The company dto provided is null");
            }

            var companyEntity = _mapper.Map<Company>(companyForCreationDto);
            _repositoryManager.CompanyRepository.CreateCompany(companyEntity);
            _repositoryManager.SaveChanges();

            var companyToReturn = _mapper.Map<CompanyDto>(companyEntity);

            return CreatedAtRoute("CompanyById", new { companyId = companyEntity.Id }, companyToReturn);
        }
    }
}

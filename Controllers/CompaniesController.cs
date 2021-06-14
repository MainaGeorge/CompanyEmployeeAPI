using AutoMapper;
using Contracts;
using Entities.DTOs;
using Microsoft.AspNetCore.Mvc;
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
    }
}

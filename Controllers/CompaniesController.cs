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
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _repositoryManager.CompanyRepository.GetAllCompanies(false);
            var companiesDto = _mapper.Map<IEnumerable<CompanyDto>>(companies);

            return Ok(companiesDto);
        }

        [HttpGet("{companyId:guid}", Name = "CompanyById")]
        [ServiceFilter(typeof(ValidateCompanyExistsAttribute))]
        public IActionResult GetCompany(Guid companyId)
        {
            var company = HttpContext.Items["company"] as Company;

            return Ok(_mapper.Map<CompanyDto>(company));
        }

        [HttpGet("collection/({ids})", Name = "CompanyCollection")]
        public async Task<IActionResult> GetCompaniesByIds([ModelBinder(BinderType = typeof(IdsModelBinder))] IEnumerable<Guid> ids)
        {
            var idArray = ids as Guid[] ?? ids.ToArray();
            if (idArray.Length < 1)
            {
                _loggerManager.LogError("The parameter ids is null or empty");
                return BadRequest("The parameter ids is null or empty");
            }

            var companies = await _repositoryManager
                .CompanyRepository.GetCompaniesFromIds(idArray, false);

            if (companies.Count() != idArray.Length)
            {
                _loggerManager.LogError("Some ids are not valid");
                return NotFound();
            }

            var companiesToReturn = _mapper.Map<IEnumerable<CompanyDto>>(companies);

            return Ok(companiesToReturn);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> CreateCompany([FromBody] CompanyForCreationDto companyForCreationDto)
        {
            var companyEntity = _mapper.Map<Company>(companyForCreationDto);
            _repositoryManager.CompanyRepository.CreateCompany(companyEntity);
            await _repositoryManager.SaveChanges();

            var companyToReturn = _mapper.Map<CompanyDto>(companyEntity);

            return CreatedAtRoute("CompanyById", new { companyId = companyEntity.Id }, companyToReturn);
        }

        [HttpPost("collection")]
        public async Task<IActionResult> CreateCompanyCollection([FromBody] IEnumerable<CompanyForCreationDto> companies)
        {
            if (!companies.Any())
            {
                _loggerManager.LogError("The collection of companies is null or empty");
                return BadRequest("The collection of companies is null or empty");
            }

            var companyEntities = _mapper.Map<IEnumerable<Company>>(companies);
            foreach (var company in companyEntities)
            {
                TryValidateModel(company);
                if (!ModelState.IsValid)
                    return UnprocessableEntity(ModelState);
                _repositoryManager.CompanyRepository.CreateCompany(company);
            }
            await _repositoryManager.SaveChanges();

            var companiesToReturn = _mapper.Map<IEnumerable<CompanyDto>>(companyEntities);
            var ids = string.Join(",", companiesToReturn.Select(c => c.Id));

            return CreatedAtRoute("CompanyCollection", new { ids }, companiesToReturn);
        }

        [HttpDelete("{companyId:guid}")]
        [ServiceFilter(typeof(ValidateCompanyExistsAttribute))]
        public async Task<IActionResult> DeleteCompany(Guid companyId)
        {
            var company = HttpContext.Items["company"] as Company;
            _repositoryManager.CompanyRepository.DeleteCompany(company);
            await _repositoryManager.SaveChanges();

            return NoContent();
        }

        [HttpPut("{companyId:guid}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> UpdateCompany(Guid companyId, CompanyForUpdateDto companyForUpdateDto)
        {
            var company = HttpContext.Items["company"] as Company;
            _mapper.Map(companyForUpdateDto, company);
            await _repositoryManager.SaveChanges();

            return NoContent();
        }

    }
}

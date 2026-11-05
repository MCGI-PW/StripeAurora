using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPet.Payments.Controllers;

/// <summary>CRUD completo da tabela TB_CLINIC.</summary>
[ApiController]
[Authorize]
[Route("api/clinics")]
[Produces("application/json")]
public class ClinicsController : ControllerBase
{
    private readonly IClinicService _service;

    public ClinicsController(IClinicService service) => _service = service;

    /// <summary>Consulta todas as clinicas cadastradas.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClinicResponse>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    /// <summary>Consulta uma clinica pelo identificador.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicResponse>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetByIdAsync(id, ct));
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Clinica nao encontrada", Detail = ex.Message, Status = 404 });
        }
    }

    /// <summary>Inclui uma nova clinica.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClinicResponse>> Create([FromBody] ClinicRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new ProblemDetails { Title = "Clinica duplicada", Detail = ex.Message, Status = 409 });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Clinica invalida", Detail = ex.Message, Status = 400 });
        }
    }

    /// <summary>Altera os dados cadastrais de uma clinica.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicResponse>> Update(Guid id, [FromBody] ClinicUpdateRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, request, ct));
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Clinica nao encontrada", Detail = ex.Message, Status = 404 });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Clinica invalida", Detail = ex.Message, Status = 400 });
        }
    }

    /// <summary>Exclui uma clinica que ainda nao possua assinaturas.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Clinica nao encontrada", Detail = ex.Message, Status = 404 });
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new ProblemDetails { Title = "Exclusao bloqueada", Detail = ex.Message, Status = 409 });
        }
    }
}

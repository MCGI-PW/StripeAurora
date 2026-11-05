using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Application.Services;
using AuroraPet.Payments.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPet.Payments.Controllers;

/// <summary>CRUD completo dos planos. Leitura: qualquer usuario autenticado; escrita: somente Admin.</summary>
[ApiController]
[Authorize]
[Route("api/plans")]
[Produces("application/json")]
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _service;

    public SubscriptionPlansController(ISubscriptionPlanService service) => _service = service;

    /// <summary>Consulta todos os planos cadastrados.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanResponse>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    /// <summary>Consulta um plano pelo identificador.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionPlanResponse>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetByIdAsync(id, ct));
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Plano nao encontrado", Detail = ex.Message, Status = 404 });
        }
    }

    /// <summary>Inclui um novo plano.</summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionPlanResponse>> Create([FromBody] SubscriptionPlanRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return BadRequest(new ProblemDetails { Title = "Plano invalido", Detail = ex.Message, Status = 400 });
        }
    }

    /// <summary>Altera um plano existente.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionPlanResponse>> Update(Guid id, [FromBody] SubscriptionPlanRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, request, ct));
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Plano nao encontrado", Detail = ex.Message, Status = 404 });
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return BadRequest(new ProblemDetails { Title = "Plano invalido", Detail = ex.Message, Status = 400 });
        }
    }

    /// <summary>Exclui um plano que ainda nao possua assinaturas vinculadas.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
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
            return NotFound(new ProblemDetails { Title = "Plano nao encontrado", Detail = ex.Message, Status = 404 });
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new ProblemDetails { Title = "Exclusao bloqueada", Detail = ex.Message, Status = 409 });
        }
    }
}

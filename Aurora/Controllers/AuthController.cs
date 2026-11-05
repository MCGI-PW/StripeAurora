using AuroraPet.Auth.Application.Dtos;
using AuroraPet.Auth.Application.Services;
using AuroraPet.Infrastructure.Security;
using AuroraPet.Payments.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPet.Payments.Controllers;

/// <summary>
/// Autenticacao JWT: cadastro e login devolvem o token que deve ser enviado nas demais
/// rotas no header <c>Authorization: Bearer {token}</c>.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service) => _service = service;

    /// <summary>Cadastra um usuario com o perfil Clinic e ja devolve o token de acesso.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        try
        {
            var response = await _service.RegisterAsync(request, ct);
            return CreatedAtAction(nameof(Me), null, response);
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new ProblemDetails { Title = "E-mail ja cadastrado", Detail = ex.Message, Status = 409 });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Cadastro invalido", Detail = ex.Message, Status = 400 });
        }
    }

    /// <summary>Autentica por e-mail e senha e devolve um token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.LoginAsync(request, ct));
        }
        catch (InvalidCredentialsException ex)
        {
            return Unauthorized(new ProblemDetails { Title = "Credenciais invalidas", Detail = ex.Message, Status = 401 });
        }
    }

    /// <summary>Dados do usuario dono do token.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(AuroraClaims.Subject)?.Value, out var userId))
            return Unauthorized(new ProblemDetails { Title = "Token invalido", Detail = "O token nao contem o id do usuario.", Status = 401 });

        try
        {
            return Ok(await _service.GetProfileAsync(userId, ct));
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Usuario nao encontrado", Detail = ex.Message, Status = 404 });
        }
    }
}

using CQRS.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Post.Cmd.Api.Commands;
using Post.Common.DTOs;

namespace Post.Cmd.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RestoreReadDbController : ControllerBase
{
    private readonly ILogger<RestoreReadDbController> _logger;
    private readonly ICommandDispatcher _commandDispatcher;

    public RestoreReadDbController(ILogger<RestoreReadDbController> logger, ICommandDispatcher commandDispatcher)
    {
        _logger = logger;
        _commandDispatcher = commandDispatcher;
    }

    [HttpPost]
    public async Task<ActionResult> RestoreReadDbAsync()
    {
        try
        {
            await _commandDispatcher.SendAsync(new RestoreReadDbCommand());
            return StatusCode(StatusCodes.Status201Created, new BaseResponse
            {
                Message = "ReadDb restored Successfully!"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.Log(LogLevel.Warning, ex, "Client made a bad request!");
            return BadRequest(new BaseResponse
            {
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            const string SAFE_ERROR_MESSAGE = "Error while processing request to restore read Db!";
            _logger.Log(LogLevel.Error, ex, SAFE_ERROR_MESSAGE);
            _logger.LogError(ex, SAFE_ERROR_MESSAGE);
            return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Message = SAFE_ERROR_MESSAGE
            });
        }
    }

}

using LogService.Application.DTOs;
using LogService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;

namespace LogService.API.Controllers;

/// <summary>
/// Merkezi log görüntüleme endpoint'leri.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogRepository _logRepository;

    public LogsController(ILogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    /// <summary>GET api/logs - Logları listele</summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? level = null)
    {
        var logs = await _logRepository.GetAllAsync(pageNumber, pageSize, level);
        var totalCount = await _logRepository.GetCountAsync(level);

        var result = new PaginatedResult<LogEntryDto>
        {
            Items = logs.Select(l => new LogEntryDto
            {
                Id = l.Id, Level = l.Level, ServiceName = l.ServiceName,
                Message = l.Message, Exception = l.Exception, Timestamp = l.Timestamp
            }).ToList(),
            TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<LogEntryDto>>.SuccessResult(result));
    }
}

using Microsoft.AspNetCore.Mvc;
using TaskStation.Application.DTOs;
using TaskStation.Application.Interfaces;

namespace TaskStation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskAppService _taskService;

    public TasksController(ITaskAppService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Creates a new task. Accepts multipart/form-data with optional file upload.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data", "application/json")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromForm] CreateTaskRequest request, CancellationToken ct)
    {
        var result = await _taskService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Lists all tasks with optional status filter (PENDING, DONE, OVERDUE).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
    {
        var result = await _taskService.GetAllAsync(status, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a task by its ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _taskService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates a task (title, SLA, status, and optionally file).
    /// </summary>
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromForm] UpdateTaskRequest request,
        CancellationToken ct)
    {
        var result = await _taskService.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates the status of a task (e.g., mark as DONE).
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        string id,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken ct)
    {
        var result = await _taskService.UpdateStatusAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Downloads the file attached to a task (when stored in MongoDB).
    /// </summary>
    [HttpGet("{id}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(string id, CancellationToken ct)
    {
        var fileData = await _taskService.GetFileAsync(id, ct);

        if (fileData == null)
        {
            return NotFound(new { message = "File not found for this task" });
        }

        // Debug: Log file data info
        Console.WriteLine($"[DEBUG] Returning file: {fileData.Value.FileName}");
        Console.WriteLine($"[DEBUG] Content-Type: {fileData.Value.ContentType}");
        Console.WriteLine($"[DEBUG] File size: {fileData.Value.FileData.Length} bytes");

        // Add headers to force download
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileData.Value.FileName}\"");

        return File(fileData.Value.FileData, fileData.Value.ContentType, fileData.Value.FileName);
    }

    /// <summary>
    /// Debug endpoint to check file info without downloading.
    /// </summary>
    [HttpGet("{id}/file-info")]
    public async Task<IActionResult> GetFileInfo(string id, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(id, ct);
        var fileData = await _taskService.GetFileAsync(id, ct);

        return Ok(new
        {
            taskId = id,
            hasFileUrl = !string.IsNullOrEmpty(task.FileUrl),
            hasFileName = !string.IsNullOrEmpty(task.FileName),
            fileDataAvailable = fileData != null,
            fileDataSize = fileData?.FileData.Length ?? 0,
            fileName = fileData?.FileName,
            contentType = fileData?.ContentType
        });
    }
}

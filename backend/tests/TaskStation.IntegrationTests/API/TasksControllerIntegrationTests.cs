using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskStation.Application.DTOs;
using TaskStation.IntegrationTests.Fixtures;

namespace TaskStation.IntegrationTests.API;

public class TasksControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public TasksControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region POST /api/tasks

    [Fact]
    public async Task CreateTask_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var content = new MultipartFormDataContent
        {
            { new StringContent("Integration Test Task"), "title" },
            { new StringContent("24"), "slaHours" }
        };

        // Act
        var response = await _client.PostAsync("/api/tasks", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.Title.Should().Be("Integration Test Task");
        task.SlaHours.Should().Be(24);
        task.Status.Should().Be("PENDING");
        task.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateTask_WithInvalidData_ShouldReturn400BadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent
        {
            { new StringContent(""), "title" }, // Empty title
            { new StringContent("24"), "slaHours" }
        };

        // Act
        var response = await _client.PostAsync("/api/tasks", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTask_WithFile_ShouldStoreFileInMongoDB()
    {
        // Arrange
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        var content = new MultipartFormDataContent
        {
            { new StringContent("Task with File"), "title" },
            { new StringContent("48"), "slaHours" },
            { fileContent, "file", "document.pdf" }
        };

        // Act
        var response = await _client.PostAsync("/api/tasks", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.FileName.Should().Be("document.pdf");
        task.FileUrl.Should().Contain("/api/tasks/");
        task.FileDataBase64.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GET /api/tasks

    [Fact]
    public async Task GetAllTasks_ShouldReturnAllTasks()
    {
        // Arrange - Create test tasks
        await CreateTestTask("Task 1", 24);
        await CreateTestTask("Task 2", 48);

        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        tasks.Should().NotBeNull();
        tasks!.Should().HaveCountGreaterThanOrEqualTo(2);
        tasks.Select(t => t.Title).Should().Contain(new[] { "Task 1", "Task 2" });
    }

    [Fact]
    public async Task GetAllTasks_WithStatusFilter_ShouldReturnFilteredTasks()
    {
        // Arrange
        var taskId = await CreateTestTask("Pending Task", 24);

        // Act
        var response = await _client.GetAsync("/api/tasks?status=PENDING");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        tasks.Should().NotBeNull();
        tasks!.Should().OnlyContain(t => t.Status == "PENDING");
    }

    #endregion

    #region GET /api/tasks/{id}

    [Fact]
    public async Task GetTaskById_WithExistingId_ShouldReturnTask()
    {
        // Arrange
        var taskId = await CreateTestTask("Test Task", 24);

        // Act
        var response = await _client.GetAsync($"/api/tasks/{taskId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.Id.Should().Be(taskId);
        task.Title.Should().Be("Test Task");
    }

    [Fact]
    public async Task GetTaskById_WithNonExistingId_ShouldReturn404NotFound()
    {
        // Arrange
        var fakeId = "507f1f77bcf86cd799439011"; // Valid ObjectId format but doesn't exist

        // Act
        var response = await _client.GetAsync($"/api/tasks/{fakeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /api/tasks/{id}

    [Fact]
    public async Task UpdateTask_WithValidData_ShouldReturn200OK()
    {
        // Arrange
        var taskId = await CreateTestTask("Original Title", 24);

        var content = new MultipartFormDataContent
        {
            { new StringContent("Updated Title"), "title" },
            { new StringContent("48"), "slaHours" },
            { new StringContent("DONE"), "status" },
            { new StringContent("false"), "removeFile" }
        };

        // Act
        var response = await _client.PutAsync($"/api/tasks/{taskId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.Title.Should().Be("Updated Title");
        task.SlaHours.Should().Be(48);
        task.Status.Should().Be("DONE");
    }

    [Fact]
    public async Task UpdateTask_WithRemoveFileTrue_ShouldRemoveFile()
    {
        // Arrange
        var taskId = await CreateTestTaskWithFile("Task with File", 24);

        var content = new MultipartFormDataContent
        {
            { new StringContent("Task with File"), "title" },
            { new StringContent("24"), "slaHours" },
            { new StringContent("PENDING"), "status" },
            { new StringContent("true"), "removeFile" }
        };

        // Act
        var response = await _client.PutAsync($"/api/tasks/{taskId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.FileName.Should().BeNull();
        task.FileUrl.Should().BeNull();
    }

    #endregion

    #region PATCH /api/tasks/{id}

    [Fact]
    public async Task UpdateTaskStatus_ToDone_ShouldReturn200OK()
    {
        // Arrange
        var taskId = await CreateTestTask("Task to Complete", 24);

        var updateRequest = new { status = "DONE" };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(updateRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PatchAsync($"/api/tasks/{taskId}", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.Status.Should().Be("DONE");
    }

    #endregion

    #region GET /api/tasks/{id}/file

    [Fact]
    public async Task GetTaskFile_WithFileData_ShouldReturnFile()
    {
        // Arrange
        var taskId = await CreateTestTaskWithFile("Task with File", 24);

        // Act
        var response = await _client.GetAsync($"/api/tasks/{taskId}/file");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var fileBytes = await response.Content.ReadAsByteArrayAsync();
        fileBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTaskFile_WithoutFile_ShouldReturn404NotFound()
    {
        // Arrange
        var taskId = await CreateTestTask("Task without File", 24);

        // Act
        var response = await _client.GetAsync($"/api/tasks/{taskId}/file");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private async Task<string> CreateTestTask(string title, int slaHours)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(title), "title" },
            { new StringContent(slaHours.ToString()), "slaHours" }
        };

        var response = await _client.PostAsync("/api/tasks", content);
        response.EnsureSuccessStatusCode();

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        return task!.Id;
    }

    private async Task<string> CreateTestTaskWithFile(string title, int slaHours)
    {
        var fileContent = new ByteArrayContent(new byte[] { 10, 20, 30, 40, 50 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        var content = new MultipartFormDataContent
        {
            { new StringContent(title), "title" },
            { new StringContent(slaHours.ToString()), "slaHours" },
            { fileContent, "file", "test.pdf" }
        };

        var response = await _client.PostAsync("/api/tasks", content);
        response.EnsureSuccessStatusCode();

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        return task!.Id;
    }

    #endregion
}

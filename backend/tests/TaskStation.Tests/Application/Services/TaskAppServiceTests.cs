using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using TaskStation.Application.DTOs;
using TaskStation.Application.Interfaces;
using TaskStation.Application.Services;
using TaskStation.Domain.Entities;
using TaskStation.Domain.Enums;
using TaskStation.Domain.Exceptions;
using TaskStation.Domain.Repositories;

namespace TaskStation.Tests.Application.Services;

public class TaskAppServiceTests
{
    private readonly Mock<ITaskRepository> _mockRepository;
    private readonly Mock<IFileStorageService> _mockFileStorage;
    private readonly Mock<IValidator<CreateTaskRequest>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateTaskRequest>> _mockUpdateValidator;
    private readonly Mock<IFileStorageSettings> _mockFileStorageSettings;
    private readonly TaskAppService _service;

    public TaskAppServiceTests()
    {
        _mockRepository = new Mock<ITaskRepository>();
        _mockFileStorage = new Mock<IFileStorageService>();
        _mockCreateValidator = new Mock<IValidator<CreateTaskRequest>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateTaskRequest>>();
        _mockFileStorageSettings = new Mock<IFileStorageSettings>();

        _service = new TaskAppService(
            _mockRepository.Object,
            _mockFileStorage.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object,
            _mockFileStorageSettings.Object
        );

        // Default: validation passes
        _mockCreateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockUpdateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateTaskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateTask()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Test Task",
            SlaHours = 24
        };

        _mockFileStorageSettings.Setup(s => s.IsS3Enabled).Returns(false);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Task");
        result.SlaHours.Should().Be(24);
        result.Status.Should().Be("PENDING");

        _mockRepository.Verify(r => r.InsertAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateTaskRequest { Title = "", SlaHours = 24 };

        var validationFailure = new ValidationFailure("Title", "Title is required");
        _mockCreateValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { validationFailure }));

        // Act
        var act = async () => await _service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<TaskValidationException>();
        _mockRepository.Verify(r => r.InsertAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithFileAndS3Enabled_ShouldUploadToS3()
    {
        // Arrange
        var mockFile = CreateMockFile("test.pdf", "application/pdf", new byte[] { 1, 2, 3 });
        var request = new CreateTaskRequest
        {
            Title = "Task with file",
            SlaHours = 24,
            File = mockFile.Object
        };

        _mockFileStorageSettings.Setup(s => s.IsS3Enabled).Returns(true);
        _mockFileStorage
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), "test.pdf", "application/pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://s3.example.com/test.pdf");

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.FileUrl.Should().Be("https://s3.example.com/test.pdf");
        _mockFileStorage.Verify(
            s => s.UploadAsync(It.IsAny<Stream>(), "test.pdf", "application/pdf", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateAsync_WithFileAndS3Disabled_ShouldStoreInMongoDB()
    {
        // Arrange
        var fileContent = new byte[] { 1, 2, 3, 4, 5 };
        var mockFile = CreateMockFile("doc.pdf", "application/pdf", fileContent);
        var request = new CreateTaskRequest
        {
            Title = "Task",
            SlaHours = 24,
            File = mockFile.Object
        };

        _mockFileStorageSettings.Setup(s => s.IsS3Enabled).Returns(false);

        TaskItem? capturedTask = null;
        _mockRepository
            .Setup(r => r.InsertAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((task, ct) => capturedTask = task)
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateAsync(request);

        // Assert
        capturedTask.Should().NotBeNull();
        capturedTask!.FileData.Should().BeEquivalentTo(fileContent);
        capturedTask.FileName.Should().Be("doc.pdf");
        capturedTask.FileContentType.Should().Be("application/pdf");
        capturedTask.FileUrl.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithoutFilter_ShouldReturnAllTasks()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", 24),
            new TaskItem("Task 2", 48)
        };

        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        // Act
        var result = await _service.GetAllAsync(null);

        // Assert
        result.Should().HaveCount(2);
        result.Select(t => t.Title).Should().Contain(new[] { "Task 1", "Task 2" });
    }

    [Fact]
    public async Task GetAllAsync_WithPendingFilter_ShouldReturnPendingTasks()
    {
        // Arrange
        var tasks = new List<TaskItem> { new TaskItem("Pending Task", 24) };
        _mockRepository.Setup(r => r.GetByStatusAsync(TaskItemStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetAllAsync("PENDING");

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be("PENDING");
        _mockRepository.Verify(r => r.GetByStatusAsync(TaskItemStatus.Pending, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidFilter_ShouldThrowException()
    {
        // Arrange & Act
        var act = async () => await _service.GetAllAsync("INVALID_STATUS");

        // Assert
        var exception = await act.Should().ThrowAsync<TaskValidationException>();
        exception.Which.Errors.Should().Contain(e => e.Contains("Invalid status filter"));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnTask()
    {
        // Arrange
        var task = new TaskItem("Task", 24);
        _mockRepository.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(task);

        // Act
        var result = await _service.GetByIdAsync("123");

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Task");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldThrowNotFoundException()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync("999", It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        // Act
        var act = async () => await _service.GetByIdAsync("999");

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*TaskItem*999*");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ShouldUpdateTask()
    {
        // Arrange
        var existingTask = new TaskItem("Old Title", 24);
        var request = new UpdateTaskRequest
        {
            Title = "New Title",
            SlaHours = 48,
            Status = "DONE"
        };

        _mockRepository.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(existingTask);

        // Act
        var result = await _service.UpdateAsync("123", request);

        // Assert
        result.Title.Should().Be("New Title");
        result.SlaHours.Should().Be(48);
        result.Status.Should().Be("DONE");

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithRemoveFileTrue_ShouldRemoveFile()
    {
        // Arrange
        var existingTask = new TaskItem("Task", 24);
        existingTask.SetFileData(new byte[] { 1, 2, 3 }, "file.pdf", "application/pdf");

        var request = new UpdateTaskRequest
        {
            Title = "Task",
            SlaHours = 24,
            Status = "PENDING",
            RemoveFile = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(existingTask);

        // Act
        await _service.UpdateAsync("123", request);

        // Assert
        existingTask.FileData.Should().BeNull();
        existingTask.FileName.Should().BeNull();
        existingTask.FileContentType.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithNewFile_ShouldReplaceFile()
    {
        // Arrange
        var existingTask = new TaskItem("Task", 24);
        var newFileContent = new byte[] { 10, 20, 30 };
        var mockFile = CreateMockFile("new.pdf", "application/pdf", newFileContent);

        var request = new UpdateTaskRequest
        {
            Title = "Task",
            SlaHours = 24,
            Status = "PENDING",
            File = mockFile.Object
        };

        _mockFileStorageSettings.Setup(s => s.IsS3Enabled).Returns(false);
        _mockRepository.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(existingTask);

        // Act
        await _service.UpdateAsync("123", request);

        // Assert
        existingTask.FileData.Should().BeEquivalentTo(newFileContent);
        existingTask.FileName.Should().Be("new.pdf");
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Fact]
    public async Task UpdateStatusAsync_ToPending_ShouldUpdateStatus()
    {
        // Arrange
        var task = new TaskItem("Task", 24);
        var request = new UpdateTaskStatusRequest { Status = "PENDING" };

        _mockRepository.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(task);

        // Act
        var result = await _service.UpdateStatusAsync("123", request);

        // Assert
        result.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task UpdateStatusAsync_ToDone_ShouldMarkAsDone()
    {
        // Arrange
        var task = new TaskItem("Task", 24);
        var request = new UpdateTaskStatusRequest { Status = "DONE" };

        _mockRepository.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(task);

        // Act
        var result = await _service.UpdateStatusAsync("123", request);

        // Assert
        result.Status.Should().Be("DONE");
        _mockRepository.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetFileAsync Tests

    [Fact]
    public async Task GetFileAsync_WithFileData_ShouldReturnFileInfo()
    {
        // Arrange
        var task = new TaskItem("Task", 24);
        var fileData = new byte[] { 1, 2, 3 };
        task.SetFileData(fileData, "document.pdf", "application/pdf");

        _mockRepository.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(task);

        // Act
        var result = await _service.GetFileAsync("123");

        // Assert
        result.Should().NotBeNull();
        result!.Value.FileData.Should().BeEquivalentTo(fileData);
        result.Value.FileName.Should().Be("document.pdf");
        result.Value.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task GetFileAsync_WithoutFileData_ShouldReturnNull()
    {
        // Arrange
        var task = new TaskItem("Task", 24);
        _mockRepository.Setup(r => r.GetByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(task);

        // Act
        var result = await _service.GetFileAsync("123");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private Mock<IFormFile> CreateMockFile(string fileName, string contentType, byte[] content)
    {
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(content);

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        return mockFile;
    }

    #endregion
}

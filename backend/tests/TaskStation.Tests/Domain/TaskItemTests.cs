using TaskStation.Domain.Entities;
using TaskStation.Domain.Enums;
using TaskStation.Domain.Exceptions;

namespace TaskStation.Tests.Domain;

public class TaskItemTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateTask()
    {
        // Arrange
        var title = "Test Task";
        var slaHours = 24;

        // Act
        var task = new TaskItem(title, slaHours);

        // Assert
        task.Title.Should().Be(title);
        task.SlaHours.Should().Be(slaHours);
        task.Status.Should().Be(TaskItemStatus.Pending);
        task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        task.DueDate.Should().BeCloseTo(DateTime.UtcNow.AddHours(slaHours), TimeSpan.FromSeconds(1));
    }


    [Fact]
    public void Constructor_WithTitleTooLong_ShouldThrowException()
    {
        // Arrange
        var longTitle = new string('a', 201);

        // Act
        var act = () => new TaskItem(longTitle, 24);

        // Assert
        var exception = act.Should().Throw<TaskValidationException>();
        exception.Which.Errors.Should().Contain(e => e.Contains("200"));
    }


    [Fact]
    public void MarkAsDone_WhenPending_ShouldChangeStatus()
    {
        // Arrange
        var task = new TaskItem("Test", 24);

        // Act
        task.MarkAsDone();

        // Assert
        task.Status.Should().Be(TaskItemStatus.Done);
    }

    [Fact]
    public void MarkAsDone_WhenAlreadyDone_ShouldThrowException()
    {
        // Arrange
        var task = new TaskItem("Test", 24);
        task.MarkAsDone();

        // Act
        var act = () => task.MarkAsDone();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already completed*");
    }

    [Fact]
    public void UpdateTitle_WithValidTitle_ShouldUpdateTitle()
    {
        // Arrange
        var task = new TaskItem("Old Title", 24);
        var newTitle = "New Title";

        // Act
        task.UpdateTitle(newTitle);

        // Assert
        task.Title.Should().Be(newTitle);
    }

    [Fact]
    public void UpdateSla_WithValidHours_ShouldUpdateSlaAndDueDate()
    {
        // Arrange
        var task = new TaskItem("Test", 24);
        var newSla = 48;

        // Act
        task.UpdateSla(newSla);

        // Assert
        task.SlaHours.Should().Be(newSla);
        task.DueDate.Should().BeCloseTo(task.CreatedAt.AddHours(newSla), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetFileUrl_WithValidUrl_ShouldSetUrl()
    {
        // Arrange
        var task = new TaskItem("Test", 24);
        var fileUrl = "https://s3.example.com/file.pdf";

        // Act
        task.SetFileUrl(fileUrl);

        // Assert
        task.FileUrl.Should().Be(fileUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetFileUrl_WithInvalidUrl_ShouldThrowException(string invalidUrl)
    {
        // Arrange
        var task = new TaskItem("Test", 24);

        // Act
        var act = () => task.SetFileUrl(invalidUrl);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*URL cannot be empty*");
    }

    [Fact]
    public void SetFileData_WithValidData_ShouldSetFileFields()
    {
        // Arrange
        var task = new TaskItem("Test", 24);
        var fileData = new byte[] { 1, 2, 3, 4 };
        var fileName = "document.pdf";
        var contentType = "application/pdf";

        // Act
        task.SetFileData(fileData, fileName, contentType);

        // Assert
        task.FileData.Should().BeEquivalentTo(fileData);
        task.FileName.Should().Be(fileName);
        task.FileContentType.Should().Be(contentType);
        task.FileUrl.Should().BeNull(); // Should clear S3 URL
    }

    [Fact]
    public void SetFileData_WithEmptyData_ShouldThrowException()
    {
        // Arrange
        var task = new TaskItem("Test", 24);

        // Act
        var act = () => task.SetFileData(Array.Empty<byte>(), "file.pdf", "application/pdf");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*data cannot be empty*");
    }

    [Fact]
    public void RemoveFile_ShouldClearAllFileFields()
    {
        // Arrange
        var task = new TaskItem("Test", 24);
        task.SetFileData(new byte[] { 1, 2, 3 }, "file.pdf", "application/pdf");

        // Act
        task.RemoveFile();

        // Assert
        task.FileData.Should().BeNull();
        task.FileName.Should().BeNull();
        task.FileContentType.Should().BeNull();
        task.FileUrl.Should().BeNull();
    }

    [Fact]
    public void MarkAsPending_WhenNotDone_ShouldSetStatusToPending()
    {
        // Arrange
        var task = new TaskItem("Test", 24);

        // Act
        task.MarkAsPending();

        // Assert
        task.Status.Should().Be(TaskItemStatus.Pending);
    }

    [Fact]
    public void MarkAsPending_WhenDone_ShouldThrowException()
    {
        // Arrange
        var task = new TaskItem("Test", 24);
        task.MarkAsDone();

        // Act
        var act = () => task.MarkAsPending();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot mark a completed task as pending*");
    }

    [Fact]
    public void IsSlaExpired_WhenNotExpiredAndPending_ShouldReturnFalse()
    {
        // Arrange
        var task = new TaskItem("Test", 24);

        // Act
        var isExpired = task.IsSlaExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

}

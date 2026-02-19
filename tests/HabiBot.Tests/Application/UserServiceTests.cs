using FluentAssertions;
using FluentValidation;
using HabiBot.Application.DTOs;
using HabiBot.Application.Services;
using HabiBot.Domain.Entities;
using HabiBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace HabiBot.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IValidator<CreateUserDto>> _validatorMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _validatorMock = new Mock<IValidator<CreateUserDto>>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);

        _userService = new UserService(
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateUser_WhenValidDto()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            TelegramUserId = 123456789,
            TelegramChatId = 123456789,
            Name = "Алексей"
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock
            .Setup(r => r.ExistsByTelegramIdAsync(dto.TelegramUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken ct) => u);

        // Act
        var result = await _userService.CreateUserAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Алексей");
        result.TelegramUserId.Should().Be(123456789);
        result.TelegramChatId.Should().Be(123456789);

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowException_WhenUserAlreadyExists()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            TelegramUserId = 123456789,
            TelegramChatId = 123456789,
            Name = "Алексей"
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock
            .Setup(r => r.ExistsByTelegramIdAsync(dto.TelegramUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _userService.CreateUserAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*уже существует*");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByTelegramIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var telegramId = 123456789L;
        var expectedUser = new User
        {
            Id = 1,
            TelegramUserId = telegramId,
            TelegramChatId = telegramId,
            Name = "Алексей"
        };

        _userRepositoryMock
            .Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetByTelegramIdAsync(telegramId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.TelegramUserId.Should().Be(telegramId);
        result.TelegramChatId.Should().Be(telegramId);
        result.Name.Should().Be("Алексей");
    }

    [Fact]
    public async Task GetByTelegramIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var telegramId = 123456789L;

        _userRepositoryMock
            .Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetByTelegramIdAsync(telegramId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByTelegramIdAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var telegramId = 123456789L;

        _userRepositoryMock
            .Setup(r => r.ExistsByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.ExistsByTelegramIdAsync(telegramId);

        // Assert
        result.Should().BeTrue();
    }
}

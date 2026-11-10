using FCG.Events;
using FCG.Users.Application.DTOs;
using FCG.Users.Application.Interfaces;
using FCG.Users.Application.Services;
using FCG.Users.Domain.Entities;
using FCG.Users.Domain.Exceptions;
using FCG.Users.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FCG.Users.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IPublishEndpoint> _publish = new();

    private AuthService CreateSut() =>
        new(_users.Object, _hasher.Object, _jwt.Object, _publish.Object, NullLogger<AuthService>.Instance);

    [Fact]
    public async Task Register_HashesPasswordPersistsAndPublishesEvent()
    {
        _users.Setup(r => r.ExistsByEmailAsync("novo@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User u, CancellationToken _) => u);
        _hasher.Setup(h => h.Hash("Senha@123")).Returns("HASH");

        var result = await CreateSut().RegisterAsync(new RegisterUserRequest("Novo User", "novo@email.com", "Senha@123"));

        Assert.Equal("novo@email.com", result.Email);
        _users.Verify(r => r.AddAsync(It.Is<User>(u => u.PasswordHash == "HASH"), It.IsAny<CancellationToken>()), Times.Once);
        _publish.Verify(p => p.Publish(It.IsAny<UserCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_RejectsWeakPasswordBeforeTouchingTheDatabase()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            CreateSut().RegisterAsync(new RegisterUserRequest("Novo User", "novo@email.com", "fraca")));

        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmail()
    {
        _users.Setup(r => r.ExistsByEmailAsync("dup@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateSut().RegisterAsync(new RegisterUserRequest("Dup User", "dup@email.com", "Senha@123")));

        _publish.Verify(p => p.Publish(It.IsAny<UserCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsTokenForValidCredentials()
    {
        var user = new User("Joao Gamer", "joao@email.com", "HASH");
        _users.Setup(r => r.GetByEmailAsync("joao@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Senha@123", "HASH")).Returns(true);
        _jwt.Setup(j => j.GenerateToken(user)).Returns("TOKEN");
        _jwt.Setup(j => j.GetExpiration()).Returns(DateTime.UtcNow.AddHours(8));

        var result = await CreateSut().LoginAsync(new LoginRequest("joao@email.com", "Senha@123"));

        Assert.Equal("TOKEN", result.Token);
        Assert.Equal("User", result.Role);
    }

    [Fact]
    public async Task Login_RejectsWrongPassword()
    {
        var user = new User("Joao Gamer", "joao@email.com", "HASH");
        _users.Setup(r => r.GetByEmailAsync("joao@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), "HASH")).Returns(false);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateSut().LoginAsync(new LoginRequest("joao@email.com", "Errada@123")));
    }

    [Fact]
    public async Task Login_RejectsUnknownUserWithSameMessageAsWrongPassword()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            CreateSut().LoginAsync(new LoginRequest("naoexiste@email.com", "Senha@123")));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task Login_RejectsDeactivatedAccount()
    {
        var user = new User("Joao Gamer", "joao@email.com", "HASH");
        user.Deactivate();
        _users.Setup(r => r.GetByEmailAsync("joao@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateSut().LoginAsync(new LoginRequest("joao@email.com", "Senha@123")));
    }
}

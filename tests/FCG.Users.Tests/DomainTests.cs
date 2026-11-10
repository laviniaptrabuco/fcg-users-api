using FCG.Users.Domain.Entities;
using FCG.Users.Domain.Enums;
using FCG.Users.Domain.Exceptions;
using FCG.Users.Domain.Services;

namespace FCG.Users.Tests;

public class PasswordValidatorTests
{
    [Theory]
    [InlineData("Senha@123")]
    [InlineData("abcdefg1#")]
    [InlineData("Xyz12345_")]
    public void Validate_AcceptsStrongPasswords(string password)
    {
        PasswordValidator.Validate(password);
        Assert.True(PasswordValidator.IsValid(password));
    }

    [Theory]
    [InlineData("curta1@")]          // menos de 8 caracteres
    [InlineData("semnumero@@")]      // sem digito
    [InlineData("12345678@")]        // sem letra
    [InlineData("SemEspecial123")]   // sem caractere especial
    public void Validate_RejectsWeakPasswords(string password)
    {
        Assert.Throws<DomainException>(() => PasswordValidator.Validate(password));
        Assert.False(PasswordValidator.IsValid(password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsEmpty(string password) =>
        Assert.Throws<DomainException>(() => PasswordValidator.Validate(password));
}

public class UserTests
{
    [Fact]
    public void Constructor_NormalizesEmailAndActivatesUser()
    {
        var user = new User("Joao Gamer", "JOAO@Email.com", "hash");

        Assert.Equal("joao@email.com", user.Email);
        Assert.True(user.IsActive);
        Assert.Equal(UserRole.User, user.Role);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Theory]
    [InlineData("", "a@b.com")]
    [InlineData("J", "a@b.com")]
    [InlineData("Joao", "")]
    [InlineData("Joao", "sem-arroba")]
    [InlineData("Joao", "a@b")]
    public void Constructor_RejectsInvalidData(string name, string email) =>
        Assert.Throws<DomainException>(() => new User(name, email, "hash"));

    [Fact]
    public void ChangePassword_RejectsEmptyHash()
    {
        var user = new User("Joao Gamer", "joao@email.com", "hash");
        Assert.Throws<DomainException>(() => user.ChangePassword(" "));
    }

    [Fact]
    public void DeactivateAndActivate_TogglesState()
    {
        var user = new User("Joao Gamer", "joao@email.com", "hash");
        user.Deactivate();
        Assert.False(user.IsActive);
        user.Activate();
        Assert.True(user.IsActive);
    }
}

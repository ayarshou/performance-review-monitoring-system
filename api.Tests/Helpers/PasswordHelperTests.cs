using PerformanceReviewApi.Helpers;

namespace PerformanceReviewApi.Tests.Helpers;

public class PasswordHelperTests
{
    [Fact]
    public void Hash_ReturnsNonEmptyStringWithColonSeparator()
    {
        var hash = PasswordHelper.Hash("password123");
        Assert.NotEmpty(hash);
        Assert.Contains(":", hash);
        Assert.Equal(2, hash.Split(':').Length);
    }

    [Fact]
    public void Hash_TwoCallsWithSamePassword_ProduceDifferentResults()
    {
        // Different random salts must produce different stored hashes
        var h1 = PasswordHelper.Hash("Password1!");
        var h2 = PasswordHelper.Hash("Password1!");
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHelper.Hash("MyS3cretPass!");
        Assert.True(PasswordHelper.Verify("MyS3cretPass!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHelper.Hash("CorrectPass!");
        Assert.False(PasswordHelper.Verify("WrongPass!", hash));
    }

    [Fact]
    public void Verify_CaseSensitivePassword_ReturnsFalse()
    {
        var hash = PasswordHelper.Hash("Password!");
        Assert.False(PasswordHelper.Verify("password!", hash));
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalse()
    {
        Assert.False(PasswordHelper.Verify("any", "notavalidhash"));
    }

    [Fact]
    public void Verify_EmptyHash_ReturnsFalse()
    {
        Assert.False(PasswordHelper.Verify("any", string.Empty));
    }
}

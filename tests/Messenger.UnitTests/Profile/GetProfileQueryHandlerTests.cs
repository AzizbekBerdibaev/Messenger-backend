using FluentAssertions;
using Messenger.Application.Features.Profile.Queries.GetProfile;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Moq;
using Xunit;

namespace Messenger.UnitTests.Profile;

public sealed class GetProfileQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();

    [Fact]
    public async Task Handle_UserExists_ReturnsProfileDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create(userId, "1234567890");
        user.UpdateProfile("John", "Doe", null);

        var usersSet = TestDbSetHelper.CreateMockDbSet<User>(new List<User> { user });
        _dbMock.Setup(d => d.Users).Returns(usersSet);

        var handler = new GetProfileQueryHandler(_dbMock.Object);
        var query = new GetProfileQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.PhoneNumber.Should().Be("1234567890");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task Handle_UserDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var usersSet = TestDbSetHelper.CreateMockDbSet<User>([]);
        _dbMock.Setup(d => d.Users).Returns(usersSet);

        var handler = new GetProfileQueryHandler(_dbMock.Object);
        var query = new GetProfileQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}

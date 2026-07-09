using FluentAssertions;
using Messenger.Application.Features.Search.Queries.SearchUsers;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Moq;
using Xunit;

namespace Messenger.UnitTests.Search;

public sealed class SearchUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ValidSearchTerm_ReturnsMatchedUsers()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var user1 = User.Create(Guid.NewGuid(), "+998901234567");
        user1.UpdateProfile("John", "Doe", null);
        var user2 = User.Create(Guid.NewGuid(), "+998907654321");
        user2.UpdateProfile("Jane", "Smith", null);

        var users = new List<User> { user1, user2 };
        dbMock.Setup(d => d.Users).Returns(TestDbSetHelper.CreateMockDbSet(users));

        var handler = new SearchUsersQueryHandler(dbMock.Object);
        var result = await handler.Handle(new SearchUsersQuery("john"), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().FirstName.Should().Be("John");
    }
}

using FluentAssertions;
using Messenger.Application.Features.Privacy.Commands.BlockUser;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Moq;
using Xunit;

namespace Messenger.UnitTests.Privacy;

public sealed class BlockUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesBlockedUser()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var blockedUsers = new List<BlockedUser>();
        var dbSetMock = TestDbSetHelper.CreateMockDbSet(blockedUsers);

        dbMock.Setup(d => d.BlockedUsers).Returns(dbSetMock);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new BlockUserCommandHandler(dbMock.Object);
        var command = new BlockUserCommand(Guid.NewGuid(), Guid.NewGuid());

        await handler.Handle(command, CancellationToken.None);

        dbMock.Verify(d => d.BlockedUsers.Add(It.IsAny<BlockedUser>()), Times.Once);
        dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BlockingSelf_ThrowsInvalidOperationException()
    {
        var dbMock = new Mock<IApplicationDbContext>();
        var handler = new BlockUserCommandHandler(dbMock.Object);
        var userId = Guid.NewGuid();
        var command = new BlockUserCommand(userId, userId);

        var action = async () => await handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot block yourself.");
    }
}

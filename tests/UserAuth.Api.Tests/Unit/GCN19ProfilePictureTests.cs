using Microsoft.AspNetCore.Hosting;
using Moq;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;

namespace UserAuth.Api.Tests.Unit;

public class GCN19ProfilePictureTests : IDisposable
{
    private readonly string _tempRoot;

    public GCN19ProfilePictureTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "carenova-gcn19-" + Guid.NewGuid());

        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task UpdateProfilePictureAsync_ValidPicture_SavesFileAndUpdatesProfile()
    {
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FullName = "GCN19 QA Customer",
            Email = "gcn19.qa@example.com"
        };

        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repository
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var environment = CreateEnvironment();
        var service = new ProfileService(repository.Object, environment.Object);

        await using var imageStream =
            new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var result = await service.UpdateProfilePictureAsync(
            userId,
            imageStream,
            ".jpg");

        Assert.NotNull(result);
        Assert.True(result.HasProfilePicture);
        Assert.Equal($"/uploads/profile-pictures/{userId}.jpg",
            result.ProfilePictureUrl);

        var expectedFile = Path.Combine(
            _tempRoot,
            "wwwroot",
            "uploads",
            "profile-pictures",
            $"{userId}.jpg");

        Assert.True(File.Exists(expectedFile));

        repository.Verify(
            r => r.UpdateAsync(
                It.Is<User>(u =>
                    u.ProfilePicturePath ==
                    $"/uploads/profile-pictures/{userId}.jpg"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProfilePictureAsync_NewExtension_RemovesPreviousPicture()
    {
        var userId = Guid.NewGuid();

        var uploadsDirectory = Path.Combine(
            _tempRoot,
            "wwwroot",
            "uploads",
            "profile-pictures");

        Directory.CreateDirectory(uploadsDirectory);

        var oldFile = Path.Combine(
            uploadsDirectory,
            $"{userId}.jpg");

        await File.WriteAllBytesAsync(
            oldFile,
            new byte[] { 9, 9, 9 });

        var user = new User
        {
            Id = userId,
            FullName = "GCN19 QA Customer",
            Email = "gcn19.qa@example.com",
            ProfilePicturePath =
                $"/uploads/profile-pictures/{userId}.jpg"
        };

        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repository
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var environment = CreateEnvironment();
        var service = new ProfileService(repository.Object, environment.Object);

        await using var imageStream =
            new MemoryStream(new byte[] { 4, 5, 6, 7 });

        var result = await service.UpdateProfilePictureAsync(
            userId,
            imageStream,
            ".png");

        var newFile = Path.Combine(
            uploadsDirectory,
            $"{userId}.png");

        Assert.NotNull(result);
        Assert.False(File.Exists(oldFile));
        Assert.True(File.Exists(newFile));
        Assert.Equal(
            $"/uploads/profile-pictures/{userId}.png",
            result.ProfilePictureUrl);
    }

    [Fact]
    public async Task UpdateProfilePictureAsync_UserDoesNotExist_ReturnsNull()
    {
        var userId = Guid.NewGuid();

        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var environment = CreateEnvironment();
        var service = new ProfileService(repository.Object, environment.Object);

        await using var imageStream =
            new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await service.UpdateProfilePictureAsync(
            userId,
            imageStream,
            ".jpg");

        Assert.Null(result);

        repository.Verify(
            r => r.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private Mock<IWebHostEnvironment> CreateEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();

        environment
            .SetupGet(e => e.ContentRootPath)
            .Returns(_tempRoot);

        environment
            .SetupGet(e => e.WebRootPath)
            .Returns(Path.Combine(_tempRoot, "wwwroot"));

        return environment;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, true);
        }
    }
}

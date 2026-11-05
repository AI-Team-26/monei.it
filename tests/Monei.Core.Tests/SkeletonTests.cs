using Monei.Core.Configuration;
using Monei.Core.Repositories;
using Moq;
using NUnit.Framework;

namespace Monei.Core.Tests;

public class SkeletonTests
{
    [Test]
    public void MongoDbSettings_Defaults_Are_Empty()
    {
        var settings = new MongoDbSettings();

        Assert.That(settings.ConnectionString, Is.Empty);
        Assert.That(settings.DatabaseName, Is.Empty);
    }

    [Test]
    public async Task Repository_Can_Be_Mocked()
    {
        var repository = new Mock<IRepository<SampleEntity>>();
        repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SampleEntity>());

        var result = await repository.Object.GetAllAsync();

        Assert.That(result, Is.Empty);
    }

    public sealed class SampleEntity;
}

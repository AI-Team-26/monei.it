using Monei.Core.Configuration;
using Monei.Core.Repositories;
using Moq;
using NUnit.Framework;

namespace Monei.Core.Tests;

public class SkeletonTests
{
    [Test]
    public void SqlServerSettings_Default_Is_Empty()
    {
        var settings = new SqlServerSettings();

        Assert.That(settings.ConnectionString, Is.Empty);
    }

    [Test]
    public async Task Repository_Can_Be_Mocked()
    {
        var repository = new Mock<IRepository<SampleEntity, string>>();
        repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SampleEntity>());

        var result = await repository.Object.GetAllAsync();

        Assert.That(result, Is.Empty);
    }

    public sealed class SampleEntity;
}
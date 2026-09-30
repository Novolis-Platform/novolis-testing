using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novolis.Testing.Logging;
using Novolis.Testing.TestBases;

namespace Novolis.Testing.Unit;

public sealed class HostApplicationTestBaseLifecycleTests
{
    [Test]
    public async Task GetServices_before_initialize_throws()
    {
        var subject = new ProbeHostApplicationTestBase();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            _ = subject.Services;
            return Task.CompletedTask;
        });
        await Assert.That(ex!.Message).Contains("not initialized");
    }

    [Test]
    public async Task DisposeHostAsync_without_initialize_is_noop()
    {
        var subject = new ProbeHostApplicationTestBase();
        await subject.PublicDisposeHostAsync();
    }

    [Test]
    public async Task Initialize_and_dispose_runs_full_lifecycle()
    {
        var subject = new ProbeHostApplicationTestBase();
        await subject.InitializeAsync();
        await Assert.That(subject.Services.GetService<string>()).IsEqualTo("configured");
        await subject.PublicDisposeHostAsync();
    }

    private sealed class ProbeHostApplicationTestBase : HostApplicationTestBase
    {
        public IServiceProvider Services => GetServices;

        protected override Task SetupAsync(HostApplicationBuilder builder)
        {
            builder.Services.AddSingleton("configured");
            return Task.CompletedTask;
        }

        public Task PublicDisposeHostAsync() => DisposeHostAsync();
    }
}

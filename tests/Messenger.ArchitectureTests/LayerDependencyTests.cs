using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Messenger.ArchitectureTests;

/// <summary>
/// Architecture tests using NetArchTest to enforce Clean Architecture dependency rules.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "Messenger.Domain";
    private const string ApplicationNamespace = "Messenger.Application";
    private const string InfrastructureNamespace = "Messenger.Infrastructure";
    private const string WebApiNamespace = "Messenger.WebApi";

    // Load all assemblies once
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Messenger.Domain.Entities.User).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Messenger.Application.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(Messenger.Infrastructure.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly WebApiAssembly = typeof(Program).Assembly;

    // ── Domain must have NO dependencies on any other Messenger layer ─────────

    [Fact(DisplayName = "Domain layer must not depend on Application layer")]
    public void Domain_MustNotDependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain is the core; it must not know about Application");
    }

    [Fact(DisplayName = "Domain layer must not depend on Infrastructure layer")]
    public void Domain_MustNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not depend on Infrastructure");
    }

    [Fact(DisplayName = "Domain layer must not depend on WebApi layer")]
    public void Domain_MustNotDependOn_WebApi()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not depend on WebApi");
    }

    // ── Application must not depend on Infrastructure or WebApi ──────────────

    [Fact(DisplayName = "Application layer must not depend on Infrastructure layer")]
    public void Application_MustNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application should only depend on Domain and external abstractions");
    }

    [Fact(DisplayName = "Application layer must not depend on WebApi layer")]
    public void Application_MustNotDependOn_WebApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application must not know about the HTTP delivery mechanism");
    }

    // ── WebApi must not reference DbContext directly ───────────────────────────

    [Fact(DisplayName = "WebApi must not directly reference ApplicationDbContext")]
    public void WebApi_MustNotDirectlyUse_ApplicationDbContext()
    {
        var result = Types.InAssembly(WebApiAssembly)
            .ShouldNot()
            .HaveDependencyOn($"{InfrastructureNamespace}.Persistence.ApplicationDbContext")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Controllers must access data only through MediatR/Application layer, never directly through DbContext");
    }

    // ── Infrastructure must not depend on WebApi ──────────────────────────────

    [Fact(DisplayName = "Infrastructure layer must not depend on WebApi layer")]
    public void Infrastructure_MustNotDependOn_WebApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure should only depend on Application contracts");
    }

    // ── Naming conventions ─────────────────────────────────────────────────────

    [Fact(DisplayName = "All MediatR handlers must live in Application layer")]
    public void Handlers_MustResideIn_ApplicationLayer()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .ResideInNamespaceStartingWith(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All command/query handlers must reside in the Application project");
    }

    [Fact(DisplayName = "All FluentValidation validators must live in Application layer")]
    public void Validators_MustResideIn_ApplicationLayer()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .ResideInNamespaceStartingWith(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All validators must reside in the Application project");
    }
}

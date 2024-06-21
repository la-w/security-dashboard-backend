using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using NSubstitute;
using SecurityDashboard_Backend.Authentication;
using SecurityDashboard_Backend.Utilities;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SecurityDashboard_Backend.Tests.Utilities;

public class SecurityRequirementsOperationFilterTests
{
    [Authorize]
    private void DummyMethod_NoPolicy()
    {
    }

    [Authorize(Policies.UsedApiKey)]
    private void DummyMethod_UsedApiKeyPolicy()
    {
    }

    [Authorize(Policies.OAuthUser)]
    private void DummyMethod_OAuthUserPolicy()
    {
    }

    [Fact]
    public void Apply_WhenCalledWithoutPolicy_ShouldDoNothing()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var schemaGenerator = Substitute.For<ISchemaGenerator>();
        var methodInfo = GetType().GetMethod(nameof(DummyMethod_NoPolicy), BindingFlags.Instance | BindingFlags.NonPublic);
        var filterContext = new OperationFilterContext(new ApiDescription(), schemaGenerator, new SchemaRepository(), methodInfo);

        var operationFilter = new SecurityRequirementsOperationFilter();

        // Act
        operationFilter.Apply(operation, filterContext);

        // Assert
        operation.Responses.Count.Should().Be(0);
        operation.Security.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Apply_WhenCalledWithUsedApiKeyPolicy_ShouldAddSecurityRequirements()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var schemaGenerator = Substitute.For<ISchemaGenerator>();
        var methodInfo = GetType().GetMethod(nameof(DummyMethod_UsedApiKeyPolicy), BindingFlags.Instance | BindingFlags.NonPublic);
        var filterContext = new OperationFilterContext(new ApiDescription(), schemaGenerator, new SchemaRepository(), methodInfo);

        var operationFilter = new SecurityRequirementsOperationFilter();

        // Act
        operationFilter.Apply(operation, filterContext);

        // Assert
        operation.Responses.Count.Should().BeGreaterOrEqualTo(2);
        operation.Responses.Should().ContainKeys(["401", "403"]);
        operation.Security.Should().NotBeNull()
            .And.HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public void Apply_WhenCalledWithOAuthUserPolicy_ShouldAddSecurityRequirements()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var schemaGenerator = Substitute.For<ISchemaGenerator>();
        var methodInfo = GetType().GetMethod(nameof(DummyMethod_OAuthUserPolicy), BindingFlags.Instance | BindingFlags.NonPublic);
        var filterContext = new OperationFilterContext(new ApiDescription(), schemaGenerator, new SchemaRepository(), methodInfo);

        var operationFilter = new SecurityRequirementsOperationFilter();

        // Act
        operationFilter.Apply(operation, filterContext);

        // Assert
        operation.Responses.Count.Should().BeGreaterOrEqualTo(2);
        operation.Responses.Should().ContainKeys(["401", "403"]);
        operation.Security.Should().NotBeNull()
            .And.HaveCountGreaterOrEqualTo(1);
    }
}

using JsonSurfer.Core.Models;
using JsonSurfer.Core.Services;

namespace JsonSurfer.Tests.Services;

// Note: These tests are written to fail until JsonParserService.ParseToTree is refactored
// to return a ParseResult object instead of a nullable JsonNode.
public class JsonParserServiceTests
{
    private readonly JsonParserService _parser = new();

    [Fact]
    public void ParseToTree_WithEmptyString_ShouldFailWithErrorMessage()
    {
        // Arrange
        var json = string.Empty;

        // Act
        var result = _parser.ParseToTree(json);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.RootNode);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Fact]
    public void ParseToTree_WithEmptyJsonObject_ShouldSucceed()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = _parser.ParseToTree(json);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.RootNode);
        Assert.Equal(JsonNodeType.Object, result.RootNode.Type);
        Assert.Empty(result.RootNode.Children);
    }

    [Fact]
    public void ParseToTree_WithEmptyJsonArray_ShouldSucceed()
    {
        // Arrange
        var json = "[]";

        // Act
        var result = _parser.ParseToTree(json);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.RootNode);
        Assert.Equal(JsonNodeType.Array, result.RootNode.Type);
        Assert.Empty(result.RootNode.Children);
    }

    [Fact]
    public void ParseToTree_WithMalformedJson_ShouldFailWithErrorMessage()
    {
        // Arrange
        var json = "{\"key\": \"value\"";

        // Act
        var result = _parser.ParseToTree(json);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.RootNode);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Fact]
    public void ParseToTree_WithSimpleJsonObject_ShouldSucceedAndReturnCorrectTree()
    {
        // Arrange
        var json = "{\"key\": \"value\"}";

        // Act
        var result = _parser.ParseToTree(json);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.RootNode);
        Assert.Equal(JsonNodeType.Object, result.RootNode.Type);
        var child = Assert.Single(result.RootNode.Children);
        Assert.Equal("key", child.Key);
        Assert.Equal(JsonNodeType.String, child.Type);
        Assert.Equal("value", child.Value);
    }

    [Fact]
    public void ParseToTree_WithComplexJsonObject_ShouldSucceedAndReturnCorrectTree()
    {
        // Arrange
        var json = "{\"a\": 1, \"b\": true, \"c\": {\"d\": null}}";

        // Act
        var result = _parser.ParseToTree(json);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.RootNode);
        Assert.Equal(JsonNodeType.Object, result.RootNode.Type);
        Assert.Equal(3, result.RootNode.Children.Count);

        var a = result.RootNode.Children[0];
        Assert.Equal("a", a.Key);
        Assert.Equal(JsonNodeType.Number, a.Type);
        Assert.Equal(1m, a.Value); // System.Text.Json parses numbers as Decimal

        var b = result.RootNode.Children[1];
        Assert.Equal("b", b.Key);
        Assert.Equal(JsonNodeType.Boolean, b.Type);
        Assert.Equal(true, b.Value);

        var c = result.RootNode.Children[2];
        Assert.Equal("c", c.Key);
        Assert.Equal(JsonNodeType.Object, c.Type);
        var d = Assert.Single(c.Children);
        Assert.Equal("d", d.Key);
        Assert.Equal(JsonNodeType.Null, d.Type);
        Assert.Null(d.Value);
    }
}
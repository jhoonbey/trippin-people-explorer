using FluentAssertions;
using TripPin.Core.Common;

namespace TripPin.Tests.Builders;

public sealed class ODataQueryBuilderTests
{
    [Fact]
    public void Build_WithNoOptions_ReturnsResourcePathOnly()
    {
        var url = new ODataQueryBuilder("People").Build();

        url.Should().Be("People");
    }

    [Fact]
    public void Build_WithTopAndSkip_AppendsPagingOptions()
    {
        var url = new ODataQueryBuilder("People").Top(10).Skip(20).Build();

        url.Should().Be("People?$top=10&$skip=20");
    }

    [Fact]
    public void Build_WithZeroSkip_OmitsRedundantSkip()
    {
        var url = new ODataQueryBuilder("People").Top(10).Skip(0).Build();

        url.Should().Be("People?$top=10");
    }

    [Fact]
    public void Build_WithZeroTop_OmitsRedundantTop()
    {
        var url = new ODataQueryBuilder("People").Top(0).Skip(20).Build();

        url.Should().Be("People?$skip=20");
    }

    [Fact]
    public void Build_WithFilter_PreservesODataSyntaxCharsUnencoded()
    {
        var url = new ODataQueryBuilder("People")
            .Filter("contains(FirstName,'Scott')")
            .Build();

        url.Should().Be("People?$filter=contains(FirstName,'Scott')");
    }

    [Fact]
    public void Build_WithBlankFilter_IsIgnored()
    {
        var url = new ODataQueryBuilder("People").Filter("   ").Count().Build();

        url.Should().Be("People?$count=true");
    }

    [Fact]
    public void Build_CombinesOptionsInTheOrderAdded()
    {
        var url = new ODataQueryBuilder("People")
            .Filter("contains(tolower(FirstName),'r')")
            .Top(5)
            .Skip(0)
            .Count()
            .Build();

        url.Should().Be("People?$filter=contains(tolower(FirstName),'r')&$top=5&$count=true");
    }

    [Fact]
    public void Constructor_WithBlankResourcePath_Throws()
    {
        var act = () => new ODataQueryBuilder("  ");

        act.Should().Throw<ArgumentException>();
    }
}

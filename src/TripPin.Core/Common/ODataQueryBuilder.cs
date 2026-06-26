using System.Globalization;
using System.Text;

namespace TripPin.Core.Common;

public sealed class ODataQueryBuilder
{
    private readonly string _resourcePath;
    private readonly List<(string Key, string Value)> _options = [];

    public ODataQueryBuilder(string resourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);
        _resourcePath = resourcePath;
    }

    public ODataQueryBuilder Filter(string? expression) => Add("$filter", expression);

    public ODataQueryBuilder Expand(params string[] navigationProperties) =>
        navigationProperties.Length == 0 ? this : Add("$expand", string.Join(',', navigationProperties));

    public ODataQueryBuilder Top(int top) =>
        top <= 0 ? this : Add("$top", top.ToString(CultureInfo.InvariantCulture));

    public ODataQueryBuilder Skip(int skip) =>
        skip <= 0 ? this : Add("$skip", skip.ToString(CultureInfo.InvariantCulture));

    public ODataQueryBuilder Count(bool include = true) =>
        include ? Add("$count", "true") : this;

    public string Build()
    {
        if (_options.Count == 0)
            return _resourcePath;

        var sb = new StringBuilder(_resourcePath).Append('?');
        for (var i = 0; i < _options.Count; i++)
        {
            if (i > 0)
                sb.Append('&');

            var (key, value) = _options[i];
            sb.Append(key).Append('=').Append(EncodeQueryValue(value));
        }

        return sb.ToString();
    }

    public override string ToString() => Build();

    private ODataQueryBuilder Add(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _options.Add((key, value));
        return this;
    }

    private static string EncodeQueryValue(string value) =>
        Uri.EscapeDataString(value)
           .Replace("%28", "(", StringComparison.Ordinal)
           .Replace("%29", ")", StringComparison.Ordinal)
           .Replace("%2C", ",", StringComparison.Ordinal)
           .Replace("%27", "'", StringComparison.Ordinal)
           .Replace("%2F", "/", StringComparison.Ordinal)
           .Replace("%3A", ":", StringComparison.Ordinal);
}

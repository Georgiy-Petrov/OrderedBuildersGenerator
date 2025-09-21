using OrderedBuildersGenerator.EquatableCollections;

namespace OrderedBuildersGenerator.Models;

internal record ParametersInfo
{
    public ParametersInfo(EquatableArray<string> parametersIdentifiers, string parametersFullText)
    {
        ParametersIdentifiers = parametersIdentifiers;
        ParametersFullText = parametersFullText;
    }

    public EquatableArray<string> ParametersIdentifiers { get; }
    public string ParametersFullText { get; }
}
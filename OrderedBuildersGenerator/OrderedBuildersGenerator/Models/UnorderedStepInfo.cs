using OrderedBuildersGenerator.EquatableCollections;

namespace OrderedBuildersGenerator.Models;

internal record UnorderedStepInfo
{
    public UnorderedStepInfo(string name, ParametersInfo parametersInfo, EquatableArray<string> typeParameters,
        string constraints)
    {
        Name = name;
        ParametersInfo = parametersInfo;
        TypeParameters = typeParameters;
        Constraints = constraints;
    }

    public string Name { get; }
    public ParametersInfo ParametersInfo { get; }
    public EquatableArray<string> TypeParameters { get; }
    public string Constraints { get; }
}
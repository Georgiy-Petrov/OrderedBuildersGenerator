using OrderedBuildersGenerator.EquatableCollections;

namespace OrderedBuildersGenerator.Models;

internal record BuildStepInfo
{
    public BuildStepInfo(string name, ParametersInfo parametersInfo, EquatableArray<string> typeParameters,
        string constraints, string returnType)
    {
        Name = name;
        ParametersInfo = parametersInfo;
        TypeParameters = typeParameters;
        Constraints = constraints;
        ReturnType = returnType;
    }

    public string Name { get; }
    public ParametersInfo ParametersInfo { get; }
    public EquatableArray<string> TypeParameters { get; }
    public string Constraints { get; }
    public string ReturnType { get; }
}
using OrderedBuildersGenerator.EquatableCollections;

namespace OrderedBuildersGenerator.Models;

internal record BuilderInfo
{
    public EquatableArray<string> Usings { get; }
    public string ClassName { get; }
    public string ClassNameFromAttribute { get; }
    public (string Types, string Constraints) TypeParametersAndConstraints { get; }
    public string DeclaredNamespace { get; }
    public EquatableArray<ParametersInfo> ConstructorsParametersInfos { get; }
    public EquatableArray<UnorderedStepInfo> UnorderedSteps { get; }
    public EquatableArray<OrderedStepInfo> OrderedSteps { get; }
    public EquatableArray<BuildStepInfo> BuildSteps { get; }

    public BuilderInfo(
        EquatableArray<string> usings,
        string className,
        string classNameFromAttribute,
        (string Types, string Constraints) typeParametersAndConstraints,
        string declaredNamespace,
        EquatableArray<ParametersInfo> constructorsParametersInfos,
        EquatableArray<UnorderedStepInfo> unorderedSteps,
        EquatableArray<OrderedStepInfo> orderedSteps,
        EquatableArray<BuildStepInfo> buildSteps)
    {
        Usings = usings;
        ClassName = className;
        ClassNameFromAttribute = classNameFromAttribute;
        TypeParametersAndConstraints = typeParametersAndConstraints;
        DeclaredNamespace = declaredNamespace;
        ConstructorsParametersInfos = constructorsParametersInfos;
        UnorderedSteps = unorderedSteps;
        OrderedSteps = orderedSteps;
        BuildSteps = buildSteps;
    }
}
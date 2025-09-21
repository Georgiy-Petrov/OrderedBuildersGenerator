using System;
using OrderedBuildersGenerator.EquatableCollections;

namespace OrderedBuildersGenerator.Models;

internal record OrderedStepInfo
{
    public OrderedStepInfo(string name, ParametersInfo parametersInfo, EquatableArray<string> typeParameters,
        string constraints, int order)
    {
        Name = name;
        ParametersInfo = parametersInfo;
        TypeParameters = typeParameters;
        Constraints = constraints;
        Order = order;
    }

    public string Name { get; }
    public ParametersInfo ParametersInfo { get; }
    public EquatableArray<string> TypeParameters { get; }
    public string Constraints { get; }
    public int Order { get; }

    public static int ParseStepOrderEnum(string value)
    {
        return
            value switch
            {
                "StepOrder.One" => 1,
                "StepOrder.Two" => 2,
                "StepOrder.Three" => 3,
                "StepOrder.Four" => 4,
                "StepOrder.Five" => 5,
                "StepOrder.Six" => 6,
                "StepOrder.Seven" => 7,
                "StepOrder.Eight" => 8,
                "StepOrder.Nine" => 9,
                "StepOrder.Ten" => 10,
                "StepOrder.Eleven" => 11,
                "StepOrder.Twelve" => 12,
                "StepOrder.Thirteen" => 13,
                "StepOrder.Fourteen" => 14,
                "StepOrder.Fifteen" => 15,
                "StepOrder.Sixteen" => 16,
                _ => throw new NotImplementedException()
            };
    }
}
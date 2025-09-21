using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OrderedBuildersGenerator.Models;

namespace OrderedBuildersGenerator;

[Generator]
public class OrderedBuildersGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var builderInfoProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(StepBuilder).FullName!,
            static (_, _) => true,
            static (syntaxContext, _) =>
            {
                var classNameFromAttribute = syntaxContext.TargetNode
                    .TryCast<ClassDeclarationSyntax>()!.AttributeLists.SelectMany(static al => al.Attributes)
                    .First(static a =>
                        a.Name.TryCast<SimpleNameSyntax>()!.Identifier.Text is "StepBuilder" or "StepBuilderAttribute")
                    .ArgumentList?.Arguments
                    .FirstOrDefault()?
                    .ToString().Trim('"') ?? "";

                var unorderedStepsDeclarations = syntaxContext.TargetNode
                    .TryCast<ClassDeclarationSyntax>()!.Members
                    .Where(static member => member is MethodDeclarationSyntax)
                    .Where(static member => member.AttributeLists.SelectMany(static al => al.Attributes).Any(static a =>
                        a.Name.TryCast<SimpleNameSyntax>()!.Identifier.Text == "UnorderedStep"))
                    .Select(static member => member.TryCast<MethodDeclarationSyntax>()!)
                    .Select(static method =>
                        new UnorderedStepInfo(
                            method.Identifier.Text,
                            new ParametersInfo(
                                method.ParameterList.Parameters.Select(static p => p.Identifier.Text)
                                    .ToEquatableArray(),
                                method.ParameterList.Parameters.ToString()),
                            method.TypeParameterList?.Parameters.Select(t => t.ToString()).ToEquatableArray() ?? [],
                            method.ConstraintClauses.ToString()
                        ))
                    .ToEquatableArray();

                var orderedStepsDeclarations = syntaxContext.TargetNode
                    .TryCast<ClassDeclarationSyntax>()!.Members
                    .Where(static member => member is MethodDeclarationSyntax)
                    .Where(static method => method.AttributeLists.SelectMany(al => al.Attributes).Any(a =>
                        a.Name.TryCast<SimpleNameSyntax>()!.Identifier.Text is "OrderedStep" or "OrderedStepAttribute"))
                    .Select(static stepMethod => stepMethod.TryCast<MethodDeclarationSyntax>()!)
                    .Select(static method =>
                        new OrderedStepInfo(
                            method.Identifier.Text,
                            new ParametersInfo(
                                method.ParameterList.Parameters.Select(static p => p.Identifier.Text)
                                    .ToEquatableArray(),
                                method.ParameterList.Parameters.ToString()),
                            method.TypeParameterList?.Parameters.Select(t => t.ToString()).ToEquatableArray() ?? [],
                            method.ConstraintClauses.ToString(),
                            OrderedStepInfo.ParseStepOrderEnum(method.AttributeLists.SelectMany(al => al.Attributes)
                                .First(a =>
                                    a.Name.TryCast<SimpleNameSyntax>()!.Identifier.Text is "OrderedStep"
                                        or "OrderedStepAttribute").ArgumentList!
                                .Arguments[0].ToString())
                        ))
                    .ToEquatableArray();

                var buildStepsDeclarations = syntaxContext.TargetNode
                    .TryCast<ClassDeclarationSyntax>()!.Members
                    .Where(static member => member is MethodDeclarationSyntax)
                    .Where(static method => method.AttributeLists.SelectMany(al => al.Attributes).Any(a =>
                        a.Name.TryCast<SimpleNameSyntax>()!.Identifier.Text is "BuildStep" or "BuildStepAttribute"))
                    .Select(static stepMethod => stepMethod.TryCast<MethodDeclarationSyntax>()!)
                    .Select(static method =>
                        new BuildStepInfo(
                            method.Identifier.Text,
                            new ParametersInfo(
                                method.ParameterList.Parameters.Select(static p => p.Identifier.Text)
                                    .ToEquatableArray(),
                                method.ParameterList.Parameters.ToString()),
                            method.TypeParameterList?.Parameters.Select(t => t.ToString()).ToEquatableArray() ?? [],
                            method.ConstraintClauses.ToString(),
                            method.ReturnType.ToString()
                        ))
                    .ToEquatableArray();

                // Gather all 'using' directives from the source file containing the builder configuration.
                var usings =
                    syntaxContext.SemanticModel.SyntaxTree.GetCompilationUnitRoot().Usings
                        .Select(static u => u.ToString()).ToEquatableArray();

                // Retrieve details about the configuration class itself (name, namespace, generics, constraints).
                var classDeclaration = syntaxContext.TargetNode
                    .TryCast<ClassDeclarationSyntax>();
                var className = classDeclaration!.Identifier.Text;
                var classTypeParametersAndConstraints =
                (
                    classDeclaration.TypeParameterList?.Parameters.ToString() ?? "",
                    classDeclaration.ConstraintClauses.ToString()
                );

                var constructorsParametersInfos =
                    classDeclaration.Members
                        .Where(static constructor => constructor is ConstructorDeclarationSyntax)
                        .Select(static constructor => new ParametersInfo(
                            constructor.TryCast<ConstructorDeclarationSyntax>()!.ParameterList.Parameters
                                .Select(static p => p.Identifier.Text).ToEquatableArray(),
                            constructor.TryCast<ConstructorDeclarationSyntax>()!.ParameterList.Parameters.ToString()))
                        .ToEquatableArray();

                var builderNamespace = syntaxContext.TargetSymbol.ContainingNamespace.ToString();

                return new BuilderInfo(
                    usings,
                    className,
                    classNameFromAttribute,
                    classTypeParametersAndConstraints,
                    builderNamespace,
                    constructorsParametersInfos,
                    unorderedStepsDeclarations,
                    orderedStepsDeclarations,
                    buildStepsDeclarations
                );
            }
        );

        context.RegisterSourceOutput(
            builderInfoProvider,
            GenerateStepBuilders
        );
    }

    private void GenerateStepBuilders(SourceProductionContext spc, BuilderInfo b)
    {
        var classTypeParamsOnly = b.TypeParametersAndConstraints.Types; // e.g. "TFloor"
        var classTypeParamsDecl =
            string.IsNullOrWhiteSpace(classTypeParamsOnly) ? "" : $"<{classTypeParamsOnly}>"; // e.g. "<TFloor>"
        var classConstraints = string.IsNullOrWhiteSpace(b.TypeParametersAndConstraints.Constraints)
            ? ""
            : $" {b.TypeParametersAndConstraints.Constraints}";

        var genClassName = b.ClassNameFromAttribute != "" ? b.ClassNameFromAttribute : $"{b.ClassName}Generated";
        var @namespace = b.DeclaredNamespace == "<global namespace>"
            ? ""
            : $"namespace {b.DeclaredNamespace};";

        // Distinct ordered steps, grouped by order, ascending.
        var orderedByOrder = b.OrderedSteps
            .OrderBy(s => s.Order)
            .GroupBy(s => s.Order)
            .ToArray();

        var allOrders = orderedByOrder.Select(g => g.Key).OrderBy(x => x).ToArray();
        var hasOrdered = allOrders.Length > 0;

        var buildIfaceName = $"I{genClassName}_StepBuild";
        var buildIfaceFull = $"{buildIfaceName}{classTypeParamsDecl}";

        // If there are ordered steps, first ordered interface is the "entry" interface
        var entryIfaceName = hasOrdered ? $"I{genClassName}_Step{ToWord(allOrders.First())}" : buildIfaceName;
        var entryIfaceFull = $"{entryIfaceName}{classTypeParamsDecl}";

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        foreach (var u in b.Usings) sb.AppendLine(u);
        sb.AppendLine();
        sb.AppendLine($"{@namespace}");
        sb.AppendLine();

        // IUnorderedSteps<TStep, ...class T...>
        var unorderedIfaceGeneric = string.IsNullOrWhiteSpace(classTypeParamsOnly)
            ? "<TStep>"
            : $"<TStep, {classTypeParamsOnly}>";

        sb.AppendLine($"public interface I{genClassName}_UnorderedSteps{unorderedIfaceGeneric}{classConstraints}");
        sb.AppendLine("{");

        foreach (var m in b.UnorderedSteps)
        {
            var tpDecl = m.TypeParameters.Count > 0 ? $"<{string.Join(", ", m.TypeParameters)}>" : "";
            var constraints = string.IsNullOrWhiteSpace(m.Constraints) ? "" : $" {m.Constraints}";
            var paramText = m.ParametersInfo.ParametersFullText; // already "type name, type2 name2"
            sb.AppendLine($"    public TStep {m.Name}{tpDecl}({paramText}){constraints};");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        sb.AppendLine();


        // Ordered step interfaces: IStepOne<...>, IStepTwo<...>, ...
        if (hasOrdered)
            for (var i = 0; i < allOrders.Length; i++)
            {
                var order = allOrders[i];
                var thisWord = ToWord(order);
                var nextTypeName = i == allOrders.Length - 1
                    ? buildIfaceFull
                    : $"I{genClassName}_Step{ToWord(allOrders[i + 1])}{classTypeParamsDecl}";

                // interface header
                var ifaceName = $"I{genClassName}_Step{thisWord}";
                var ifaceFull = $"{ifaceName}{classTypeParamsDecl}";

                // inherits IUnorderedSteps<This, T...>
                var unorderedBase = string.IsNullOrWhiteSpace(classTypeParamsOnly)
                    ? $"I{genClassName}_UnorderedSteps<{ifaceFull}>"
                    : $"I{genClassName}_UnorderedSteps<{ifaceFull}, {classTypeParamsOnly}>";

                sb.AppendLine($"public interface {ifaceFull} : {unorderedBase}{classConstraints}");
                sb.AppendLine("{");

                // methods for this order
                foreach (var m in orderedByOrder.First(g => g.Key == order))
                {
                    var tpDecl = m.TypeParameters.Count > 0 ? $"<{string.Join(", ", m.TypeParameters)}>" : "";
                    var constraints = string.IsNullOrWhiteSpace(m.Constraints) ? "" : $" {m.Constraints}";
                    var paramText = m.ParametersInfo.ParametersFullText;

                    sb.AppendLine($"    public {nextTypeName} {m.Name}{tpDecl}({paramText}){constraints};");
                    sb.AppendLine();
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }

        // Build interface (terminal)
        {
            // inherits IUnorderedSteps<This, T...>
            var unorderedBase = string.IsNullOrWhiteSpace(classTypeParamsOnly)
                ? $"I{genClassName}_UnorderedSteps<{buildIfaceFull}>"
                : $"I{genClassName}_UnorderedSteps<{buildIfaceFull}, {classTypeParamsOnly}>";

            sb.AppendLine($"public interface {buildIfaceFull} : {unorderedBase}{classConstraints}");
            sb.AppendLine("{");

            foreach (var m in b.BuildSteps)
            {
                // build methods keep their own generic params & constraints & (original) return type
                var tpDecl = m.TypeParameters.Count > 0 ? $"<{string.Join(", ", m.TypeParameters)}>" : "";
                var constraints = string.IsNullOrWhiteSpace(m.Constraints) ? "" : $" {m.Constraints}";
                var paramText = m.ParametersInfo.ParametersFullText;

                sb.AppendLine($"    public {m.ReturnType} {m.Name}{tpDecl}({paramText}){constraints};");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Generated concrete builder class
        var genClassDecl = $"public class {genClassName}{classTypeParamsDecl} : {entryIfaceFull}{classConstraints}";

        sb.AppendLine(genClassDecl);
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {b.ClassName}{classTypeParamsDecl} _builder;");
        sb.AppendLine();

        // Constructors (mirror original)
        foreach (var ctor in b.ConstructorsParametersInfos)
        {
            var paramText = ctor.ParametersFullText;
            var argList = string.Join(", ", ctor.ParametersIdentifiers);
            sb.AppendLine($"    public {genClassName}({paramText})");
            sb.AppendLine("    {");
            sb.AppendLine($"        _builder = new {b.ClassName}{classTypeParamsDecl}({argList});");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // If the original type had no ctors, expose a default one that creates default(T)s (optional).
        if (b.ConstructorsParametersInfos.Count == 0)
        {
            sb.AppendLine($"    public {genClassName}()");
            sb.AppendLine("    {");
            sb.AppendLine($"        _builder = new {b.ClassName}{classTypeParamsDecl}();");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Unordered methods on the entry class
        foreach (var m in b.UnorderedSteps)
        {
            var tpDecl = m.TypeParameters.Count > 0 ? $"<{string.Join(", ", m.TypeParameters)}>" : "";
            var constraints = string.IsNullOrWhiteSpace(m.Constraints) ? "" : $" {m.Constraints}";
            var paramText = m.ParametersInfo.ParametersFullText;
            var args = string.Join(", ", m.ParametersInfo.ParametersIdentifiers);

            sb.AppendLine($"    public {entryIfaceFull} {m.Name}{tpDecl}({paramText}){constraints}");
            sb.AppendLine("    {");
            sb.AppendLine($"       _builder.{m.Name}{tpDecl}({args});");
            sb.AppendLine("        return this;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Ordered methods on the entry class (if any)
        if (hasOrdered)
        {
            var firstOrder = allOrders.First();
            var thisGroup = orderedByOrder.First(g => g.Key == firstOrder);

            var nextTypeName = allOrders.Length == 1
                ? buildIfaceFull
                : $"{genClassName}Step{ToWord(allOrders[1])}";

            foreach (var m in thisGroup)
            {
                var tpDecl = m.TypeParameters.Count > 0 ? $"<{string.Join(", ", m.TypeParameters)}>" : "";
                var constraints = string.IsNullOrWhiteSpace(m.Constraints) ? "" : $" {m.Constraints}";
                var paramText = m.ParametersInfo.ParametersFullText;
                var args = string.Join(", ", m.ParametersInfo.ParametersIdentifiers);

                var returns = allOrders.Length == 1
                    ? buildIfaceFull
                    : $"I{genClassName}_Step{ToWord(allOrders[1])}{classTypeParamsDecl}";

                sb.AppendLine($"    public {returns} {m.Name}{tpDecl}({paramText}){constraints}");
                sb.AppendLine("    {");
                sb.AppendLine($"        _builder.{m.Name}{tpDecl}({args});");

                if (allOrders.Length == 1)
                    sb.AppendLine($"        return new {genClassName}Step{b.BuildSteps.First().Name}(_builder);");
                else
                    sb.AppendLine($"        return new {nextTypeName}(_builder);");

                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        // Nested classes for the remaining ordered steps and the terminal build step
        if (hasOrdered)
            for (var i = 1; i < allOrders.Length; i++)
            {
                var order = allOrders[i];
                var thisWord = ToWord(order);

                var ifaceThis = $"I{genClassName}_Step{thisWord}{classTypeParamsDecl}";
                var classThis = $"{genClassName}Step{thisWord}";

                var nextPublicIface = i == allOrders.Length - 1
                    ? buildIfaceFull
                    : $"I{genClassName}_Step{ToWord(allOrders[i + 1])}{classTypeParamsDecl}";

                var nextClass = i == allOrders.Length - 1
                    ? $"{genClassName}Step{b.BuildSteps.First().Name}"
                    : $"{genClassName}Step{ToWord(allOrders[i + 1])}";

                sb.AppendLine($"    private class {classThis} : {ifaceThis}");
                sb.AppendLine("    {");
                sb.AppendLine($"        private readonly {b.ClassName}{classTypeParamsDecl} _builder;");
                sb.AppendLine();
                sb.AppendLine($"        public {classThis}({b.ClassName}{classTypeParamsDecl} builder)");
                sb.AppendLine("        {");
                sb.AppendLine("            _builder = builder;");
                sb.AppendLine("        }");
                sb.AppendLine();

                // unordered methods return this
                foreach (var um in b.UnorderedSteps)
                {
                    var tpDecl = um.TypeParameters.Count > 0 ? $"<{string.Join(", ", um.TypeParameters)}>" : "";
                    var constraints = string.IsNullOrWhiteSpace(um.Constraints) ? "" : $" {um.Constraints}";
                    var paramText = um.ParametersInfo.ParametersFullText;
                    var args = string.Join(", ", um.ParametersInfo.ParametersIdentifiers);

                    sb.AppendLine($"        public {ifaceThis} {um.Name}{tpDecl}({paramText}){constraints}");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            _builder.{um.Name}{tpDecl}({args});");
                    sb.AppendLine("             return this;");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }

                // ordered methods jump to next
                foreach (var om in orderedByOrder.First(g => g.Key == order))
                {
                    var tpDecl = om.TypeParameters.Count > 0 ? $"<{string.Join(", ", om.TypeParameters)}>" : "";
                    var constraints = string.IsNullOrWhiteSpace(om.Constraints) ? "" : $" {om.Constraints}";
                    var paramText = om.ParametersInfo.ParametersFullText;
                    var args = string.Join(", ", om.ParametersInfo.ParametersIdentifiers);

                    sb.AppendLine($"        public {nextPublicIface} {om.Name}{tpDecl}({paramText}){constraints}");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            _builder.{om.Name}{tpDecl}({args});");
                    sb.AppendLine($"            return new {nextClass}(_builder);");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }

        EmitTerminal(hasOrdered, genClassName, b, buildIfaceFull, sb, classTypeParamsDecl);

        sb.AppendLine("}");

        spc.AddSource($"{genClassName}.g.cs", sb.ToString());
    }

    private static string ToWord(int n)
    {
        return n switch
        {
            1 => "One",
            2 => "Two",
            3 => "Three",
            4 => "Four",
            5 => "Five",
            6 => "Six",
            7 => "Seven",
            8 => "Eight",
            9 => "Nine",
            10 => "Ten",
            11 => "Eleven",
            12 => "Twelve",
            13 => "Thirteen",
            14 => "Fourteen",
            15 => "Fifteen",
            16 => "Sixteen",
            _ => throw new NotImplementedException()
        };
    }

    private static void EmitTerminal(bool hasOrdered, string? genClassName, BuilderInfo b, string? buildIfaceFull,
        StringBuilder sb, string? classTypeParamsDecl)
    {
        var className = hasOrdered
            ? $"{genClassName}Step{b.BuildSteps.First().Name}"
            : $"{genClassName}"; // entry class is terminal when no ordered steps

        var ifaceName = buildIfaceFull;

        if (hasOrdered)
        {
            sb.AppendLine($"    private class {className} : {ifaceName}");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly {b.ClassName}{classTypeParamsDecl} _builder;");
            sb.AppendLine();
            sb.AppendLine($"        public {className}({b.ClassName}{classTypeParamsDecl} builder)");
            sb.AppendLine("        {");
            sb.AppendLine("            _builder = builder;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        foreach (var bm in b.BuildSteps)
        {
            var tpDecl = bm.TypeParameters.Count > 0 ? $"<{string.Join(", ", bm.TypeParameters)}>" : "";
            var constraints = string.IsNullOrWhiteSpace(bm.Constraints) ? "" : $" {bm.Constraints}";
            var paramText = bm.ParametersInfo.ParametersFullText;
            var args = string.Join(", ", bm.ParametersInfo.ParametersIdentifiers);

            var header = hasOrdered
                ? $"        public {bm.ReturnType} {bm.Name}{tpDecl}({paramText}){constraints}"
                : $"    public {bm.ReturnType} {bm.Name}{tpDecl}({paramText}){constraints}";

            sb.AppendLine(header);
            sb.AppendLine("        {");
            sb.AppendLine($"            return _builder.{bm.Name}{tpDecl}({args});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Unordered methods
        foreach (var m in b.UnorderedSteps)
        {
            var tpDecl = m.TypeParameters.Count > 0 ? $"<{string.Join(", ", m.TypeParameters)}>" : "";
            var constraints = string.IsNullOrWhiteSpace(m.Constraints) ? "" : $" {m.Constraints}";
            var paramText = m.ParametersInfo.ParametersFullText;
            var args = string.Join(", ", m.ParametersInfo.ParametersIdentifiers);

            sb.AppendLine($"        public {ifaceName} {m.Name}{tpDecl}({paramText}){constraints}");
            sb.AppendLine("        {");
            sb.AppendLine($"           _builder.{m.Name}{tpDecl}({args});");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        if (hasOrdered) sb.AppendLine("    }");
    }
}
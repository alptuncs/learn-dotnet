﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen;

[Generator(LanguageNames.CSharp)]
public partial class JsonSchemaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;
        var additionalTextsProvider = context.AdditionalTextsProvider.Where(at => at.Path.EndsWith("analyzer.config.json")).Select((text, _) => text.GetText());

        var combine = compilationProvider.Combine(additionalTextsProvider.Collect());

        context.RegisterSourceOutput(combine, (context, compilation) =>
        {
            var config = compilation.Right.FirstOrDefault();
            var analyzerConfig = config != null ? config.Deserialize<AnalyzerConfig>() : null;

            if (analyzerConfig?.ControllerServicesNamespace == null) return;

            context.AddSource(
                $"{compilation.Left.SourceModule.Name.Split('.').First()}.generated.cs",
                ServiceModelTemplateAsCs(
                    GetServiceModel(compilation.Left, analyzerConfig).Serialize()
                )
            );
        });
    }

    private List<ServiceModel> GetServiceModel(Compilation compilation, AnalyzerConfig config)
    {
        var result = new List<ServiceModel>();

        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);

            var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDeclaration in classDeclarations)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

                if (classSymbol?.ContainingNamespace?.ToString() == config.ControllerServicesNamespace &&
                    classSymbol?.DeclaredAccessibility == Accessibility.Public)
                {
                    ServiceModel applicationModel = new();

                    applicationModel.TargetNamespace = config.TargetProject;
                    applicationModel.Namespace = classSymbol.ContainingNamespace.ToString();
                    applicationModel.Name = classSymbol?.Name;

                    var methods = classSymbol?.GetMembers()
                        .OfType<IMethodSymbol>()
                        .Where(m => m.MethodKind == MethodKind.Ordinary)
                        .Select(m => new Operation()
                        {
                            Name = m.Name,
                            Type = m.ReturnType.MetadataName,
                            ReturnValue = $"This is {m.Name} from {classSymbol.Name}"
                        }).ToArray();

                    applicationModel.Operations = methods;
                    result.Add(applicationModel);
                }
            }
        }

        return result;
    }

    private string ServiceModelTemplateAsCs(string source) =>
$@"
/* Auto Generated
===JSON BEGIN===
{source.Replace('"', '\'')}
===JSON END===
*/";
}

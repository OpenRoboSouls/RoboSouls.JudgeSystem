using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Utils;

namespace SourceGenerator;

[Generator]
public class HashedGenerator: IIncrementalGenerator
{
    private struct HashContext
    {
        public string NamespaceName;
        public string ClassName;
        public string PropertyName;
        public string Accessibility;
    }

    private const string AttributeName = "Hashed";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: Predicate,
                transform: GetSemanticTarget);

        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool Predicate(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not PropertyDeclarationSyntax p)
        {
            return false;
        }

        if (p.AttributeLists.Count == 0)
        {
            return false;
        }

        return p.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == AttributeName));
    }

    private static HashContext? GetSemanticTarget(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var p = (PropertyDeclarationSyntax)context.Node;
        var attribute = p.AttributeLists.SelectMany(al => al.Attributes)
            .FirstOrDefault(a => context.SemanticModel.GetTypeInfo(a).Type?.Name == AttributeName);

        if (attribute == null)
        {
            return null;
        }
        
        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);
        if (symbol == null)
        {
            return null;
        }

        var propertyName = p.Identifier.ValueText;
        var className = symbol.ContainingType.Name;
        var ns = symbol.ContainingNamespace.ToString();
        var accessibility = new HashSet<string> { symbol.DeclaredAccessibility.ToString().ToLower() };

        if (symbol.IsStatic)
        {
            accessibility.Add("static");
        }

        if (symbol.IsVirtual)
        {
            accessibility.Add("virtual");
        }

        if (symbol.IsOverride)
        {
            accessibility.Add("override");
        }
        
        return new HashContext
        {
            NamespaceName = ns,
            ClassName = className,
            PropertyName = propertyName,
            Accessibility = string.Join(" ", accessibility)
        };
    }

    private static void Execute(SourceProductionContext spc, HashContext? target)
    {
        if (target == null)
        {
            return;
        }

        var value = target.Value;
        var hash = Hash.HashCode(value.PropertyName);

        var code = $$"""
                     using RoboSouls.JudgeSystem;

                     namespace {{value.NamespaceName}}
                     {
                         partial class {{value.ClassName}}
                         {
                             {{value.Accessibility}} partial int {{value.PropertyName}} => {{hash}};
                         }
                     }
                     """;

        spc.AddSource($"{value.ClassName}.{value.PropertyName}.g.cs", code);
    }
}

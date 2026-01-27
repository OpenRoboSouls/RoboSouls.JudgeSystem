using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoboSouls.JudgeSystem;
using RoboSouls.JudgeSystem.Attributes;

namespace SourceGenerator;

[Generator]
public class PropertyGenerator: IIncrementalGenerator
{
    private struct PropertyContext
    {
        public string NamespaceName;
        public string ClassName;
        public string PropertyName;
        public string PropertyType;
        public string Accessibility;
        public string GetterAccessibility;
        public string SetterAccessibility;
        public string ProviderVarName;
        public bool ServerNamespace;
    }

    private const string AttributeName = nameof(Property);
    
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
    
    private static PropertyContext? GetSemanticTarget(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var p = (PropertyDeclarationSyntax)context.Node;
        var attribute = p.AttributeLists.SelectMany(al => al.Attributes)
            .FirstOrDefault(a => context.SemanticModel.GetTypeInfo(a).Type?.Name == AttributeName);

        if (attribute?.ArgumentList == null)
        {
            return null;
        }
        
        // public class Property(string storageProvider, bool serverNamespace = false): Attribute
        var attributeArguments = attribute.ArgumentList.Arguments;
        var storageProvider = attributeArguments[0].ToString();
        var serverNamespace = attributeArguments.Count > 1 && attributeArguments[1].ToString() == "true";

        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not IPropertySymbol symbol)
        {
            return null;
        }

        var propertyName = p.Identifier.ValueText;
        var className = symbol.ContainingType.Name;
        var ns = symbol.ContainingNamespace.ToString();
        var propertyType = symbol.Type.ToString();
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
        
        var getterAccess = symbol.GetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable;
        var setterAccess = symbol.SetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable;
        
        return new PropertyContext()
        {
            NamespaceName = ns,
            ClassName = className,
            PropertyName = propertyName,
            PropertyType = propertyType,
            Accessibility = string.Join(" ", accessibility),
            GetterAccessibility = getterAccess.ToString().ToLower(),
            SetterAccessibility = setterAccess.ToString().ToLower(),
            ProviderVarName = storageProvider,
            ServerNamespace = serverNamespace
        };
    }

    private static void Execute(SourceProductionContext spc, PropertyContext? target)
    {
        if (target == null)
        {
            return;
        }

        var value = target.Value;
        var key = value.PropertyName.Sum();

        var identitySegment = $$"""
                                public {{value.PropertyType}} Get{{value.PropertyName}}(Identity id) => {{value.ProviderVarName}}.WithReaderNamespace(id).Load({{key}});
                                private void Set{{value.PropertyName}}(Identity id, int value) => {{value.ProviderVarName}}.WithWriterNamespace(id).Save({{key}}, value);
                                """;

        var campSegment = $$"""
                            public int Get{{value.PropertyName}}(Camp camp) => {{value.ProviderVarName}}.WithReaderNamespace(new Identity(camp, 0)).Load({{key}});
                            private void Set{{value.PropertyName}}(Camp camp, int value) => {{value.ProviderVarName}}.WithWriterNamespace(new Identity(camp, 0)).Save({{key}}, value);
                            """;

        var code = $$"""
                     using RoboSouls.JudgeSystem;

                     namespace {{value.NamespaceName}}
                     {
                        partial class {{value.ClassName}}
                        {
                            public partial int RadarCounterCount
                            {
                                get => intCache.Load({{key}});
                                private set => intCache.Save({{key}}, value);
                            }
                         
                            public int GetRadarCounterCount(Identity id) => intCache.WithReaderNamespace(id).Load({{key}});
                            private void SetRadarCounterCount(Identity id, int value) => intCache.WithWriterNamespace(id).Save({{key}}, value);
                         
                            public int GetRadarCounterCount(Camp camp) => intCache.WithReaderNamespace(new Identity(camp, 0)).Load({{key}});
                            private void SetRadarCounterCount(Camp camp, int value) => intCache.WithWriterNamespace(new Identity(camp, 0)).Save({{key}}, value);
                         }
                     }
                     """;
        
        spc.AddSource(value.ClassName + ".g.cs", code);
    }
}
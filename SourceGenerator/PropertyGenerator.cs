using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Utils;

namespace SourceGenerator;

[Generator]
public class PropertyGenerator: IIncrementalGenerator
{
    [Flags]
    private enum PropertyStorageMode
    {
        Single,
        Identity,
        Camp
    }
    
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
        public PropertyStorageMode StorageMode;
        public string IdVar;
    }

    private const string AttributeName = "Property";
    
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
            // return null;
            return new PropertyContext
            {
                PropertyName = "attribute argument list is null",
                ClassName = p.Identifier.ValueText
            };
        }
        
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not IPropertySymbol symbol)
        {
            // return null;
            return new PropertyContext
            {
                PropertyName = context.SemanticModel.GetDeclaredSymbol(context.Node)?.ToDisplayString() ?? "symbol is null",
                ClassName = p.Identifier.ValueText
            };
        }
        
        var attributeData = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "RoboSouls.JudgeSystem.Attributes.Property");
        
        if (attributeData == null)
        {
            // return null;
            return new PropertyContext
            {
                PropertyName = "attribute data is null： " + symbol.GetAttributes().Length + " " + string.Join(", ", symbol.GetAttributes().Select(a => a.AttributeClass?.ToDisplayString())),
                ClassName = p.Identifier.ValueText
            };
        }
        
        var storageProvider = attributeData.ConstructorArguments[0].Value?.ToString();
        var modeArgument = attributeData.ConstructorArguments[1];
        var modeValue = (PropertyStorageMode)(modeArgument.Value ?? 0);
        var idArgument = attributeData.ConstructorArguments.Length > 2
            ? attributeData.ConstructorArguments[2].Value?.ToString()
            : null;
        
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
        
        return new PropertyContext
        {
            NamespaceName = ns,
            ClassName = className,
            PropertyName = propertyName,
            PropertyType = propertyType,
            Accessibility = string.Join(" ", accessibility),
            GetterAccessibility = getterAccess.ToString().ToLower(),
            SetterAccessibility = setterAccess.ToString().ToLower(),
            ProviderVarName = storageProvider,
            StorageMode = modeValue,
            IdVar = idArgument
        };
    }

    private static void Execute(SourceProductionContext spc, PropertyContext? target)
    {
        if (target == null)
        {
            return;
        }

        var value = target.Value;
        var key = Hash.HashCode(value.PropertyName);

        var identitySegment = $"""
                                {value.GetterAccessibility} {value.PropertyType} Get{value.PropertyName}(in Identity id) => {value.ProviderVarName}.WithReaderNamespace(id).Load({key});
                                {value.SetterAccessibility} void Set{value.PropertyName}(in Identity id, {value.PropertyType} value) => {value.ProviderVarName}.WithWriterNamespace(id).Save({key}, value);
                                """;
        var hasIdentity = (value.StorageMode & PropertyStorageMode.Identity) != 0;

        var campSegment = $"""
                            {value.GetterAccessibility} {value.PropertyType} Get{value.PropertyName}(in Camp camp) => {value.ProviderVarName}.WithReaderNamespace(new Identity(camp, 0)).Load({key});
                            {value.SetterAccessibility} void Set{value.PropertyName}(in Camp camp, {value.PropertyType} value) => {value.ProviderVarName}.WithWriterNamespace(new Identity(camp, 0)).Save({key}, value);
                            """;
        var hasCamp = (value.StorageMode & PropertyStorageMode.Camp) != 0;
        
        var singleId = string.IsNullOrEmpty(value.IdVar) ? "Identity.Server" : value.IdVar;

        var code = $$"""
                     using RoboSouls.JudgeSystem;

                     namespace {{value.NamespaceName}}
                     {
                        partial class {{value.ClassName}}
                        {
                            public partial {{value.PropertyType}} {{value.PropertyName}}
                            {
                                get => {{value.ProviderVarName}}.WithReaderNamespace({{singleId}}).Load({{key}});
                                {{value.SetterAccessibility}} set => {{value.ProviderVarName}}.WithWriterNamespace({{singleId}}).Save({{key}}, value);
                            }
                         
                            {{(hasCamp ? campSegment : "")}}
                         
                            {{(hasIdentity ? identitySegment : "")}}
                         }
                     }
                     """;
        
        spc.AddSource($"{value.ClassName}.{value.PropertyName}.g.cs", code);
    }
}
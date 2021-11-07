//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using Microsoft.Build.Locator;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.FindSymbols;
//using Microsoft.CodeAnalysis.MSBuild;


//namespace Core
//{

//    class OldApproach
//    {
//        private static string SolutionPath;
//        private static string NameOfTheProjectThatContainsCommandBusClass;
//        private static string CommandBusClassFullyQualifiedMetadataName;
//        private static string CommandBusSendMethodName;
//        private static string CommandInterfaceTypeFullyQualifiedMetadataName;

//        private static INamedTypeSymbol _commandInteraceType;
//        private static int _tempCount = 0;

//        private static Dictionary<string, SemanticModel> _semanticModelsCache = new Dictionary<string, SemanticModel>();

//        private static void AnalyzeArgumentsOfCallerMethods(Solution solution, IMethodSymbol calledMethodSymbol)
//        {
//            var callers = SymbolFinder.FindCallersAsync(calledMethodSymbol, solution).Result;

//            foreach (var callerInfo in callers)
//            {
//                foreach (var callerLocation in callerInfo.Locations)
//                {
//                    var document = solution.GetDocument(callerLocation.SourceTree);
//                    // excluding test projects. who needs tests?
//                    if (document.Project.Name.Contains("Test"))
//                    {
//                        break;
//                    }

//                    if (!_semanticModelsCache.TryGetValue(document.FilePath, out var semanticModel))
//                    {
//                        semanticModel = document.GetSemanticModelAsync().Result;
//                        _semanticModelsCache[document.FilePath] = semanticModel;
//                    }

//                    var syntaxTreeRoot = document.GetSyntaxRootAsync().Result;
//                    var nodeAtCallerLocation = syntaxTreeRoot.FindNode(callerLocation.SourceSpan);
//                    // check for delegates. FindCallersAsync returns all references, not only actual calls
//                    if (callerLocation.SourceTree.ToString()[callerLocation.SourceSpan.End] != '(')
//                    {
//                        break;
//                    }
//                    // nodeAtCallerLocation is not an invocation node itself but a descendant of it
//                    var methodInvocationNode = FindClosestAncestor<InvocationExpressionSyntax>(nodeAtCallerLocation, SyntaxKind.InvocationExpression);

//                    foreach (var arg in methodInvocationNode.ArgumentList.Arguments)
//                    {
//                        var argChildren = arg.ChildNodes().ToArray();
//                        if (argChildren.Length != 1)
//                        {
//                            System.Diagnostics.Debugger.Break(); // argument node has not exactly one child
//                        }

//                        var variableType = semanticModel.GetTypeInfo(argChildren[0]).Type;
//                        if (variableType == null || (variableType.ToString() != CommandInterfaceTypeFullyQualifiedMetadataName && !IsCommand(variableType)))
//                        {
//                            System.Diagnostics.Debugger.Break();
//                        }

//                        AnalyzeSyntaxNode(solution, callerLocation, semanticModel, syntaxTreeRoot, argChildren[0]);
//                    }
//                }
//            }
//        }

//        private static void AnalyzeSyntaxNode(
//            Solution solution,
//            Location callerLocation,
//            SemanticModel semanticModel,
//            SyntaxNode syntaxTreeRoot,
//            SyntaxNode node)
//        {
//            var nodeKind = node.Kind();
//            switch (nodeKind)
//            {
//                case SyntaxKind.ObjectCreationExpression:
//                    AnalyzeObjectCreationNode(semanticModel, node as ObjectCreationExpressionSyntax);
//                    break;

//                case SyntaxKind.IdentifierName:
//                    var variableNode = node as IdentifierNameSyntax;
//                    var parentMethodDeclarationNode = FindClosestAncestor<MethodDeclarationSyntax>(node, SyntaxKind.MethodDeclaration);
//                    AnalyzeVariableFlow(solution, callerLocation, semanticModel, syntaxTreeRoot, parentMethodDeclarationNode, variableNode);
//                    break;

//                case SyntaxKind.ImplicitArrayCreationExpression:
//                case SyntaxKind.ArrayCreationExpression:
//                    System.Diagnostics.Debugger.Break();
//                    break;
//                case SyntaxKind.InvocationExpression:
//                    System.Diagnostics.Debugger.Break();
//                    break;
//                default:
//                    System.Diagnostics.Debugger.Break(); // argument node has unknown kind
//                    break;
//            }
//        }

//        private static void AnalyzeVariableFlow(Solution solution,
//            Location callerLocation,
//            SemanticModel semanticModel,
//            SyntaxNode syntaxTreeRoot,
//            MethodDeclarationSyntax parentMethodDeclarationNode,
//            IdentifierNameSyntax variableNode)
//        {
//            var variableType = semanticModel.GetTypeInfo(variableNode).Type;
//            switch (variableType.TypeKind)
//            {
//                case TypeKind.Class:
//                case TypeKind.Array:
//                case TypeKind.Interface:
//                    var variableText = variableNode.Identifier.Text;
//                    if (parentMethodDeclarationNode.ParameterList.Parameters.Any(x => x.Identifier.Text == variableText))
//                    {
//                        var parentMethodSymbol = semanticModel.GetDeclaredSymbol(parentMethodDeclarationNode);
//                        AnalyzeArgumentsOfCallerMethods(solution, parentMethodSymbol);
//                    }
//                    else
//                    {
//                        var variableSymbol = semanticModel.GetSymbolInfo(variableNode).Symbol;
//                        var variableReferences = SymbolFinder.FindReferencesAsync(variableSymbol, solution).Result;
//                        if (variableReferences.Count() > 1)
//                        {
//                            throw new Exception(); // if this hits check why!!!
//                        }


//                        var variableAssignmentLocation = variableSymbol.Locations
//                            .OrderByDescending(x => x.SourceSpan.Start)
//                            .First(x => x.SourceSpan.Start < callerLocation.SourceSpan.Start);
//                        var variableAssignmentNode = syntaxTreeRoot.FindNode(variableAssignmentLocation.SourceSpan);
//                        var variableAssignmentNodeKind = variableAssignmentNode.Kind();



//                        switch (variableAssignmentNodeKind)
//                        {
//                            case SyntaxKind.ForEachStatement:
//                                AnalyzeForEach(solution, callerLocation, semanticModel, syntaxTreeRoot, parentMethodDeclarationNode, variableAssignmentNode);
//                                break;

//                            case SyntaxKind.VariableDeclarator:
//                                var rValueInAssignment = variableAssignmentNode
//                                    .ChildNodes().OfType<EqualsValueClauseSyntax>().First()
//                                    .ChildNodes().Last();

//                                var rValueKind = rValueInAssignment.Kind();
//                                switch (rValueKind)
//                                {
//                                    case SyntaxKind.ArrayCreationExpression:
//                                        System.Diagnostics.Debugger.Break();
//                                        break;
//                                    case SyntaxKind.ObjectCreationExpression:
//                                        AnalyzeObjectCreationNode(semanticModel, rValueInAssignment as ObjectCreationExpressionSyntax);
//                                        break;
//                                    case SyntaxKind.ConditionalExpression:
//                                        var conditionalNode = rValueInAssignment as ConditionalExpressionSyntax;
//                                        AnalyzeSyntaxNode(solution, callerLocation, semanticModel, syntaxTreeRoot, conditionalNode.WhenTrue);
//                                        AnalyzeSyntaxNode(solution, callerLocation, semanticModel, syntaxTreeRoot, conditionalNode.WhenFalse);
//                                        break;
//                                    default:
//                                        System.Diagnostics.Debugger.Break();
//                                        break;
//                                }
//                                break;

//                            default:
//                                System.Diagnostics.Debugger.Break();
//                                break;
//                        }
//                    }
//                    break;
//                case TypeKind.TypeParameter:
//                    AnalyzeCallersWithTypeParameter(solution, semanticModel, parentMethodDeclarationNode);
//                    break;
//                default:
//                    System.Diagnostics.Debugger.Break(); // variable node has unknown kind
//                    break;
//            }
//        }

//        private static void AnalyzeForEach(Solution solution, Location callerLocation, SemanticModel semanticModel, SyntaxNode syntaxTreeRoot, MethodDeclarationSyntax parentMethodNode, SyntaxNode variableAssignmentNode)
//        {
//            var foreachNode = variableAssignmentNode as ForEachStatementSyntax;
//            var rirghtValueInAssignment = foreachNode.Expression as IdentifierNameSyntax;
//            if (rirghtValueInAssignment == null)
//            {
//                System.Diagnostics.Debugger.Break(); // foreach contains not just a variable
//            }
//            AnalyzeVariableFlow(solution, callerLocation, semanticModel, syntaxTreeRoot, parentMethodNode, rirghtValueInAssignment);
//        }

//        private static void AnalyzeObjectCreationNode(SemanticModel semanticModel, ObjectCreationExpressionSyntax objCreationNode)
//        {
//            var objCreationNodeType = semanticModel.GetTypeInfo(objCreationNode).Type;
//            if (objCreationNodeType == null)
//            {
//                System.Diagnostics.Debugger.Break();
//            }
//            if (IsCommand(objCreationNodeType))
//            {
//                AddCommandSendingPoint();
//            }
//            else
//            {
//                System.Diagnostics.Debugger.Break();
//            }
//        }

//        private static void AnalyzeCallersWithTypeParameter(Solution solution, SemanticModel semanticModel, MethodDeclarationSyntax parentMethodNode)
//        {
//            var calledMethodSymbol = semanticModel.GetDeclaredSymbol(parentMethodNode);
//            var callers = SymbolFinder.FindCallersAsync(calledMethodSymbol, solution).Result;

//            foreach (var callerInfo in callers)
//            {
//                foreach (var callerLocation in callerInfo.Locations)
//                {
//                    // TODO: to do
//                }
//            }
//        }

//        static void AddCommandSendingPoint()
//        {
//            _tempCount++;
//        }

//        static T FindClosestAncestor<T>(SyntaxNode node, SyntaxKind ancestorSyntaxKind) where T : SyntaxNode
//        {
//            var ancestorNode = node.Parent;
//            while (!ancestorNode.IsKind(ancestorSyntaxKind))
//            {
//                ancestorNode = ancestorNode.Parent;
//            }

//            return (T)ancestorNode;
//        }

//        static bool IsCommand(ITypeSymbol symbol)
//        {
//            return symbol.AllInterfaces.Any(i => CommandInterfaceTypeFullyQualifiedMetadataName == i.ToString());
//        }
//    }
//}

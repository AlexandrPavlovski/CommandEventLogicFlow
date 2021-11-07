using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Graph;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Core
{
    public class Analyzer
    {
        private readonly Config _cfg;

        private const int MethodsCalledByHadlersCachingDepth = 2;

        private Solution _solution;
        private INamedTypeSymbol _commandInteraceTypeSymbol;
        private INamedTypeSymbol _eventInteraceTypeSymbol;

        private readonly SymbolEqualityComparer _symbolEqualityComparer;

        private readonly Dictionary<string, SemanticModel> _semanticModelsCache = new Dictionary<string, SemanticModel>();

        private readonly Dictionary<ITypeSymbol, List<InstantiationInfo>> _commandInstantiations;
        private readonly Dictionary<ITypeSymbol, List<InstantiationInfo>> _eventInstantiations;

        private readonly Dictionary<ITypeSymbol, List<HandlerInfo>> _commandHandlers;
        private readonly Dictionary<ITypeSymbol, List<HandlerInfo>> _eventHandlers;

        private readonly Dictionary<IMethodSymbol, List<ITypeSymbol>> _methodsThatDirectlyInstantiateCommands;
        private readonly Dictionary<IMethodSymbol, List<ITypeSymbol>> _methodsThatDirectlyInstantiateEvents;

        private readonly Dictionary<IMethodSymbol, List<ITypeSymbol>> _allInstantiatedTypesCache;

        private List<string> temp = new List<string>();

        public Analyzer(Solution solution, Config cfg)
        {
            _solution = solution;
            _cfg = cfg;

            _symbolEqualityComparer = new SymbolEqualityComparer();
            _commandInstantiations = new Dictionary<ITypeSymbol, List<InstantiationInfo>>(_symbolEqualityComparer);
            _eventInstantiations = new Dictionary<ITypeSymbol, List<InstantiationInfo>>(_symbolEqualityComparer);

            _commandHandlers = new Dictionary<ITypeSymbol, List<HandlerInfo>>(_symbolEqualityComparer);
            _eventHandlers = new Dictionary<ITypeSymbol, List<HandlerInfo>>(_symbolEqualityComparer);

            _methodsThatDirectlyInstantiateCommands = new Dictionary<IMethodSymbol, List<ITypeSymbol>>(_symbolEqualityComparer);
            _methodsThatDirectlyInstantiateEvents = new Dictionary<IMethodSymbol, List<ITypeSymbol>>(_symbolEqualityComparer);

            _allInstantiatedTypesCache = new Dictionary<IMethodSymbol, List<ITypeSymbol>>(_symbolEqualityComparer);
        }

        public void Start()
        {
            var project = _solution.Projects.FirstOrDefault(x => x.Name == _cfg.ProjectThatContainsCommandInterface);
            if (project == null)
            {
                return;
            }
            var compilation = project.GetCompilationAsync().Result;
            _commandInteraceTypeSymbol = compilation.GetTypeByMetadataName(_cfg.CommandInterfaceTypeNameWithNamespace);

            project = _solution.Projects.FirstOrDefault(x => x.Name == _cfg.ProjectThatContainsEventInterface);
            if (project == null)
            {
                return;
            }
            compilation = project.GetCompilationAsync().Result;
            _eventInteraceTypeSymbol = compilation.GetTypeByMetadataName(_cfg.EventInterfaceTypeNameWithNamespace);

            FindInstantiationsAndHandlers(_commandInteraceTypeSymbol, true);
            FindInstantiationsAndHandlers(_eventInteraceTypeSymbol, false);

            FindCommandToEventRelations();

            //File.WriteAllLines(@"C:/temp.cs", temp);
        }

        public CommandsEventsGraph GetCommandsEventsGraph()
        {
            var graph = new CommandsEventsGraph();

            if (_commandInstantiations.Any())
            {
                // todo
            }

            graph.Commands = new GraphNode[]
            {
                new GraphNode()
                {
                    Name = "lol kek",
                    Children = new GraphNode[]
                    {
                        new GraphNode()
                        {
                            Name = "pep bub"
                        }
                    }
                }
            };

            return graph;
        }

        private void FindInstantiationsAndHandlers(ITypeSymbol typeSymbol, bool isCommand)
        {
            var allTypeReferences = SymbolFinder.FindReferencesAsync(typeSymbol, _solution).Result;
            if (allTypeReferences.Count() > 2)
            {
                throw new Exception("More references than expected");
            }

            // the first element in collection is always a list of references to the type itself
            foreach (var refLoc in allTypeReferences.First().Locations)
            {
                var treeRoot = refLoc.Location.SourceTree.GetRoot();
                var node = treeRoot.FindNode(refLoc.Location.SourceSpan);

                var nodeKind = node.Kind();
                SemanticModel semanticModel;
                switch (nodeKind)
                {
                    case SyntaxKind.SimpleBaseType:
                        semanticModel = GetSemanticModel(refLoc.Location.SourceTree);
                        var inheritorTypeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(node.Parent.Parent);
                        if (inheritorTypeSymbol == null)
                        {
                            throw new Exception("GetDeclaredSymbol returned something unexpected");
                        }

                        FindInstantiationsAndHandlers(inheritorTypeSymbol, isCommand);
                        break;

                    case SyntaxKind.IdentifierName:
                        if (node.Parent.IsKind(SyntaxKind.ObjectCreationExpression)
                            || node.Parent.Parent.IsKind(SyntaxKind.ObjectCreationExpression))
                        {
                            AddInstantiationInfo(typeSymbol, refLoc, node, isCommand);
                        }
                        else
                        {
                            if (typeSymbol.TypeKind == TypeKind.Interface)
                            {
                                break;
                            }

                            if (node.Parent.IsKind(SyntaxKind.Parameter))
                            {
                                if (node.Parent.Parent.Parent.IsKind(SyntaxKind.MethodDeclaration))
                                {
                                    semanticModel = GetSemanticModel(refLoc.Location.SourceTree);
                                    var methodDeclarationSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(node.Parent.Parent.Parent);

                                    if (IsHandlerType(methodDeclarationSymbol.ContainingType)
                                        && _cfg.HandlerMethodNames.Contains(methodDeclarationSymbol.Name))
                                    {
                                        AddHandler(typeSymbol, methodDeclarationSymbol, node.Parent.Parent.Parent, isCommand);
                                    }
                                    //else
                                    //{
                                    //    if (!methodDeclarationSymbol.ToString().Contains("Aggregate"))
                                    //    {
                                    //        temp.Add(methodDeclarationSymbol.ToString());
                                    //        temp.Add("=====================================================================");
                                    //    }
                                    //}
                                }
                                //else
                                //{
                                //    System.Diagnostics.Debugger.Break();
                                //}
                            }
                            else if (node.Parent.IsKind(SyntaxKind.TypeArgumentList))
                            {
                                if (node.Parent.Parent.Parent.ToString().StartsWith("Mapper.Map<"))
                                {
                                    AddInstantiationInfo(typeSymbol, refLoc, node, isCommand);
                                }
                                //else
                                //{
                                //    temp.Add(commandNode.Parent.Parent.ToString());
                                //    temp.Add("=====================================================================");
                                //}
                            }
                            //else
                            //{
                            //    temp.Add(commandNode.Parent.ToString());
                            //    temp.Add("=====================================================================");
                            //}
                        }
                        break;

                    case SyntaxKind.TypeConstraint:
                        break;

                    default:
                        throw new Exception("Unsupported syntax kind");
                }
            }

            // the second element in collection is always a list of references to the type's constructor
            if (allTypeReferences.Count() == 2)
            {
                foreach (var refLoc in allTypeReferences.Last().Locations)
                {
                    var treeRoot = refLoc.Location.SourceTree.GetRoot();
                    var node = treeRoot.FindNode(refLoc.Location.SourceSpan);

                    // checking in case I missed something and previous comment is not true
                    if (!node.Parent.IsKind(SyntaxKind.ObjectCreationExpression)
                        && !node.Parent.Parent.IsKind(SyntaxKind.ObjectCreationExpression))
                    {
                        throw new Exception("Reference is not an object creation expression");
                    }

                    AddInstantiationInfo(typeSymbol, refLoc, node, isCommand);
                }
            }
        }

        private void FindCommandToEventRelations()
        {
            foreach (var handlersKVP in _commandHandlers)
            {
                foreach (var handlerInfo in handlersKVP.Value)
                {
                    var instantiatedEvents = GetInstantiatedTypesWithinWholeCallTree(handlerInfo.MethodNode, handlerInfo.MethodSymbol, isCommandHandler: true);
                }
            }
            foreach (var handlersKVP in _eventHandlers)
            {
                foreach (var handlerInfo in handlersKVP.Value)
                {
                    var instantiatedCommands = GetInstantiatedTypesWithinWholeCallTree(handlerInfo.MethodNode, handlerInfo.MethodSymbol, isCommandHandler: false);
                }
            }
        }

        private List<ITypeSymbol> GetInstantiatedTypesWithinWholeCallTree(
            SyntaxNode methodNode,
            IMethodSymbol methodSymbol,
            bool isCommandHandler,
            HashSet<string> alreadyVisitedMethods = null,
            int depth = 0)
        {
            if (depth == 0)
            {
                alreadyVisitedMethods = new HashSet<string>();
            }
            alreadyVisitedMethods.Add(methodSymbol.ToString());

            if (_allInstantiatedTypesCache.TryGetValue(methodSymbol, out List<ITypeSymbol> instantiatedTypes))
            {
                return instantiatedTypes;
            }

            var methodsThatDirectlyInstantiateType = isCommandHandler
                ? _methodsThatDirectlyInstantiateEvents
                : _methodsThatDirectlyInstantiateCommands;
            if (methodsThatDirectlyInstantiateType.TryGetValue(methodSymbol, out instantiatedTypes) == false)
            {
                instantiatedTypes = new List<ITypeSymbol>();
            }

            var semanticModel = GetSemanticModel(methodNode.SyntaxTree);

            var calledMethodNodes = methodNode.DescendantNodes().Where(x => x.IsKind(SyntaxKind.InvocationExpression) && x.ToString().StartsWith("nameof(") == false);
            foreach (var calledMethodNode in calledMethodNodes)
            {
                var methodDeclarationSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(calledMethodNode).Symbol;
                if (methodDeclarationSymbol == null)
                {
                    temp.Add(calledMethodNode.ToString());
                    continue;
                }

                ISymbol[] implementations;
                if (methodDeclarationSymbol.ContainingType.TypeKind == TypeKind.Interface)
                {
                    implementations = SymbolFinder.FindImplementationsAsync(methodDeclarationSymbol, _solution).Result.ToArray();
                    //if (implementations.Length > 1)
                    //{
                    //    System.Diagnostics.Debugger.Break();
                    //}
                }
                else
                {
                    implementations = new[] { methodDeclarationSymbol };
                }


                foreach (var implementation in implementations)
                {
                    // detecting circular calls
                    // the code analyzed may not have such calls
                    // but this analyzer is not perfect and can stuck in a loop
                    if (alreadyVisitedMethods.Contains(implementation.ToString()))
                    {
                        continue;
                    }

                    // is method declared outside of analyzed solution?
                    if (implementation.DeclaringSyntaxReferences.Length == 0)
                    {
                        continue;
                    }

                    if (implementation.DeclaringSyntaxReferences.Length > 1)
                    {
                        throw new Exception("Unexpected number of declaring syntax references");
                    }

                    var syntaxRef = implementation.DeclaringSyntaxReferences.First();
                    var calledMethodDeclarationNode = syntaxRef.SyntaxTree.GetRoot().FindNode(syntaxRef.Span);

                    if (depth > 1000) System.Diagnostics.Debugger.Break();

                    var typesInstantiatedWithin = GetInstantiatedTypesWithinWholeCallTree(
                        calledMethodDeclarationNode,
                        (IMethodSymbol)implementation,
                        isCommandHandler,
                        alreadyVisitedMethods,
                        depth + 1);
                    instantiatedTypes.AddRange(typesInstantiatedWithin);
                }
            }

            if (depth <= MethodsCalledByHadlersCachingDepth)
            {
                _allInstantiatedTypesCache[methodSymbol] = instantiatedTypes;
            }

            return instantiatedTypes;
        }


        private SemanticModel GetSemanticModel(SyntaxTree tree)
        {
            if (!_semanticModelsCache.TryGetValue(tree.FilePath, out var semanticModel))
            {
                var document = _solution.GetDocument(tree);
                semanticModel = document.GetSemanticModelAsync().Result;
                _semanticModelsCache[tree.FilePath] = semanticModel;
            }

            return semanticModel;
        }

        private void AddInstantiationInfo(ITypeSymbol commandOrEventTypeSymbol, ReferenceLocation refLoc, SyntaxNode instantiationNode, bool isCommand)
        {
            var store = isCommand
                ? _commandInstantiations
                : _eventInstantiations;

            var instInfo = new InstantiationInfo(refLoc);

            if (store.TryGetValue(commandOrEventTypeSymbol, out var referenceLocations))
            {
                referenceLocations.Add(instInfo);
            }
            else
            {
                referenceLocations = new List<InstantiationInfo>() { instInfo };
                store[commandOrEventTypeSymbol] = referenceLocations;
            }

            var store2 = isCommand
                ? _methodsThatDirectlyInstantiateCommands
                : _methodsThatDirectlyInstantiateEvents;

            var semanticModel = GetSemanticModel(refLoc.Location.SourceTree);
            var methodDeclarationNode = GetMethodDeclarationThatContainsNode(instantiationNode);
            var methodDeclarationSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(methodDeclarationNode);

            if (store2.TryGetValue(methodDeclarationSymbol, out var typeSymbols))
            {
                typeSymbols.Add(commandOrEventTypeSymbol);
            }
            else
            {
                typeSymbols = new List<ITypeSymbol>() { commandOrEventTypeSymbol };
                store2[methodDeclarationSymbol] = typeSymbols;
            }
        }

        private bool IsHandlerType(ITypeSymbol typeSymbol)
        {
            return typeSymbol.AllInterfaces.Any(i => i.ToString().StartsWith(_cfg.HandlerMarkerInterfaceTypeNameWithNamespace));
        }

        private void AddHandler(ITypeSymbol handledTypeSymbol, IMethodSymbol methodDeclarationSymbol, SyntaxNode methodNode, bool isCommand)
        {
            var store = isCommand
                ? _commandHandlers
                : _eventHandlers;

            var handlerInfo = new HandlerInfo
            {
                MethodSymbol = methodDeclarationSymbol,
                MethodNode = methodNode
            };

            if (store.TryGetValue(handledTypeSymbol, out var handlerMethods))
            {
                handlerMethods.Add(handlerInfo);
            }
            else
            {
                handlerMethods = new List<HandlerInfo>() { handlerInfo };
                store[handledTypeSymbol] = handlerMethods;
            }
        }

        private SyntaxNode GetMethodDeclarationThatContainsNode(SyntaxNode node)
        {
            var ancestorNode = node.Parent;
            while (!ancestorNode.IsKind(SyntaxKind.MethodDeclaration))
            {
                ancestorNode = ancestorNode.Parent;
            }

            return ancestorNode;
        }
    }
}

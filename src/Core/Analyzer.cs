using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.EqualityComparers;
using Core.Graph;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace Core
{
    public class Analyzer
    {
        private Config _cfg;

        private const int MethodsCalledByHadlersCachingDepth = 2;

        private Solution _solution;
        private INamedTypeSymbol _commandInteraceTypeSymbol;
        private INamedTypeSymbol _eventInteraceTypeSymbol;

        private readonly SymbolEqualityComparer _symbolEqualityComparer = new SymbolEqualityComparer();

        private Dictionary<string, SemanticModel> _semanticModelsCache;

        private Dictionary<ITypeSymbol, List<InstantiationInfo>> _commandInstantiations;
        private Dictionary<ITypeSymbol, List<InstantiationInfo>> _eventInstantiations;

        private Dictionary<ITypeSymbol, List<HandlerInfo>> _commandHandlers;
        private Dictionary<ITypeSymbol, List<HandlerInfo>> _eventHandlers;

        private Dictionary<IMethodSymbol, List<ITypeSymbol>> _methodsThatDirectlyInstantiateCommands;
        private Dictionary<IMethodSymbol, List<ITypeSymbol>> _methodsThatDirectlyInstantiateEvents;

        private Dictionary<IMethodSymbol, List<ITypeSymbol>> _allInstantiatedTypesCache;

        private List<string> temp = new List<string>();


        public async Task StartAsync(Config cfg, IProgress<AnalysisProgress> progress = null)
        {
            progress.SafeReport(0, "Starting");

            _cfg = cfg;

            _semanticModelsCache = new Dictionary<string, SemanticModel>();
            _commandInstantiations = new Dictionary<ITypeSymbol, List<InstantiationInfo>>(_symbolEqualityComparer);
            _eventInstantiations = new Dictionary<ITypeSymbol, List<InstantiationInfo>>(_symbolEqualityComparer);
            _commandHandlers = new Dictionary<ITypeSymbol, List<HandlerInfo>>(_symbolEqualityComparer);
            _eventHandlers = new Dictionary<ITypeSymbol, List<HandlerInfo>>(_symbolEqualityComparer);
            _methodsThatDirectlyInstantiateCommands = new Dictionary<IMethodSymbol, List<ITypeSymbol>>(_symbolEqualityComparer);
            _methodsThatDirectlyInstantiateEvents = new Dictionary<IMethodSymbol, List<ITypeSymbol>>(_symbolEqualityComparer);
            _allInstantiatedTypesCache = new Dictionary<IMethodSymbol, List<ITypeSymbol>>(_symbolEqualityComparer);

            progress.SafeReport(10, "Getting solution compilation");

            var workspace = MSBuildWorkspace.Create();
            _solution = await workspace.OpenSolutionAsync(_cfg.SolutionPath);

            var project = _solution.Projects.FirstOrDefault(x => x.Name == _cfg.ProjectThatContainsCommandInterface);
            if (project == null)
            {
                return;
            }
            var compilation = await project.GetCompilationAsync();
            _commandInteraceTypeSymbol = compilation.GetTypeByMetadataName(_cfg.CommandInterfaceTypeNameWithNamespace);

            project = _solution.Projects.FirstOrDefault(x => x.Name == _cfg.ProjectThatContainsEventInterface);
            if (project == null)
            {
                return;
            }
            compilation = await project.GetCompilationAsync();
            _eventInteraceTypeSymbol = compilation.GetTypeByMetadataName(_cfg.EventInterfaceTypeNameWithNamespace);

            progress.SafeReport(40, "Searching for commands and events");

            Task.WaitAll(
                FindInstantiationsAndHandlersAsync(_commandInteraceTypeSymbol, true),
                FindInstantiationsAndHandlersAsync(_eventInteraceTypeSymbol, false)
            );

            //foreach (var item in _commandHandlers.Keys)
            //{
            //    if (_commandInstantiations.ContainsKey(item) == false)
            //    {
            //        temp.Add(item.Name);
            //    }
            //}

            progress.SafeReport(70, "Building relations between commands and events");

            await FindCommandToEventRelations();

            //File.WriteAllLines(@"C:/temp.cs", temp);
        }

        public CommandsEventsGraph GetCommandsEventsGraph()
        {
            var graph = new CommandsEventsGraph();

            if (_commandInstantiations.Any())
            {
                // building commands-first tree
                graph.Commands = new GraphNode[_commandHandlers.Keys.Count];
                for (int i = 0; i < graph.Commands.Length; i++)
                {
                    var cmdSmbl = _commandHandlers.Keys.ElementAt(i);
                    var graphNode = graph.Commands[i] = new GraphNode();
                    var path = new HashSet<string>();
                    BuildCommandsFirstTreeRecursively(graphNode, cmdSmbl, path);
                }

                // building events-first tree
                var gnec = new GraphNodeEqualityComparer();
                var eventsToCommandsLookup = new Dictionary<GraphNode, HashSet<GraphNode>>(_eventHandlers.Keys.Count, gnec);
                var commandsToEventsLookup = new Dictionary<GraphNode, HashSet<GraphNode>>(_commandHandlers.Keys.Count, gnec);
                void FindBackwardRelationsRecursively(GraphNode node)
                {
                    if (node.Children == null) return;

                    var lookup = node.Type == GraphNodeType.Command
                        ? eventsToCommandsLookup
                        : commandsToEventsLookup;
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        var child = node.Children[i];
                        if (lookup.TryGetValue(child, out var parents))
                        {
                            parents.Add(node);
                        }
                        else
                        {
                            lookup[child] = new HashSet<GraphNode>(gnec) { node };
                        }

                        FindBackwardRelationsRecursively(child);
                    }
                }
                for (int i = 0; i < graph.Commands.Length; i++)
                {
                    FindBackwardRelationsRecursively(graph.Commands[i]);
                }

                void BuildEventsFirstTreeRecursively(GraphNode eventNode, HashSet<string> eventsAlreadyAddedToTree, int d = 0)
                {
                    if (eventsAlreadyAddedToTree.Contains(eventNode.Text))
                    {
                        eventNode.IsRepeatedInTree = true;
                        return;
                    }
                    eventsAlreadyAddedToTree.Add(eventNode.Text);

                    if (!eventsToCommandsLookup.TryGetValue(eventNode, out var commands)) return;
                    eventNode.Children = commands.Select(x => x.MakeChildfreeCopy()).ToList();

                    for (int i = 0; i < eventNode.Children.Count; i++)
                    {
                        var cmd = eventNode.Children[i];
                        if (!commandsToEventsLookup.TryGetValue(cmd, out var events)) continue;
                        cmd.Children = events.Select(x => x.MakeChildfreeCopy()).ToList();

                        for (int j = 0; j < cmd.Children.Count; j++)
                        {
                            BuildEventsFirstTreeRecursively(cmd.Children[j], eventsAlreadyAddedToTree, d + 1);
                        }
                    }

                    eventsAlreadyAddedToTree.Remove(eventNode.Text);
                }
                graph.Events = new GraphNode[eventsToCommandsLookup.Keys.Count];
                for (int i = 0; i < graph.Events.Length; i++)
                {
                    graph.Events[i] = eventsToCommandsLookup.Keys.ElementAt(i).MakeChildfreeCopy();
                    var path = new HashSet<string>();
                    BuildEventsFirstTreeRecursively(graph.Events[i], path);
                }
            }

            return graph;
        }

        private void BuildCommandsFirstTreeRecursively(GraphNode commandNode, ITypeSymbol commandSymbol, HashSet<string> commandsAlreadyAddedToTree, int d = 0)
        {
            if (commandsAlreadyAddedToTree.Contains(commandSymbol.Name))
            {
                commandNode.IsRepeatedInTree = true;
                return;
            }
            commandsAlreadyAddedToTree.Add(commandSymbol.Name);

            commandNode.Text = commandSymbol.Name;
            commandNode.Type = GraphNodeType.Command;
            commandNode.TypeSymbol = commandSymbol;
            commandNode.Handlers = _commandHandlers[commandSymbol];

            if (_commandInstantiations.TryGetValue(commandSymbol, out var cInst))
            {
                commandNode.Instantiations = cInst;
            }

            foreach (var cmdHnldr in commandNode.Handlers)
            {
                var eventSymbols = _allInstantiatedTypesCache[cmdHnldr.MethodSymbol];
                foreach (var eventSymbol in eventSymbols)
                {
                    var eventNode = new GraphNode
                    {
                        Text = eventSymbol.Name,
                        Type = GraphNodeType.Event,
                        TypeSymbol = eventSymbol
                    };

                    if (_eventInstantiations.TryGetValue(eventSymbol, out var eInst))
                    {
                        eventNode.Instantiations = eInst;
                    }
                    commandNode.AddChild(eventNode);

                    if (_eventHandlers.TryGetValue(eventSymbol, out var eventHandlers))
                    {
                        eventNode.Handlers = eventHandlers;
                        foreach (var evtHndlr in eventHandlers)
                        {
                            var commandSymbols = _allInstantiatedTypesCache[evtHndlr.MethodSymbol];
                            foreach (var cmdSmbl in commandSymbols)
                            {
                                var cmdNode = new GraphNode
                                {
                                    Text = cmdSmbl.Name,
                                    Type = GraphNodeType.Command,
                                    TypeSymbol = cmdSmbl,
                                    Handlers = _commandHandlers[cmdSmbl],
                                    Instantiations = _commandInstantiations[cmdSmbl],
                                };
                                eventNode.AddChild(cmdNode);
                                BuildCommandsFirstTreeRecursively(cmdNode, cmdSmbl, commandsAlreadyAddedToTree, d + 1);
                            }
                        }
                    }
                }
            }

            commandsAlreadyAddedToTree.Remove(commandSymbol.Name);
        }

        private async Task FindInstantiationsAndHandlersAsync(ITypeSymbol typeSymbol, bool isCommand)
        {
            var allTypeReferences = await SymbolFinder.FindReferencesAsync(typeSymbol, _solution);
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
                        semanticModel = await GetSemanticModelAsync(refLoc.Location.SourceTree);
                        var inheritorTypeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(node.Parent.Parent);
                        if (inheritorTypeSymbol == null)
                        {
                            throw new Exception("GetDeclaredSymbol returned something unexpected");
                        }

                        await FindInstantiationsAndHandlersAsync(inheritorTypeSymbol, isCommand);
                        break;

                    case SyntaxKind.IdentifierName:
                        if (node.Parent.IsKind(SyntaxKind.ObjectCreationExpression)
                            || node.Parent.Parent.IsKind(SyntaxKind.ObjectCreationExpression))
                        {
                            await AddInstantiationInfoAsync(typeSymbol, refLoc, node, isCommand);
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
                                    semanticModel = await GetSemanticModelAsync(refLoc.Location.SourceTree);
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
                                    await AddInstantiationInfoAsync(typeSymbol, refLoc, node, isCommand);
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

                    await AddInstantiationInfoAsync(typeSymbol, refLoc, node, isCommand);
                }
            }
        }

        private async Task FindCommandToEventRelations()
        {
            //void ProcessInParallel(Dictionary<ITypeSymbol, List<HandlerInfo>> handlersLookup)
            //{
            //    var handlers = handlersLookup.SelectMany(x => x.Value).ToArray();
            //    var batchSize = (int)Math.Ceiling((float)handlers.Length / Environment.ProcessorCount);

            //    var topTasks = new Task[Environment.ProcessorCount];
            //    for (int i = 0; i < Environment.ProcessorCount; i++)
            //    {
            //        var thread = new Thread()
            //        topTasks[i] = Task.Factory.StartNew(() =>
            //        {
            //            var tasks = handlers.Skip(i * batchSize).Take(batchSize)
            //                .Select(y => GetInstantiatedTypesWithinCallTreeAsync(y.MethodNode, y.MethodSymbol, isCommandHandler: true))
            //                .ToArray();
            //            Task.WaitAll(tasks);
            //        });
            //    }
            //    Task.WaitAll(topTasks);
            //}

            //ProcessInParallel(_commandHandlers);
            //ProcessInParallel(_eventHandlers);



            //var cmdTasks = _commandHandlers
            //    .SelectMany(x => x.Value)
            //    .Select(y => GetInstantiatedTypesWithinCallTreeAsync(y.MethodNode, y.MethodSymbol, isCommandHandler: true));
            //var evtTasks = _eventHandlers
            //    .SelectMany(x => x.Value)
            //    .Select(y => GetInstantiatedTypesWithinCallTreeAsync(y.MethodNode, y.MethodSymbol, isCommandHandler: false));
            //Task.WaitAll(cmdTasks.Union(evtTasks).ToArray());



            foreach (var handlersKVP in _commandHandlers)
            {
                foreach (var handlerInfo in handlersKVP.Value)
                {
                    var instantiatedEvents = await GetInstantiatedTypesWithinCallTreeAsync(handlerInfo.MethodNode, handlerInfo.MethodSymbol, isCommandHandler: true);
                }
            }
            foreach (var handlersKVP in _eventHandlers)
            {
                foreach (var handlerInfo in handlersKVP.Value)
                {
                    var instantiatedCommands = await GetInstantiatedTypesWithinCallTreeAsync(handlerInfo.MethodNode, handlerInfo.MethodSymbol, isCommandHandler: false);
                }
            }
        }

        private async Task<List<ITypeSymbol>> GetInstantiatedTypesWithinCallTreeAsync(
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

            var semanticModel = await GetSemanticModelAsync(methodNode.SyntaxTree);

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
                    implementations = (await SymbolFinder.FindImplementationsAsync(methodDeclarationSymbol, _solution)).ToArray();
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
                    // but this analyzer is not perfect and can get stuck in a loop
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

                    var typesInstantiatedWithin = await GetInstantiatedTypesWithinCallTreeAsync(
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


        private async Task<SemanticModel> GetSemanticModelAsync(SyntaxTree tree)
        {
            if (!_semanticModelsCache.TryGetValue(tree.FilePath, out var semanticModel))
            {
                var document = _solution.GetDocument(tree);
                semanticModel = await document.GetSemanticModelAsync();
                _semanticModelsCache[tree.FilePath] = semanticModel;
            }

            return semanticModel;
        }

        private async Task AddInstantiationInfoAsync(ITypeSymbol commandOrEventTypeSymbol, ReferenceLocation refLoc, SyntaxNode instantiationNode, bool isCommand)
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

            var semanticModel = await GetSemanticModelAsync(refLoc.Location.SourceTree);
            var methodDeclarationNode = GetMethodDeclarationThatContainsNode(instantiationNode);

            // method declaration will be null here if event object is created outside of method, obviously
            if (methodDeclarationNode != null)
            {
                var methodDeclarationSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(methodDeclarationNode);
                instInfo.ContainingMethodSymbol = methodDeclarationSymbol;

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
            while (!ancestorNode.IsKind(SyntaxKind.MethodDeclaration) && ancestorNode != null)
            {
                ancestorNode = ancestorNode.Parent;
            }

            return ancestorNode;
        }
    }
}

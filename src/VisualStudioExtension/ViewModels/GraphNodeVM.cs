using Core;
using Core.Graph;
using System.Collections.Generic;
using System.Linq;
using VisualStudioExtension.Misc;

namespace VisualStudioExtension.ViewModels
{
    public class GraphNodeVM
    {
        public GraphNodeType Type { get; set; }
        public string Text { get; set; }
        public List<HandlerInfoVM> Handlers { get; set; }
        public List<InstantiationInfoVM> Instantiations { get; set; }
        public List<GraphNodeVM> Children { get; set; }


        public GraphNodeVM(GraphNode gn)
        {
            Type = gn.Type;
            Text = gn.Text;

            if (gn.Children != null)
            {
                Children = gn.Children.Select(x => new GraphNodeVM(x)).ToList();
            }

            if (gn.Handlers == null)
            {
                Handlers = new List<HandlerInfoVM> { new HandlerInfoVM { Text = "No handlers found" } };
            }
            else
            {
                Handlers = gn.Handlers.Select(x =>
                {
                    var startLinePosition = x.MethodNode.GetLocation().GetMappedLineSpan().StartLinePosition;
                    return new HandlerInfoVM
                    {
                        Text = x.MethodSymbol.ContainingType.Name,
                        CodeLocation = new CodeLocation
                        {
                            FilePath = x.MethodNode.SyntaxTree.FilePath,
                            Line = startLinePosition.Line,
                            Character = startLinePosition.Character
                        }
                    };
                }).OrderBy(x => x.Text).ToList();
            }

            if (gn.Instantiations == null)
            {
                Instantiations = new List<InstantiationInfoVM> { new InstantiationInfoVM { Text = "No instantiations found" } };
            }
            else
            {
                var instantiationsGroups = gn.Instantiations.Where(x => x.ContainingMethodSymbol != null).GroupBy(x => x.ContainingMethodSymbol.ContainingType.ToDisplayString());
                Instantiations = instantiationsGroups.Select(x =>
                {
                    var instantiation = x.First();
                    if (x.Count() == 1)
                    {
                        var containingType = instantiation.ContainingMethodSymbol.ContainingType.ToDisplayString();
                        var containingMethod = instantiation.ContainingMethodSymbol.Name;
                        var text = $"{containingType}->{containingMethod}()";

                        return GetInstantiationInfoVM(instantiation, text);
                    }
                    else
                    {
                        var inst = new InstantiationInfoVM
                        {
                            Text = instantiation.ContainingMethodSymbol.ContainingType.ToDisplayString(),
                            ProjectName = instantiation.ReferenceLocation.Document.Project.Name
                        };

                        inst.Methods = x.Select(i => GetInstantiationInfoVM(i, $"{i.ContainingMethodSymbol.Name}()")).ToList();

                        return inst;
                    }
                }).OrderBy(x => x.ProjectName).ToList();
            }
        }

        private InstantiationInfoVM GetInstantiationInfoVM(InstantiationInfo ii, string text)
        {
            var startLinePosition = ii.ReferenceLocation.Location.GetMappedLineSpan().StartLinePosition;
            return new InstantiationInfoVM
            {
                Text = text,
                ProjectName = ii.ReferenceLocation.Document.Project.Name,
                CodeLocation = new CodeLocation
                {
                    FilePath = ii.ReferenceLocation.Location.SourceTree.FilePath,
                    Line = startLinePosition.Line,
                    Character = startLinePosition.Character
                }
            };
        }
    }
}

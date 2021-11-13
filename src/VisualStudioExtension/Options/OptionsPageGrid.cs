using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioExtension
{
    public class OptionPageGrid : DialogPage
    {
        [Category("My Category")]
        [DisplayName("Project That Contains Command Interface")]
        public string ProjectThatContainsCommandInterface { get; set; }

        [Category("My Category")]
        [DisplayName("Project That Contains Event Interface")]
        public string ProjectThatContainsEventInterface { get; set; }

        [Category("My Category")]
        [DisplayName("Command Interface Type Name With Namespace")]
        public string CommandInterfaceTypeNameWithNamespace { get; set; }

        [Category("My Category")]
        [DisplayName("Event Interface Type Name With Namespace")]
        public string EventInterfaceTypeNameWithNamespace { get; set; }

        [Category("My Category")]
        [DisplayName("Handler Method Names")]
        [TypeConverter(typeof(StringArrayConverter))]
        public string[] HandlerMethodNames { get; set; }

        [Category("My Category")]
        [DisplayName("Handler Marker Interface Type Name With Namespace")]
        public string HandlerMarkerInterfaceTypeNameWithNamespace { get; set; }
    }
}

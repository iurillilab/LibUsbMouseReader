using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Bonsai.Extensions
{
    [Combinator]
    [Description("")]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class SourceScript
    {
        public IObservable<int> Process()
        {
            return Observable.Return(0);
        }
    }
}

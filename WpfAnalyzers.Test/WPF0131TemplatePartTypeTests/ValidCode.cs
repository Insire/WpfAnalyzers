namespace WpfAnalyzers.Test.WPF0131TemplatePartTypeTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GetTemplateChildAnalyzer();

        [Test]
        public void WhenCastingToSameType()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = ""PART_Bar"", Type = typeof(Border))]
    public class FooControl : Control
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var bar = (Border)this.GetTemplateChild(""PART_Bar"");
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenCastingToSameTypeFullyQualified()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    [TemplatePart(Name = ""PART_Bar"", Type = typeof(System.Windows.Controls.Border))]
    public class FooControl : System.Windows.Controls.Control
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var bar = (System.Windows.Controls.Border)this.GetTemplateChild(""PART_Bar"");
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenCastingToLessSpecificType()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = ""PART_Bar"", Type = typeof(Border))]
    public class FooControl : Control
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var bar = (FrameworkElement)this.GetTemplateChild(""PART_Bar"");
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenIsPatternSameType()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = PartBar, Type = typeof(Border))]
    public class FooControl : Control
    {
        private const string PartBar = ""PART_Bar"";

        private Border bar;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.bar = null;
            if (this.GetTemplateChild(PartBar) is Border border)
            {
                this.bar = border;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("as FrameworkElement")]
        [TestCase("as UIElement")]
        [TestCase("as Control")]
        public void AsCastStringLiteral(string cast)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = PartBar, Type = typeof(FrameworkElement))]
    public class FooControl : Control
    {
        private const string PartBar = ""PART_Bar"";

        private object bar;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.bar = this.GetTemplateChild(PartBar) as FrameworkElement;
        }
    }
}".AssertReplace("as FrameworkElement", cast);

            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}

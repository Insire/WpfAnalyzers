namespace WpfAnalyzers.Test.WPF0085XamlSetTypeConverterTargetTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new AttributeAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(WPF0085XamlSetTypeConverterTarget.Descriptor);

        [Test]
        public void Message()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetTypeConverter(↓nameof(ReceiveTypeConverter))]
    public class FooControl : Control
    {
        public static int ReceiveTypeConverter(object targetObject, XamlSetTypeConverterEventArgs eventArgs)
        {
            return int;
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Expected a method with signature void ReceiveTypeConverter(object, XamlSetTypeConverterEventArgs)."), testCode);
        }

        [Test]
        public void WhenReturningInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetTypeConverter(↓nameof(ReceiveTypeConverter))]
    public class FooControl : Control
    {
        public static int ReceiveTypeConverter(object targetObject, XamlSetTypeConverterEventArgs eventArgs)
        {
            return int;
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void WhenNoParameters()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetTypeConverter(↓nameof(ReceiveTypeConverter))]
    public class FooControl : Control
    {
        public static void ReceiveTypeConverter()
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void WhenMOreThanTwoParameters()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetTypeConverter(↓nameof(ReceiveTypeConverter))]
    public class FooControl : Control
    {
        public static void ReceiveTypeConverter(object targetObject, XamlSetTypeConverterEventArgs eventArgs, int i)
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void WhenFirstParameterIsInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetTypeConverter(↓nameof(ReceiveTypeConverter))]
    public class FooControl : Control
    {
        public static void ReceiveTypeConverter(int targetObject, XamlSetTypeConverterEventArgs eventArgs)
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void WhenSecondParameterIsInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetTypeConverter(↓nameof(ReceiveTypeConverter))]
    public class FooControl : Control
    {
        public static void ReceiveTypeConverter(object targetObject, int eventArgs)
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}

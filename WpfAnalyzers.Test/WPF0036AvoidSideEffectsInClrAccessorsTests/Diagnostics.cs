namespace WpfAnalyzers.Test.WPF0036AvoidSideEffectsInClrAccessorsTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClrPropertyDeclarationAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(WPF0036AvoidSideEffectsInClrAccessors.Descriptor);

        [Test]
        public void Message()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            nameof(Bar),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty OtherProperty = DependencyProperty.Register(
            ""Other"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get
            {
                ↓SideEffect(); 
                return (int)this.GetValue(BarProperty); 
            }
            set { this.SetValue(OtherProperty, value); }
        }

        private void SideEffect()
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Avoid side effects in CLR accessors."), testCode);
        }

        [Test]
        public void DependencyPropertySideEffectInGetter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            nameof(Bar),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty OtherProperty = DependencyProperty.Register(
            ""Other"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get
            {
                ↓SideEffect(); 
                return (int)this.GetValue(BarProperty); 
            }
            set { this.SetValue(OtherProperty, value); }
        }

        private void SideEffect()
        {
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void DependencyPropertySideEffectInSetter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            nameof(Bar),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty OtherProperty = DependencyProperty.Register(
            ""Other"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get
            {
                return (int)this.GetValue(BarProperty); 
            }
            set 
            {
                this.SetValue(OtherProperty, value);
                ↓SideEffect(); 
            }
        }

        private static void SideEffect()
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void ReadOnlyDependencyProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterReadOnly(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            protected set 
            { 
                this.SetValue(BarPropertyKey, value); 
                ↓SideEffect(); 
            }
        }

        private void SideEffect()
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}

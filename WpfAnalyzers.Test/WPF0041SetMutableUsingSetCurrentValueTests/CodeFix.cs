namespace WpfAnalyzers.Test.WPF0041SetMutableUsingSetCurrentValueTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new WPF0041SetMutableUsingSetCurrentValue();
        private static readonly CodeFixProvider Fix = new UseSetCurrentValueFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(WPF0041SetMutableUsingSetCurrentValue.DiagnosticId);

        [TestCase(true, "1")]
        [TestCase(false, "1")]
        [TestCase(true, "CreateValue()")]
        [TestCase(false, "CreateValue()")]
        public void ClrProperty(bool underscore, string value)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            ↓this.Bar = 1;
        }

        private int CreateValue() => 4;
    }
}".AssertReplace("1", value)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            this.SetCurrentValue(BarProperty, 1);
        }

        private int CreateValue() => 4;
    }
}".AssertReplace("1", value)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase(false, "1")]
        [TestCase(true, "1")]
        [TestCase(false, "CreateValue()")]
        [TestCase(true, "CreateValue()")]
        public void ClrPropertyWithTrivia(bool underscore, string value)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            // comment before
            ↓this.Bar = 1; // line comment
            // comment after
        }

        private int CreateValue() => 4;
    }
}".AssertReplace("1", value)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            // comment before
            this.SetCurrentValue(BarProperty, 1); // line comment
            // comment after
        }

        private int CreateValue() => 4;
    }
}".AssertReplace("1", value)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase("Bar = 1;", "SetCurrentValue(FooControl.BarProperty, (double)1);")]
        [TestCase("Bar = 1.0;", "SetCurrentValue(FooControl.BarProperty, 1.0);")]
        [TestCase("SetValue(FooControl.BarProperty, 1.0);", "SetCurrentValue(FooControl.BarProperty, 1.0);")]
        [TestCase("Bar = CreateValue();", "SetCurrentValue(FooControl.BarProperty, CreateValue());")]
        [TestCase("SetValue(FooControl.BarProperty, CreateValue());", "SetCurrentValue(FooControl.BarProperty, CreateValue());")]
        [TestCase("SetValue(FooControl.BarProperty, CreateObjectValue());", "SetCurrentValue(FooControl.BarProperty, CreateObjectValue());")]
        public void FromOutside(string before, string after)
        {
            var fooControlCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata(default(double)));

        public double Bar
        {
            get { return (double)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public static class Foo
    {
        private static readonly FooControl FooControl = new FooControl();

        public static void Meh()
        {
            ↓FooControl.Bar = 1;
        }

        private static double CreateValue() => 4;
        private static object CreateObjectValue() => 4;
    }
}".AssertReplace("FooControl.Bar = 1;", "FooControl." + before);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public static class Foo
    {
        private static readonly FooControl FooControl = new FooControl();

        public static void Meh()
        {
            FooControl.SetCurrentValue(FooControl.BarProperty, 1);
        }

        private static double CreateValue() => 4;
        private static object CreateObjectValue() => 4;
    }
}".AssertReplace("FooControl.SetCurrentValue(FooControl.BarProperty, 1);", "FooControl." + after);

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { testCode, fooControlCode }, fixedCode);
        }

        [TestCase("this.fooControl?↓.SetValue(FooControl.BarProperty, 1);")]
        public void DependencyPropertyFromOutsideConditional(string setExpression)
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly FooControl fooControl = new FooControl();

        public void Meh()
        {
            this.fooControl.↓Bar = 1;
        }
    }
}".AssertReplace("this.fooControl.↓Bar = 1;", setExpression);
            var fooControlCode = @"
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

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            this.SetCurrentValue(BarProperty, 1);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly FooControl fooControl = new FooControl();

        public void Meh()
        {
            this.fooControl?.SetCurrentValue(FooControl.BarProperty, 1);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooCode, fooControlCode }, fixedCode);
        }

        [Test]
        public void ClrPropertyObject()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            var value = GetValue(BarProperty);
            ↓SetValue(BarProperty, value);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            var value = GetValue(BarProperty);
            SetCurrentValue(BarProperty, value);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void ClrPropertyWhenFieldNameIsNotMatching()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        internal static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl));

        internal double Value
        {
            get { return (double)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Bar()
        {
            ↓this.Value = 1.0;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        internal static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl));

        internal double Value
        {
            get { return (double)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Bar()
        {
            this.SetCurrentValue(BarProperty, 1.0);
        }
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void InternalClrProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl));

        internal double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public void Bar()
        {
            ↓this.Value = 1.0;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl));

        internal double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public void Bar()
        {
            this.SetCurrentValue(ValueProperty, 1.0);
        }
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void ClrPropertySetInGenericClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl<T> : TextBox
    {
        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl<T>));

        internal double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public void Bar()
        {
            ↓this.Value = 1.0;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl<T> : TextBox
    {
        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl<T>));

        internal double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public void Bar()
        {
            this.SetCurrentValue(ValueProperty, 1.0);
        }
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void ClrPropertyWithGenericBaseClass()
        {
            var fooControlCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl<T> : TextBox
    {
        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl<T>));

        internal double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class BarControl : FooControl<int>
    {
        public void Bar()
        {
            ↓this.Value = 1.0;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class BarControl : FooControl<int>
    {
        public void Bar()
        {
            this.SetCurrentValue(ValueProperty, 1.0);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooControlCode, testCode }, fixedCode);
        }

        [Test]
        public void ClrPropertyOnGenericClass()
        {
            var fooControlCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl<T> : TextBox
    {
        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl<T>));

        internal double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class Foo
    {
        private readonly FooControl<int> fooControl = new FooControl<int>();

        public void Bar()
        {
            ↓fooControl.Value = 1.0;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class Foo
    {
        private readonly FooControl<int> fooControl = new FooControl<int>();

        public void Bar()
        {
            fooControl.SetCurrentValue(FooControl<int>.ValueProperty, 1.0);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { fooControlCode, testCode }, fixedCode);
        }

        [Test]
        public void ClrPropertyWithImplicitCastInt()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl));

        internal double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public void Bar()
        {
            ↓this.Value = 1;
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl));

        internal double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public void Bar()
        {
            this.SetCurrentValue(ValueProperty, (double)1);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void ClrPropertyInBaseclass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        public void Bar()
        {
            ↓this.Text = ""abc"";
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        public void Bar()
        {
            this.SetCurrentValue(TextProperty, ""abc"");
        }
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void SetValueInBaseclass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        public void Bar()
        {
            ↓this.SetValue(TextProperty, ""abc"");
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        public void Bar()
        {
            this.SetCurrentValue(TextProperty, ""abc"");
        }
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase(true, "1")]
        [TestCase(false, "1")]
        [TestCase(true, "CreateValue()")]
        [TestCase(false, "CreateValue()")]
        public void SetValue(bool underscore, string setExpression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            ↓this.SetValue(BarProperty, 1);
        }

        private int CreateValue() => 4;
    }
}".AssertReplace("1", setExpression)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            this.SetCurrentValue(BarProperty, 1);
        }

        private int CreateValue() => 4;
    }
}".AssertReplace("1", setExpression)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase(true, "1")]
        [TestCase(false, "1")]
        [TestCase(true, "this.CreateValue()")]
        [TestCase(false, "this.CreateValue()")]
        public void SetValueWithTrivia(bool underscore, string setExpression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            // comment before
            ↓this.SetValue(BarProperty, 1); // line comment
            // comment after
        }

        private int CreateValue() => 4;
    }
}".AssertReplace("1", setExpression)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        public void Meh()
        {
            // comment before
            this.SetCurrentValue(BarProperty, 1); // line comment
            // comment after
        }

        private int CreateValue() => 4;
    }
}".AssertReplace("1", setExpression)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void SetValueInCallback()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
            ""Value"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(
                default(int),
                OnValueChanged));

        public static readonly DependencyProperty SynchronizedProperty = DependencyProperty.RegisterAttached(""Synchronized"", typeof(int), typeof(Foo), new PropertyMetadata(default(int)));

        public static void SetValue(this DependencyObject element, int value)
        {
            element.SetValue(ValueProperty, value);
        }

        [AttachedPropertyBrowsableForChildren(IncludeDescendants = false)]
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetValue(this DependencyObject element)
        {
            return (int)element.GetValue(ValueProperty);
        }

        public static void SetSynchronized(this DependencyObject element, int value)
        {
            element.SetValue(SynchronizedProperty, value);
        }

        [AttachedPropertyBrowsableForChildren(IncludeDescendants = false)]
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetSynchronized(this DependencyObject element)
        {
            return (int)element.GetValue(SynchronizedProperty);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ↓d.SetValue(SynchronizedProperty, e.NewValue);
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
            ""Value"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(
                default(int),
                OnValueChanged));

        public static readonly DependencyProperty SynchronizedProperty = DependencyProperty.RegisterAttached(""Synchronized"", typeof(int), typeof(Foo), new PropertyMetadata(default(int)));

        public static void SetValue(this DependencyObject element, int value)
        {
            element.SetValue(ValueProperty, value);
        }

        [AttachedPropertyBrowsableForChildren(IncludeDescendants = false)]
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetValue(this DependencyObject element)
        {
            return (int)element.GetValue(ValueProperty);
        }

        public static void SetSynchronized(this DependencyObject element, int value)
        {
            element.SetValue(SynchronizedProperty, value);
        }

        [AttachedPropertyBrowsableForChildren(IncludeDescendants = false)]
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static int GetSynchronized(this DependencyObject element)
        {
            return (int)element.GetValue(SynchronizedProperty);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetCurrentValue(SynchronizedProperty, e.NewValue);
        }
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase(true, "\"1\"")]
        [TestCase(false, "\"1\"")]
        [TestCase(true, "CreateValue()")]
        [TestCase(false, "CreateValue()")]
        public void InheritedTextBoxTexUsingClrProperty(bool underscore, string value)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        public void Meh()
        {
            ↓this.Text = ""1"";
        }

        private string CreateValue() => ""2"";
    }
}".AssertReplace("\"1\"", value)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : TextBox
    {
        public void Meh()
        {
            this.SetCurrentValue(TextProperty, ""1"");
        }

        private string CreateValue() => ""2"";
    }
}".AssertReplace("\"1\"", value)
  .AssertReplace("this.", underscore ? string.Empty : "this.");

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [TestCase("this.")]
        [TestCase("")]
        public void TextBoxFieldTexUsingClrProperty(string thisExpression)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class Foo
    {
        private readonly TextBox textBox = new TextBox();

        public void Meh()
        {
            ↓this.textBox.Text = ""1"";
        }
    }
}".AssertReplace("this.", thisExpression);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class Foo
    {
        private readonly TextBox textBox = new TextBox();

        public void Meh()
        {
            this.textBox.SetCurrentValue(TextBox.TextProperty, ""1"");
        }
    }
}".AssertReplace("this.", thisExpression);

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void SetValueInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public FooControl()
        {
            this.Loaded += (sender, args) =>
            {
                ↓this.SetValue(BarProperty, 1);
            };
        }

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public FooControl()
        {
            this.Loaded += (sender, args) =>
            {
                this.SetCurrentValue(BarProperty, 1);
            };
        }

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }
    }
}

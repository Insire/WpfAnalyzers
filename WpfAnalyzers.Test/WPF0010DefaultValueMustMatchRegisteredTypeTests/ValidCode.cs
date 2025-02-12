namespace WpfAnalyzers.Test.WPF0010DefaultValueMustMatchRegisteredTypeTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new PropertyMetadataAnalyzer();

        [Test]
        public void DependencyPropertyRegisterNoMetadata()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl));

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DependencyPropertyRegisterWithMetadataWithCallbackOnly()
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
            new PropertyMetadata(OnBarChanged));

        public int Bar
        {
            get { return (int) this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        private static void OnBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // nop
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("int", "new PropertyMetadata()")]
        [TestCase("int", "new FrameworkPropertyMetadata()")]
        [TestCase("int", "new PropertyMetadata(default(int))")]
        [TestCase("int", "new PropertyMetadata(1, OnValueChanged)")]
        [TestCase("int", "new PropertyMetadata(1)")]
        [TestCase("int?", "new PropertyMetadata(1)")]
        [TestCase("int?", "new PropertyMetadata(null)")]
        [TestCase("bool?", "new PropertyMetadata(null)")]
        [TestCase("bool?", "new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)")]
        [TestCase("int?", "new PropertyMetadata(default(int?))")]
        [TestCase("Nullable<int>", "new PropertyMetadata(default(int?))")]
        [TestCase("int", "new PropertyMetadata(CreateDefaultValue())")]
        [TestCase("int", "new PropertyMetadata(CreateObjectValue())")]
        [TestCase("ObservableCollection<int>", "new PropertyMetadata(null)")]
        [TestCase("ObservableCollection<int>", "new PropertyMetadata(new ObservableCollection<int>())")]
        [TestCase("ObservableCollection<int>", "new PropertyMetadata(default(ObservableCollection<int>))")]
        [TestCase("IEnumerable", "new PropertyMetadata(new ObservableCollection<int>())")]
        public void DependencyPropertyRegisterWithMetadata(string typeName, string metadata)
        {
            var testCode = @"
#pragma warning disable WPF0016 // Default value is shared reference type.
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;

    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata(1));

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // nop
        }

        private static double CreateDefaultValue() => default(double);
        private static object CreateObjectValue() => default(double);
    }
}".AssertReplace("new PropertyMetadata(1)", metadata)
  .AssertReplace("double", typeName);

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DependencyPropertyRegisterWhenGenericContainingType()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl<T> : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(T),
            typeof(FooControl<T>),
            new PropertyMetadata(default(T)));

        public T Bar
        {
            get { return (T)GetValue(BarProperty); }
            set { SetValue(BarProperty, value); }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DependencyPropertyRegisterWhenBoxed()
        {
            var booleanBoxesCode = @"
namespace RoslynSandbox
{
    internal static class BooleanBoxes
    {
        internal static readonly object True = true;
        internal static readonly object False = false;

        internal static object Box(bool value)
        {
            return value
                        ? True
                        : False;
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(bool),
            typeof(FooControl),
            new PropertyMetadata(BooleanBoxes.Box(true)));

        public bool Value
        {
            get { return (bool)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, booleanBoxesCode, testCode);
        }

        [Test]
        public void DependencyPropertyRegisterReadOnly()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        private static readonly DependencyPropertyKey ValuePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Value),
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata(1.0));

        public static readonly DependencyProperty ValueProperty = ValuePropertyKey.DependencyProperty;

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValuePropertyKey, value); }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DependencyPropertyRegisterAttached()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(default(int)));

        public static void SetBar(this FrameworkElement element, int value) => element.SetValue(BarProperty, value);

        public static int GetBar(this FrameworkElement element) => (int)element.GetValue(BarProperty);
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DependencyPropertyRegisterAttachedWhenBoxed()
        {
            var booleanBoxesCode = @"
namespace RoslynSandbox
{
    internal static class BooleanBoxes
    {
        internal static readonly object True = true;
        internal static readonly object False = false;

        internal static object Box(bool value)
        {
            return value
                        ? True
                        : False;
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(bool),
            typeof(Foo),
            new PropertyMetadata(BooleanBoxes.Box(true)));

        public static void SetBar(FrameworkElement element, bool value)
        {
            element.SetValue(BarProperty, value);
        }

        public static bool GetBar(FrameworkElement element)
        {
            return (bool)element.GetValue(BarProperty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, booleanBoxesCode, testCode);
        }

        [Test]
        public void DependencyPropertyRegisterAttachedReadOnly()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(default(int)));

            public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;

        public static void SetBar(this FrameworkElement element, int value) => element.SetValue(BarPropertyKey, value);

        public static int GetBar(this FrameworkElement element) => (int)element.GetValue(BarProperty);
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DependencyPropertyAddOwner()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(int), 
            typeof(Foo), 
            new FrameworkPropertyMetadata(
                default(int), 
                FrameworkPropertyMetadataOptions.Inherits));

        public static void SetBar(DependencyObject element, int value)
        {
            element.SetValue(BarProperty, value);
        }

        public static int GetBar(DependencyObject element)
        {
            return (int) element.GetValue(BarProperty);
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = Foo.BarProperty.AddOwner(typeof(FooControl), new FrameworkPropertyMetadata(1));

        public int Bar
        {
            get { return (int) this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode, fooCode);
        }

        [Test]
        public void DependencyPropertyOverrideMetadata()
        {
            var fooControlCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            ""Value"",
            typeof(int),
            typeof(Control),
            new PropertyMetadata(default(int)));
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class BarControl : FooControl
    {
        static BarControl()
        {
            ValueProperty.OverrideMetadata(typeof(BarControl), new PropertyMetadata(1));
        }
    }
}";

            RoslynAssert.Valid(Analyzer, fooControlCode, testCode);
        }

        [Test]
        public void CastIntToDouble()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public sealed class FooControl : Control
    {
        private const int DefaultValue = 1;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata((double)DefaultValue));

        public double Value
        {
            get => (double)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void FontFamilyConverterConvertFromString()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Media;

    public class Foo
    {
        public static readonly DependencyProperty ButtonFontFamilyProperty = DependencyProperty.RegisterAttached(
            ""ButtonFontFamily"",
            typeof(FontFamily),
            typeof(Foo),
            new FrameworkPropertyMetadata(new FontFamilyConverter().ConvertFromString(""Marlett"")));

        /// <summary>
        /// Helper for setting ButtonFontFamily property on a DependencyObject.
        /// </summary>
        /// <param name=""element"">DependencyObject to set ButtonFontFamily property on.</param>
        /// <param name=""value"">ButtonFontFamily property value.</param>
        public static void SetButtonFontFamily(DependencyObject element, FontFamily value)
        {
            element.SetValue(ButtonFontFamilyProperty, value);
        }

        /// <summary>
        /// Helper for reading ButtonFontFamily property from a DependencyObject.
        /// </summary>
        /// <param name=""element"">DependencyObject to read ButtonFontFamily property from.</param>
        /// <returns>ButtonFontFamily property value.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static FontFamily GetButtonFontFamily(DependencyObject element)
        {
            return (FontFamily)element.GetValue(ButtonFontFamilyProperty);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void BoxedBool()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty IsTrueProperty = DependencyProperty.Register(
            nameof(IsTrue),
            typeof(bool), 
            typeof(FooControl), 
            new PropertyMetadata(BooleanBoxes.False));

        public bool IsTrue
        {
            get => (bool)GetValue(IsTrueProperty);
            set => SetValue(IsTrueProperty, BooleanBoxes.Box(value));
        }
    }

    internal static class BooleanBoxes
    {
        internal static readonly object True = true;
        internal static readonly object False = false;

        internal static object Box(bool value)
        {
            return value ? True : False;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void EnumIssue211()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        /// <summary>Identifies the <see cref=""FooEnum""/> dependency property.</summary>
        public static readonly DependencyProperty FooEnumProperty = DependencyProperty.Register(
            nameof(FooEnum),
            typeof(FooEnum),
            typeof(FooControl),
            new PropertyMetadata(FooEnum.Bar));

        public FooEnum FooEnum
        {
            get => (FooEnum) this.GetValue(FooEnumProperty);
            set => this.SetValue(FooEnumProperty, value);
        }
    }
}";
            var enumCode = @"
namespace RoslynSandbox
{
    public enum FooEnum
    {
        Bar,
        Baz
    }
}";
            RoslynAssert.Valid(Analyzer, testCode, enumCode);
            RoslynAssert.Valid(Analyzer, enumCode, testCode);
        }

        [Test]
        public void EnumAddOwnerIssue211()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty FooEnumProperty = DependencyProperty.RegisterAttached(
            ""FooEnum"",
            typeof(FooEnum),
            typeof(Foo),
            new FrameworkPropertyMetadata(FooEnum.Baz, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>Helper for setting <see cref=""FooEnumProperty""/> on <paramref name=""element""/>.</summary>
        /// <param name=""element""><see cref=""DependencyObject""/> to set <see cref=""FooEnumProperty""/> on.</param>
        /// <param name=""value"">FooEnum property value.</param>
        public static void SetFooEnum(DependencyObject element, FooEnum value)
        {
            element.SetValue(FooEnumProperty, value);
        }

        /// <summary>Helper for getting <see cref=""FooEnumProperty""/> from <paramref name=""element""/>.</summary>
        /// <param name=""element""><see cref=""DependencyObject""/> to read <see cref=""FooEnumProperty""/> from.</param>
        /// <returns>FooEnum property value.</returns>
        public static FooEnum GetFooEnum(DependencyObject element)
        {
            return (FooEnum)element.GetValue(FooEnumProperty);
        }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        /// <summary>Identifies the <see cref=""FooEnum""/> dependency property.</summary>
        public static readonly DependencyProperty FooEnumProperty = Foo.FooEnumProperty.AddOwner(
            typeof(FooControl),
            new FrameworkPropertyMetadata(
                FooEnum.Bar,
                FrameworkPropertyMetadataOptions.Inherits));

        public FooEnum FooEnum
        {
            get => (FooEnum) this.GetValue(FooEnumProperty);
            set => this.SetValue(FooEnumProperty, value);
        }
    }
}";
            var enumCode = @"namespace RoslynSandbox
{
    public enum FooEnum
    {
        Bar,
        Baz
    }
}";
            RoslynAssert.Valid(Analyzer, testCode, enumCode, fooCode);
            RoslynAssert.Valid(Analyzer, testCode, fooCode, enumCode);
            RoslynAssert.Valid(Analyzer, fooCode, testCode, enumCode);
            RoslynAssert.Valid(Analyzer, fooCode, enumCode, testCode);
            RoslynAssert.Valid(Analyzer, enumCode, fooCode, testCode);
            RoslynAssert.Valid(Analyzer, enumCode, testCode, fooCode);
        }
    }
}

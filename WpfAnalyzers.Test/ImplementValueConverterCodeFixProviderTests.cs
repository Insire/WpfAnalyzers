// ReSharper disable InconsistentNaming
namespace WpfAnalyzers.Test
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;

    public class ImplementValueConverterCodeFixProviderTests
    {
        private static readonly CodeFixProvider Fix = new ImplementValueConverterFix();
        //// ReSharper disable once InconsistentNaming
        private static readonly ExpectedDiagnostic CS0535 = ExpectedDiagnostic.Create(nameof(CS0535));

        [TestCase("FooConverter")]
        [TestCase("BarConverter")]
        public void IValueConverter(string className)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Data;

    public class FooConverter : IValueConverter
    {
    }
}".AssertReplace("FooConverter", className);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows.Data;

    public class FooConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        object IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException($""{nameof(FooConverter)} can only be used in OneWay bindings"");
        }
    }
}".AssertReplace("FooConverter", className);

            RoslynAssert.FixAll(Fix, CS0535, testCode, fixedCode);
        }

        [Test]
        public void IValueConverterConvertBack()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Data;

    public class FooConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows.Data;

    public class FooConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        object IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException($""{nameof(FooConverter)} can only be used in OneWay bindings"");
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS0535, testCode, fixedCode);
        }

        [Test]
        public void FullyQualifiedIValueConverter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class FooConverter : System.Windows.Data.IValueConverter
    {
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public class FooConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        object System.Windows.Data.IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException($""{nameof(FooConverter)} can only be used in OneWay bindings"");
        }
    }
}";
            RoslynAssert.FixAll(Fix, CS0535, testCode, fixedCode);
        }

        [TestCase("FooConverter")]
        [TestCase("BarConverter")]
        public void IMultiValueConverter(string className)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Data;

    public class FooConverter : IMultiValueConverter
    {
    }
}".AssertReplace("FooConverter", className);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows.Data;

    public class FooConverter : IMultiValueConverter
    {
        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        object[] IMultiValueConverter.ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException($""{nameof(FooConverter)} can only be used in OneWay bindings"");
        }
    }
}".AssertReplace("FooConverter", className);

            RoslynAssert.FixAll(Fix, CS0535, testCode, fixedCode);
        }

        [Test]
        public void IMultiValueConverterConvertBack()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows.Data;

    public class FooConverter : IMultiValueConverter
    {
        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Windows.Data;

    public class FooConverter : IMultiValueConverter
    {
        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        object[] IMultiValueConverter.ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException($""{nameof(FooConverter)} can only be used in OneWay bindings"");
        }
    }
}";

            RoslynAssert.CodeFix(Fix, CS0535, testCode, fixedCode);
        }

        [Test]
        public void FullyQualifiedIMultiValueConverter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class FooConverter : System.Windows.Data.IMultiValueConverter
    {
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public class FooConverter : System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        object[] System.Windows.Data.IMultiValueConverter.ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException($""{nameof(FooConverter)} can only be used in OneWay bindings"");
        }
    }
}";
            RoslynAssert.FixAll(Fix, CS0535, testCode, fixedCode);
        }
    }
}

namespace WpfAnalyzers.Test.WPF0062DocumentPropertyChangedCallbackTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new CallbackAnalyzer();

        [Test]
        public void DependencyPropertyRegister()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        /// <summary>Identifies the <see cref=""Value""/> dependency property.</summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata(
                default(double),
                (d, e) => ((FooControl)d).OnValueChanged((double)e.OldValue, (double)e.NewValue)));

        public double Value
        {
            get => (double)this.GetValue(ValueProperty);
            set => this.SetValue(ValueProperty, value);
        }

        /// <summary>This method is invoked when the <see cref=""ValueProperty""/> changes.</summary>
        /// <param name=""oldValue"">The old value of <see cref=""ValueProperty""/>.</param>
        /// <param name=""newValue"">The new value of <see cref=""ValueProperty""/>.</param>
        protected virtual void OnValueChanged(double oldValue, double newValue)
        {
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}

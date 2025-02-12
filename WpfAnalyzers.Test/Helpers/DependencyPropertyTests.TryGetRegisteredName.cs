namespace WpfAnalyzers.Test
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DependencyPropertyTests
    {
        public class TryGetRegisteredName
        {
            [TestCase("nameof(Bar)")]
            [TestCase("\"Bar\"")]
            public void DependencyPropertyBackingField(string nameCode)
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

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }
    }
}".AssertReplace("nameof(Bar)", nameCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, RoslynAssert.MetadataReferences);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var declaration = syntaxTree.FindFieldDeclaration("BarProperty");
                var symbol = semanticModel.GetDeclaredSymbolSafe(declaration, CancellationToken.None);
                Assert.AreEqual(true, BackingFieldOrProperty.TryCreateForDependencyProperty(symbol, out var fieldOrProperty));
                Assert.AreEqual(true, DependencyProperty.TryGetRegisteredName(fieldOrProperty, semanticModel, CancellationToken.None, out var name));
                Assert.AreEqual("Bar", name);
            }

            [TestCase("nameof(Bar)")]
            [TestCase("\"Bar\"")]
            public void DependencyPropertyBackingProperty(string nameCode)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static DependencyProperty BarProperty { get; } = DependencyProperty.Register(
            nameof(Bar),
            typeof(int),
            typeof(FooControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }
    }
}".AssertReplace("nameof(Bar)", nameCode);
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, RoslynAssert.MetadataReferences);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var declaration = syntaxTree.FindPropertyDeclaration("BarProperty");
                var symbol = semanticModel.GetDeclaredSymbol(declaration, CancellationToken.None);
                Assert.AreEqual(true, BackingFieldOrProperty.TryCreateForDependencyProperty(symbol, out var fieldOrProperty));
                Assert.AreEqual(true, DependencyProperty.TryGetRegisteredName(fieldOrProperty, semanticModel, CancellationToken.None, out var name));
                Assert.AreEqual("Bar", name);
            }

            [Test]
            public void TextElementFontSizePropertyAddOwner()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Documents;

    public class FooControl : FrameworkElement
    {
        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(FooControl));

        public double FontSize
        {
            get => (double)this.GetValue(FontSizeProperty);
            set => this.SetValue(FontSizeProperty, value);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, RoslynAssert.MetadataReferences);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var declaration = syntaxTree.FindFieldDeclaration("FontSizeProperty");
                var symbol = semanticModel.GetDeclaredSymbolSafe(declaration, CancellationToken.None);
                Assert.AreEqual(true, BackingFieldOrProperty.TryCreateForDependencyProperty(symbol, out var fieldOrProperty));
                Assert.AreEqual(true, DependencyProperty.TryGetRegisteredName(fieldOrProperty, semanticModel, CancellationToken.None, out var name));
                Assert.AreEqual("FontSize", name);
            }

            [Test]
            public void BorderBorderThicknessPropertyAddOwner()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : FrameworkElement
    {
        public static readonly DependencyProperty BorderThicknessProperty = Border.BorderThicknessProperty.AddOwner(typeof(FooControl));

        public Size BorderThickness
        {
            get => (Size)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, RoslynAssert.MetadataReferences);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var declaration = syntaxTree.FindFieldDeclaration("BorderThicknessProperty");
                var symbol = semanticModel.GetDeclaredSymbolSafe(declaration, CancellationToken.None);
                Assert.AreEqual(true, BackingFieldOrProperty.TryCreateForDependencyProperty(symbol, out var fieldOrProperty));
                Assert.AreEqual(true, DependencyProperty.TryGetRegisteredName(fieldOrProperty, semanticModel, CancellationToken.None, out var name));
                Assert.AreEqual("BorderThickness", name);
            }
        }
    }
}

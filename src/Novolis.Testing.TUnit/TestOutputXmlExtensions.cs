using TUnit.Core;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Novolis.Testing.TUnit;

/// <summary>XML serialization helpers for TUnit test output.</summary>
public static class TestOutputXmlExtensions
{
    /// <summary>Serializes <paramref name="source"/> to XML and writes it to test output.</summary>
    /// <typeparam name="T">Type to serialize.</typeparam>
    /// <param name="outputHelper">TUnit test context.</param>
    /// <param name="source">Object to serialize.</param>
    /// <param name="xmlWriterSettings">Optional XML writer settings.</param>
    public static void WriteXml<T>(this TestContext? outputHelper, T source, XmlWriterSettings? xmlWriterSettings = null)
    {
        var settings = xmlWriterSettings ?? XmlWriterSettings;
        
        using var textWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(textWriter, settings);
        var xmlSerializer = new XmlSerializerFactory().CreateSerializer(typeof(T));
        xmlSerializer.Serialize(xmlWriter, source, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
        outputHelper?.OutputWriter.WriteLine(textWriter.ToString());
    }

    private static XmlWriterSettings XmlWriterSettings => new()
    {
        Indent = true,
        IndentChars = new string(' ', 4),
        NewLineChars = "\n",
        NewLineHandling = NewLineHandling.Replace,
        OmitXmlDeclaration = false,
        Encoding = new UTF8Encoding(false)
    };
}
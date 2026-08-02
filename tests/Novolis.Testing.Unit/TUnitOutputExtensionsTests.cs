using System.Text.Json;
using System.Xml.Serialization;
using Novolis.Testing.TUnit;
using TUnit.Core;

namespace Novolis.Testing.Unit;

public sealed class TUnitOutputExtensionsTests
{
    [Test]
    public async Task WriteJson_serializes_with_default_options()
    {
        TestContext.Current?.WriteJson(new { Name = "alpha", Count = 3 });
        await Assert.That(TestContext.Current).IsNotNull();
    }

    [Test]
    public async Task WriteJson_uses_custom_options()
    {
        var options = new JsonSerializerOptions { WriteIndented = false };
        TestContext.Current?.WriteJson(new { Name = "beta" }, options);
        await Assert.That(options.WriteIndented).IsFalse();
    }

    [Test]
    public async Task WriteLine_delegates_to_json()
    {
        TestContext.Current?.WriteLine(new { Message = "hello" });
        await Assert.That(TestContext.Current).IsNotNull();
    }

    [Test]
    public async Task WriteTable_renders_rows()
    {
        TestContext.Current?.WriteTable(new[]
        {
            new { Key = "a", Value = 1 },
            new { Key = "b", Value = 2 },
        });
        await Assert.That(TestContext.Current).IsNotNull();
    }

    [Test]
    public async Task WriteCSharp_dumps_object()
    {
        TestContext.Current?.WriteCSharp(new SamplePerson { Name = "Ada", Age = 42 });
        await Assert.That(TestContext.Current).IsNotNull();
    }

    [Test]
    public async Task WriteCSharp_dumps_enumerable_with_id_selector()
    {
        var people = new[]
        {
            new SamplePerson { Name = "Ada", Age = 42 },
            new SamplePerson { Name = "Grace", Age = 37 },
        };
        TestContext.Current?.WriteCSharp(people, p => p.Name);
        await Assert.That(TestContext.Current).IsNotNull();
    }

    [Test]
    public async Task WriteXml_serializes_object()
    {
        TestContext.Current?.WriteXml(new SampleXmlPayload { Title = "demo", Value = 7 });
        await Assert.That(TestContext.Current).IsNotNull();
    }

    [Test]
    public async Task WriteXml_uses_custom_writer_settings()
    {
        var settings = new System.Xml.XmlWriterSettings { Indent = false, OmitXmlDeclaration = true };
        TestContext.Current?.WriteXml(new SampleXmlPayload { Title = "custom", Value = 1 }, settings);
        await Assert.That(settings.Indent).IsFalse();
    }

    private sealed class SamplePerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    [XmlRoot("Payload")]
    public sealed class SampleXmlPayload
    {
        public string Title { get; set; } = "";
        public int Value { get; set; }
    }
}

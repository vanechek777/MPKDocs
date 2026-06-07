using System.Xml.Linq;

namespace MPKDocumentsMAUI.Shared.Services;

public static class CsprojVersionWriter
{
    public static (string Version, int Build)? TryRead(string csprojPath)
    {
        if (!File.Exists(csprojPath))
            return null;

        var doc = XDocument.Load(csprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var version = ReadProperty(doc, ns, "ApplicationDisplayVersion");
        var buildText = ReadProperty(doc, ns, "ApplicationVersion");
        if (string.IsNullOrWhiteSpace(version) || !int.TryParse(buildText, out var build) || build < 1)
            return null;

        return (version.Trim(), build);
    }

    public static void Apply(string csprojPath, string version, int build)
    {
        if (build < 1)
            throw new ArgumentOutOfRangeException(nameof(build), "build должен быть >= 1.");

        version = version.Trim();
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Укажите version.", nameof(version));

        var doc = XDocument.Load(csprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        SetProperty(doc, ns, "ApplicationDisplayVersion", version);
        SetProperty(doc, ns, "ApplicationVersion", build.ToString());
        SetProperty(doc, ns, "Version", version);
        SetProperty(doc, ns, "AssemblyVersion", $"{version}.0");
        SetProperty(doc, ns, "FileVersion", $"{version}.0");
        SetProperty(doc, ns, "InformationalVersion", version);
        doc.Save(csprojPath);
    }

    private static string? ReadProperty(XDocument doc, XNamespace ns, string name) =>
        doc.Descendants(ns + name).Select(x => x.Value?.Trim()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static void SetProperty(XDocument doc, XNamespace ns, string name, string value)
    {
        var node = doc.Descendants(ns + name).FirstOrDefault();
        if (node is not null)
        {
            node.Value = value;
            return;
        }

        var group = doc.Descendants(ns + "PropertyGroup").FirstOrDefault();
        if (group is null)
        {
            group = new XElement(ns + "PropertyGroup");
            doc.Root?.Add(group);
        }

        group.Add(new XElement(ns + name, value));
    }
}

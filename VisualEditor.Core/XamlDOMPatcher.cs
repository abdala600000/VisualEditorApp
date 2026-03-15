using System;
using System.IO;
using System.Linq;
using System.Xml;
using Avalonia.Controls;

namespace VisualEditor.Core;

/// <summary>
/// A smart parser that modifies an existing XAML document
/// without rebuilding it from scratch, preserving all events, bindings, and structure.
/// </summary>
public static class XamlDOMPatcher
{
    public static string PatchProperty(string originalXaml, Control targetControl, string propertyName, string newValue)
    {
        if (targetControl == null || string.IsNullOrWhiteSpace(originalXaml))
            return originalXaml;

        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = true; // Preserve all formatting and spaces

        try
        {
            doc.LoadXml(originalXaml);

            // Find the element by name or infer it by its unique signature/path if the name is not set
            // For now, if the control has a Name, we search for that specifically.
            XmlElement targetElement = null;

            if (!string.IsNullOrEmpty(targetControl.Name))
            {
                targetElement = FindElementByName(doc.DocumentElement, targetControl.Name);
            }

            if (targetElement != null)
            {
                UpdatePropertyAttribute(targetElement, propertyName, newValue);
                return RenderXmlDocument(doc);
            }
        }
        catch (XmlException)
        {
            // If the XAML is currently invalid due to typing, skip patching
            return originalXaml;
        }

        return originalXaml; // Return unpatched if target not found
    }
    
    public static string ApplyDesignChanges(string originalXaml, Control rootControl)
    {
        // For moving controls with the designer tool, we'll implement a deeper patcher later
        // that crawls through rootControl and aligns properties to the DOC
        return originalXaml;
    }

    private static XmlElement FindElementByName(XmlElement root, string name)
    {
        if (root.GetAttribute("Name") == name || root.GetAttribute("x:Name") == name)
            return root;

        foreach (XmlNode node in root.ChildNodes)
        {
            if (node is XmlElement childEl)
            {
                var found = FindElementByName(childEl, name);
                if (found != null) return found;
            }
        }
        return null;
    }

    private static void UpdatePropertyAttribute(XmlElement element, string propertyName, string newValue)
    {
        // Basic mapping for Margin (some designers prefer discrete margins, we'll keep it simple: Margin string)
        if (newValue == null)
        {
            element.RemoveAttribute(propertyName);
        }
        else
        {
            element.SetAttribute(propertyName, newValue);
        }
    }

    private static string RenderXmlDocument(XmlDocument doc)
    {
        using var stringWriter = new StringWriter();
        using var xmlTextWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = false, // Keep existing whitespaces explicitly
            NamespaceHandling = NamespaceHandling.OmitDuplicates
        });

        doc.WriteContentTo(xmlTextWriter);
        xmlTextWriter.Flush();
        return stringWriter.ToString();
    }
}

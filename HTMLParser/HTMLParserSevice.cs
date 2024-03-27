using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace HTMLParser;

/// <summary>
/// Represents an HTML element with attributes and child elements.
/// </summary>
public class HtmlElement
{
    // Properties to hold the element's tag name, attributes, children, and parent
    public string TagName { get; }
    public Dictionary<string, string> Attributes { get; }
    public List<HtmlElement> Children { get; }
    public HtmlElement Parent { get; }
    public string InnerText
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var child in Children)
            {
                if (child.TagName.Equals("text", StringComparison.CurrentCultureIgnoreCase))
                {
                    sb.Append(child.Attributes["content"]);
                }
                else
                {
                    sb.Append(child.InnerText);
                }
            }
            return sb.ToString();
        }
        set // Assumption: the first child is the text node
        {
            if (Children.Count > 0 && Children[0].TagName.Equals("text", StringComparison.OrdinalIgnoreCase))
            {
                Children[0].Attributes["content"] = value;
            }
            else // No text node exists, add one
            {
                var textNode = new HtmlElement("text", new Dictionary<string, string> { { "content", value } }, this);
                Children.Insert(0, textNode);
            }
        }
    }
    //pip
    /// <summary>
    /// Initializes a new instance of HtmlElement class.
    /// </summary>
    /// <param name="tagName">The tag name of the HTML element.</param>
    /// <param name="attributes">The attributes of the HTML element.</param>
    /// <param name="parent">The parent HTML element.</param>
    public HtmlElement(string tagName, Dictionary<string, string> attributes, HtmlElement parent)
    {
        TagName = tagName;
        Attributes = attributes ?? new Dictionary<string, string>();
        Children = new List<HtmlElement>();
        Parent = parent;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"<{TagName}");
        foreach (var attribute in Attributes)
        {
            sb.Append($" {attribute.Key}=\"{attribute.Value}\"");
        }
        sb.Append('>');
        return sb.ToString();
    }

    public JsonElement ToJson()
    {
        var serializer = new JsonHtmlSerializer();
        return serializer.Serialize(this);
    }

    /// <summary>
    /// Finds all elements in the tree with the specified tag name.
    /// </summary>
    /// <param name="tagName">The tag name to search for.</param>
    /// <returns>An enumerable collection of HtmlElement objects.</returns>
    public IEnumerable<HtmlElement> FindElementsByTag(string tagName)
    {
        var foundElements = new List<HtmlElement>();
        FindElementsByTag(tagName, this, foundElements);
        return foundElements;
    }

    /// <summary>
    /// Recursive helper method to find elements by tag name.
    /// </summary>
    /// <param name="tagName">The tag name to search for.</param>
    /// <param name="element">The current element to check.</param>
    /// <param name="foundElements">The list to add found elements to.</param>
    private void FindElementsByTag(string tagName, HtmlElement element, List<HtmlElement> foundElements)
    {
        if (element.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
        {
            foundElements.Add(element);
        }

        foreach (HtmlElement child in element.Children)
        {
            FindElementsByTag(tagName, child, foundElements);
        }
    }

    /// <summary>
    /// Finding Elements By Attribute. Method to find elements that match a given attribute name and value.
    /// </summary>
    /// <param name="attributeName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public IEnumerable<HtmlElement> FindElementsByAttribute(string attributeName, string value = null)
    {
        var foundElements = new List<HtmlElement>();
        FindElementsByAttribute(attributeName, value, this, foundElements);
        return foundElements;
    }

    private void FindElementsByAttribute(string attributeName, string value, HtmlElement element, List<HtmlElement> foundElements)
    {
        if (element.Attributes.TryGetValue(attributeName, out string foundValue) && (value == null || foundValue == value))
        {
            foundElements.Add(element);
        }

        foreach (HtmlElement child in element.Children)
        {
            FindElementsByAttribute(attributeName, value, child, foundElements);
        }
    }

    #region GETS METHODS
    public string GetInnerText()
    {
        var stringBuilder = new StringBuilder();
        GetInnerText(this, stringBuilder);
        return stringBuilder.ToString();
    }

    private void GetInnerText(HtmlElement element, StringBuilder stringBuilder)
    {
        foreach (HtmlElement child in element.Children)
        {
            if (child.TagName.Equals("textnode", StringComparison.OrdinalIgnoreCase))
            {
                stringBuilder.Append(child.Attributes["value"]);
            }
            else
            {
                GetInnerText(child, stringBuilder);
            }
        }
    }

    public IEnumerable<HtmlElement> QuerySelector(string cssSelector)
    {
        // Обработка простейшего селектора (например, класса или ID)
        if (cssSelector.StartsWith("."))
        {
            string className = cssSelector.Substring(1);
            return GetElementsByClassName(className);
        }
        if (cssSelector.StartsWith("#"))
        {
            string id = cssSelector.Substring(1);
            return GetElementsById(id);
        }

        return null;
    }

    private IEnumerable<HtmlElement> GetElementsByClassName(string className)
    {
        var elementsWithClass = new List<HtmlElement>();
        GetElementsByCondition(this, el =>
            el.Attributes.TryGetValue("class", out string classAttr) && classAttr.Split(' ').Contains(className),
            elementsWithClass);
        return elementsWithClass;
    }

    private IEnumerable<HtmlElement> GetElementsById(string id)
    {
        var elementsWithId = new List<HtmlElement>();
        GetElementsByCondition(this, el =>
            el.Attributes.TryGetValue("id", out string idAttr) && idAttr == id,
            elementsWithId);
        return elementsWithId;
    }

    private void GetElementsByCondition(HtmlElement element, Func<HtmlElement, bool> condition, List<HtmlElement> foundElements)
    {
        if (condition(element))
        {
            foundElements.Add(element);
        }
        foreach (HtmlElement child in element.Children)
        {
            GetElementsByCondition(child, condition, foundElements);
        }
    }
    #endregion

    public HtmlElement AddChild(string tagName, Dictionary<string, string> attributes = null)
    {
        var child = new HtmlElement(tagName, attributes, this);
        this.Children.Add(child);
        return child;
    }

    public void RemoveChild(HtmlElement element)
    {
        this.Children.Remove(element);
    }

    public Dictionary<string, HtmlElement> BuildIdIndex()
    {
        var index = new Dictionary<string, HtmlElement>();
        BuildIdIndex(this, index);
        return index;
    }

    private void BuildIdIndex(HtmlElement element, Dictionary<string, HtmlElement> index)
    {
        if (element.Attributes.TryGetValue("id", out string id))
        {
            index[id] = element;
        }

        foreach (HtmlElement child in element.Children)
        {
            BuildIdIndex(child, index);
        }
    }

    public void PrettyPrint(int indentLevel = 0)
    {
        string indent = new string(' ', indentLevel * 4);
        string innerIndent = new string(' ', (indentLevel + 1) * 4);

        Console.WriteLine($"{indent}<{TagName}{HTMLParserViewer.FormatAttributes(Attributes)}>");

        foreach (var child in Children)
        {
            if (child.Children.Count == 0)
            {
                Console.WriteLine($"{innerIndent}{child.TagName}{HTMLParserViewer.FormatAttributes(child.Attributes)}");
            }
            else
            {
                child.PrettyPrint(indentLevel + 1);
            }
        }

        Console.WriteLine($"{indent}</{TagName}>");
    }
}

public class JsonHtmlSerializer
{
    // Сериализует HTML-элемент в JSON
    public JsonElement Serialize(HtmlElement element)
    {
        using var jsonDoc = JsonDocument.Parse(SerializeToJsonString(element));
        return jsonDoc.RootElement.Clone();
    }

    private string SerializeToJsonString(HtmlElement element)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(ToDictionary(element), options);
    }

    // Конвертирует HtmlElement в словарь для последующей сериализации в JSON
    private Dictionary<string, object> ToDictionary(HtmlElement element)
    {
        var dict = new Dictionary<string, object>
        {
            ["TagName"] = element.TagName,
            ["Attributes"] = element.Attributes
        };

        if (element.Children.Any())
        {
            dict["Children"] = element.Children.Select(ToDictionary).ToList();
        }

        return dict;
    }
}

/// <summary>
/// Provides services for parsing HTML content and building an element tree.
/// </summary>
public class HTMLParserService
{
    // Regular expressions for matching tags and attributes
    private static readonly Regex TagRegex = new Regex("<(\\/)?([a-z1-6]+)([^<>]*)>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AttributeRegex = new Regex("(\\w+)(=\"([^\"]*)\")?", RegexOptions.Compiled);

    /// <summary>
    /// Parses the provided HTML content into an element tree.
    /// </summary>
    /// <param name="html">The HTML content to parse.</param>
    /// <returns>A HtmlElement object representing the root of the tree.</returns>
    public HtmlElement Parse(string html)
    {
        var root = new HtmlElement("document", null, null);
        var current = root;

        foreach (Match tagMatch in TagRegex.Matches(html))
        {
            string tagName = tagMatch.Groups[2].Value.ToLower();
            bool isClosingTag = tagMatch.Groups[1].Value == "/";

            var attributes = new Dictionary<string, string>();
            foreach (Match attrMatch in AttributeRegex.Matches(tagMatch.Groups[3].Value))
            {
                string attrName = attrMatch.Groups[1].Value;
                string attrValue = attrMatch.Groups[3].Value;
                attributes[attrName] = attrValue;
            }

            if (!isClosingTag)
            {
                var child = new HtmlElement(tagName, attributes, current);
                current.Children.Add(child);
                current = child;
            }
            else
            {
                if (current != null && tagName.Equals(current.TagName, StringComparison.OrdinalIgnoreCase))
                {
                    current = current.Parent;
                }
            }
        }

        return root;
    }

    /// <summary>
    /// Asynchronously parses HTML content from the specified URL into an element tree.
    /// </summary>
    /// <param name="url">The URL to download and parse HTML content from.</param>
    /// <returns>A task that represents the asynchronous operation of parsing HTML content.</returns>
    public async Task<HtmlElement> ParseFromAsync(string url)
    {
        using HttpClient client = new HttpClient();

        byte[] htmlBytes = await client.GetByteArrayAsync(url);
        string htmlContent = Encoding.UTF8.GetString(htmlBytes);

        return Parse(htmlContent);
    }
}

/// <summary>
/// Provides helper methods for HTML element viewing and debugging.
/// </summary>
public static class HTMLParserViewer
{
    /// <summary>
    /// Recursively traverses the HTML element tree and prints a textual representation.
    /// </summary>
    /// <param name="element">The element to traverse.</param>
    /// <param name="depth">The current depth in the tree, used for indentation.</param>
    public static void Traverse(this HtmlElement element, int depth)
    {
        Console.WriteLine("{0}{1} {2}", new string(' ', depth * 4), element.TagName, element.Attributes.Count > 0 ? $"({string.Join(", ", element.Attributes)})" : "");
        foreach (var child in element.Children)
        {
            Traverse(child, depth + 1);
        }
    }

    private static void Traverse(this HtmlElement element, int depth, StreamWriter writer)
    {
        writer.WriteLine("{0}{1} {2}", new string(' ', depth * 4), element.TagName, element.Attributes.Count > 0 ? $"({string.Join(", ", element.Attributes)})" : "");
        foreach (var child in element.Children)
        {
            Traverse(child, depth + 1, writer);
        }
    }

    public static void TraverseAndSaveToFile(this HtmlElement element, string filePath)
    {
        using StreamWriter writer = new(filePath);
        element.Traverse(0, writer);
    }

    public static async Task TraverseAsync(this HtmlElement element, int depth, StreamWriter writer)
    {
        await writer.WriteLineAsync($"{new string(' ', depth * 4)}{element.TagName} {(element.Attributes.Count > 0 ? $"({string.Join(", ", element.Attributes)})" : "")}");
        foreach (var child in element.Children)
        {
            await TraverseAsync(child, depth + 1, writer);
        }
    }

    public static async Task TraverseAndSaveToFileAsync(this HtmlElement element, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            await element.TraverseAsync(0, writer);
        }

        Console.WriteLine($"the file is saved in this path:{filePath}");
    }

    public static string FormatAttributes(Dictionary<string, string> attributes)
    {
        return attributes.Count > 0
            ? " " + string.Join(" ", attributes.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\""))
            : string.Empty;
    }
}
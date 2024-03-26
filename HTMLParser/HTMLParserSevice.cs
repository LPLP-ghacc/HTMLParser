using System.Text;
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
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            element.Traverse(0, writer);
        }
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
}
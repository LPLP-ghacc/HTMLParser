# HTMLParser Library

## Overview

The `HTMLParser` library is a lightweight C# solution designed to parse HTML content and construct an in-memory representation of the HTML element tree. It offers a straightforward interface for traversing, inspecting, and interacting with the hierarchical structure of an HTML document.

## Key Features

- **HtmlElement Class**: Central to the library, this class encapsulates the necessary properties of HTML elements, including tag name, attributes, child elements, and a reference to the parent element.

- **HTMLParserService Class**: Provides the core functionality to parse raw HTML strings. It utilizes regular expressions to identify HTML tags and attributes and assembles a structured tree of `HtmlElement` objects that mirrors the layout of the HTML document.

- **HTMLParserViewer Utility**: Includes a `Traverse` extension method for the `HtmlElement` class, enabling recursive traversal and a console output of the element tree, which is useful for debugging or examining the parsed HTML structure.

- **Element Search Functionality**: The `FindElementsByTag` method in `HtmlElement` allows users to retrieve all elements of a specific tag name, simplifying the task of finding relevant parts of the HTML tree.

## Usage

```csharp
var parser = new HTMLParserService();
HtmlElement document = parser.Parse(htmlContent);
IEnumerable<HtmlElement> links = document.FindElementsByTag("a");
links.ToList().ForEach(link => Console.WriteLine(link.Attributes["href"]));
```

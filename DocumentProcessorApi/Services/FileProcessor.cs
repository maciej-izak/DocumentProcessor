using DocumentProcessorApi.Models;
using DocumentProcessorApi.Exceptions;
using System.Globalization;
using System.Text;

namespace DocumentProcessorApi.Services;

/// <summary>
/// The result of processing the file.
/// </summary>
public class ProcessResult
{
    /// <summary>
    /// List of parsed documents.
    /// </summary>
    public List<Document> Documents { get; set; } = new();

    /// <summary>
    /// Total number of lines processed.
    /// </summary>
    public int LineCount { get; set; }

    /// <summary>
    /// Total number of characters processed.
    /// </summary>
    public int CharCount { get; set; }
}

/// <summary>
/// Interface for file processing.
/// </summary>
public interface IFileProcessor
{
    /// <summary>
    /// Processes the input stream and returns the parsing result.
    /// </summary>
    /// <param name="stream">Input stream of the file.</param>
    /// <returns>Result of the processing.</returns>
    ProcessResult Process(Stream stream);
}

/// <summary>
/// Implementation of the file processor.
/// </summary>
public class FileProcessor : IFileProcessor
{
    private readonly List<ReadOnlyMemory<char>> _columns = new(16); // Maximum number of columns
    private int _lineCount = 0;

    /// <summary>
    /// Processes the input stream and parses the content.
    /// </summary>
    /// <param name="stream">Input stream of the file.</param>
    /// <returns>Result of the processing.</returns>
    public ProcessResult Process(Stream stream)
    {
        var result = new ProcessResult();
        Document currentDocument = null!;

        // StreamReader handles BOM if present
        using var reader = new StreamReader(
            stream,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: false
        );

        // We'll accumulate partial reads here and extract lines as soon as we find newlines
        var leftover = new StringBuilder();
        char[] buffer = new char[4096];
        int bytesRead;

        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            // Append the newly read chunk to leftover
            leftover.Append(buffer, 0, bytesRead);

            // We'll parse leftover for complete lines and remove them as we go
            int lineStart = 0;
            for (int i = 0; i < leftover.Length; i++)
            {
                // Check for '\r', '\n', or '\r\n'
                if (leftover[i] == '\r')
                {
                    // Check if we have a "\r\n" sequence
                    if (i + 1 < leftover.Length && leftover[i + 1] == '\n')
                    {
                        // We found a "\r\n" line break
                        int lineLength = i - lineStart; // excludes the "\r"
                        string lineContent = leftover.ToString(lineStart, lineLength);

                        // Process the line (including all trailing spaces, no trimming!)
                        ProcessLine(lineContent.AsSpan(), ref currentDocument, result);

                        // Add the line content length plus 2 for "\r\n"
                        result.CharCount += lineLength + 2;

                        // Remove the processed line and the "\r\n" from leftover
                        leftover.Remove(lineStart, lineLength + 2);

                        // Adjust the loop index to restart from just before new leftover start
                        i = lineStart - 1;
                    }
                    else
                    {
                        // Standalone '\r' is a line break
                        int lineLength = i - lineStart;
                        string lineContent = leftover.ToString(lineStart, lineLength);

                        // Process the complete line
                        ProcessLine(lineContent.AsSpan(), ref currentDocument, result);

                        // Update character count (including '\r')
                        result.CharCount += lineLength + 1;

                        // Remove the processed line and '\r' from leftover
                        leftover.Remove(lineStart, lineLength + 1);

                        // Adjust the loop index
                        i = lineStart - 1;
                    }
                }
                else if (leftover[i] == '\n')
                {
                    // Standalone '\n' is a line break
                    int lineLength = i - lineStart;
                    string lineContent = leftover.ToString(lineStart, lineLength);

                    // Process the complete line
                    ProcessLine(lineContent.AsSpan(), ref currentDocument, result);

                    // Update character count (including '\n')
                    result.CharCount += lineLength + 1;

                    // Remove the processed line and '\n' from leftover
                    leftover.Remove(lineStart, lineLength + 1);

                    // Adjust the loop index
                    i = lineStart - 1;
                }
            }
        }

        // After we've read the entire file, if leftover still has content,
        // that's a final line with no terminating newline.
        if (leftover.Length > 0)
        {
            string lineContent = leftover.ToString(); // No trimming
            ProcessLine(lineContent.AsSpan(), ref currentDocument, result);
            result.CharCount += lineContent.Length;
        }

        // Final validation for the last document
        if (currentDocument is { })
        {
            ValidateDocumentHasPositions(currentDocument, _lineCount);
        }

        result.LineCount = _lineCount;
        return result;
    }

    /// <summary>
    /// Processes a single line, updates the current document, and updates the result.
    /// </summary>
    /// <param name="line">The line content.</param>
    /// <param name="currentDocument">Reference to the current document.</param>
    /// <param name="result">The processing result to update.</param>
    private void ProcessLine(ReadOnlySpan<char> line, ref Document currentDocument, ProcessResult result)
    {
        _lineCount++;
        if (line.IsEmpty)
            return;

        // Split the line by commas without allocating new strings
        SplitByComma(line);
        if (_columns.Count == 0)
            return;

        // The first column determines the line type
        var lineTypeSpan = _columns[0].Span.Trim();

        try
        {
            switch (lineTypeSpan.ToString())
            {
                case "H":
                    // Before processing a new header, validate the current document
                    if (currentDocument is { })
                    {
                        ValidateDocumentHasPositions(currentDocument, _lineCount);
                    }

                    currentDocument = ParseHeader(line);
                    result.Documents.Add(currentDocument);
                    break;

                case "B":
                    if (currentDocument == null)
                        throw new ParseException("Position without a document header.", _lineCount, 0, 0);

                    var position = ParsePosition(line);
                    currentDocument.Positions.Add(position);
                    break;

                case "C":
                    // Comment line - can be skipped or handled if needed
                    break;

                default:
                    throw new ParseException($"Unknown line type: '{lineTypeSpan}'.", _lineCount, 0, 0);
            }
        }
        catch (ParseException)
        {
            // Re-throw the same parse exception
            throw;
        }
        catch (Exception ex)
        {
            // Wrap any other exception
            throw new ParseException("Error processing line.", _lineCount, 0, 0, ex);
        }
    }

    /// <summary>
    /// Validates that the document has at least one position unless the Brutto value is zero.
    /// </summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="currentLine">The current line number for error reporting.</param>
    private void ValidateDocumentHasPositions(Document document, int currentLine)
    {
        // Assuming 'Brutto' field determines if positions are required
        // Replace 'Brutto' with the appropriate field if different
        if (document.Brutto != 0 && document.Positions is not { Count: not 0 })
        {
            throw new ParseException(
                "Document has no positions but positions are required.",
                currentLine,
                0,
                0
            );
        }
    }

    /// <summary>
    /// Splits a line into columns based on commas without allocating new strings.
    /// </summary>
    /// <param name="line">The line to split.</param>
    private void SplitByComma(ReadOnlySpan<char> line)
    {
        _columns.Clear();
        int start = 0;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ',')
            {
                if (i > start)
                {
                    // Add the column as ReadOnlyMemory<char>
                    _columns.Add(line.Slice(start, i - start).ToArray());
                }
                else
                {
                    // Empty column
                    _columns.Add(ReadOnlyMemory<char>.Empty);
                }

                start = i + 1;
            }
        }

        // Add the last column
        if (start < line.Length)
        {
            _columns.Add(line[start..].ToArray());
        }
        else
        {
            // Line ends with a comma, add an empty column
            _columns.Add(ReadOnlyMemory<char>.Empty);
        }
    }

    /// <summary>
    /// Parses the header line and returns a Document object.
    /// </summary>
    /// <param name="line">The header line content.</param>
    /// <returns>Parsed Document object.</returns>
    private Document ParseHeader(ReadOnlySpan<char> line)
    {
        if (_columns.Count < 16)
            throw new ParseException("Invalid header format.", _lineCount, 0, 0);

        try
        {
            return new Document
            {
                BACode = TrimAndConvertToString(_columns[1], columnIndex: 1, line: line),
                DocumentType = TrimAndConvertToString(_columns[2], columnIndex: 2, line: line),
                DocumentNumber = TrimAndConvertToString(_columns[3], columnIndex: 3, line: line),
                OperationDate = ParseDate(_columns[4], columnIndex: 4, line: line),
                DocumentDayNumber = TrimAndConvertToString(_columns[5], columnIndex: 5, line: line),
                ContractorCode = TrimAndConvertToString(_columns[6], columnIndex: 6, line: line),
                ContractorName = TrimAndConvertToString(_columns[7], columnIndex: 7, line: line),
                ExternalDocumentNumber = TrimAndConvertToString(_columns[8], columnIndex: 8, line: line),
                ExternalDocumentDate = ParseDate(_columns[9], columnIndex: 9, line: line),
                Netto = ParseDecimal(_columns[10], columnIndex: 10, line: line),
                Vat = ParseDecimal(_columns[11], columnIndex: 11, line: line),
                Brutto = ParseDecimal(_columns[12], columnIndex: 12, line: line),
                Fl = ParseDecimal(_columns[13], columnIndex: 13, line: line),
                F2 = ParseDecimal(_columns[14], columnIndex: 14, line: line),
                F3 = ParseDecimal(_columns[15], columnIndex: 15, line: line)
            };
        }
        catch (ParseException)
        {
            // Re-throw the same parse exception
            throw;
        }
        catch (Exception ex)
        {
            // Wrap any other exception
            throw new ParseException("Error parsing header.", _lineCount, 0, 0, ex);
        }
    }

    /// <summary>
    /// Parses a position line and returns a Position object.
    /// </summary>
    /// <param name="line">The position line content.</param>
    /// <returns>Parsed Position object.</returns>
    private Position ParsePosition(ReadOnlySpan<char> line)
    {
        if (_columns.Count < 12)
            throw new ParseException("Invalid position format.", _lineCount, 0, 0);

        try
        {
            return new Position
            {
                ProductCode = TrimAndConvertToString(_columns[1], columnIndex: 1, line: line),
                ProductName = TrimAndConvertToString(_columns[2], columnIndex: 2, line: line),
                Quantity = ParseDecimal(_columns[3], columnIndex: 3, line: line),
                PriceNetto = ParseDecimal(_columns[4], columnIndex: 4, line: line),
                ValueNetto = ParseDecimal(_columns[5], columnIndex: 5, line: line),
                Vat = ParseDecimal(_columns[6], columnIndex: 6, line: line),
                LengthBefore = ParseNullableDecimal(_columns[7], columnIndex: 7, line: line),
                AvgBefore = ParseNullableDecimal(_columns[8], columnIndex: 8, line: line),
                LengthAfter = ParseNullableDecimal(_columns[9], columnIndex: 9, line: line),
                AvgAfter = ParseNullableDecimal(_columns[10], columnIndex: 10, line: line),
                Group = TrimAndConvertToString(_columns[11], columnIndex: 11, line: line)
            };
        }
        catch (ParseException)
        {
            // Re-throw the same parse exception
            throw;
        }
        catch (Exception ex)
        {
            // Wrap any other exception
            throw new ParseException("Error parsing position.", _lineCount, 0, 0, ex);
        }
    }

    /// <summary>
    /// Trims the input and converts it to a string.
    /// Calculates the starting character position of the column for error reporting.
    /// </summary>
    /// <param name="memory">The input memory.</param>
    /// <param name="columnIndex">The column index for error reporting.</param>
    /// <param name="line">The entire line content.</param>
    /// <returns>Trimmed string.</returns>
    private string TrimAndConvertToString(ReadOnlyMemory<char> memory, int columnIndex, ReadOnlySpan<char> line)
    {
        // Calculate the starting character position of this column
        int startPosition = GetColumnStartPosition(line, columnIndex);
        // For simplicity, assume trimming doesn't alter character position
        return memory.Span.Trim().ToString();
    }

    /// <summary>
    /// Parses a date from the input column. Throws ParseException on error with character position.
    /// </summary>
    /// <param name="memory">The input memory.</param>
    /// <param name="columnIndex">The column index for error reporting.</param>
    /// <param name="line">The entire line content.</param>
    /// <returns>Parsed DateTime object.</returns>
    private DateTime ParseDate(ReadOnlyMemory<char> memory, int columnIndex, ReadOnlySpan<char> line)
    {
        var input = memory.Span;
        if (DateTime.TryParseExact(
                input,
                "dd-MM-yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date))
        {
            return date;
        }

        // Calculate the character position: start of the column
        int charPos = GetColumnStartPosition(line, columnIndex);
        throw new ParseException($"Invalid date format: '{input.ToString()}'", _lineCount, charPos, columnIndex);
    }

    /// <summary>
    /// Parses a decimal from the input column. Throws ParseException on error with character position.
    /// </summary>
    /// <param name="memory">The input memory.</param>
    /// <param name="columnIndex">The column index for error reporting.</param>
    /// <param name="line">The entire line content.</param>
    /// <returns>Parsed decimal value.</returns>
    private decimal ParseDecimal(ReadOnlyMemory<char> memory, int columnIndex, ReadOnlySpan<char> line)
    {
        var input = memory.Span;
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        // Calculate the character position: start of the column
        int charPos = GetColumnStartPosition(line, columnIndex);
        throw new ParseException($"Invalid number value: '{input.ToString()}'", _lineCount, charPos, columnIndex);
    }

    /// <summary>
    /// Parses a nullable decimal from the input column. Throws ParseException on error with character position.
    /// </summary>
    /// <param name="memory">The input memory.</param>
    /// <param name="columnIndex">The column index for error reporting.</param>
    /// <param name="line">The entire line content.</param>
    /// <returns>Parsed nullable decimal value.</returns>
    private decimal? ParseNullableDecimal(ReadOnlyMemory<char> memory, int columnIndex, ReadOnlySpan<char> line)
    {
        var input = memory.Span;
        if (input.IsEmpty)
            return null;

        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        // Calculate the character position: start of the column
        int charPos = GetColumnStartPosition(line, columnIndex);
        throw new ParseException($"Invalid nullable number value: '{input.ToString()}'", _lineCount, charPos, columnIndex);
    }

    /// <summary>
    /// Calculates the starting character position of a given column in a line.
    /// </summary>
    /// <param name="line">The entire line content.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <returns>The starting character position of the column (indexed from 1).</returns>
    private int GetColumnStartPosition(ReadOnlySpan<char> line, int columnIndex)
    {
        if (columnIndex == 0)
            return 1;

        int currentColumn = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ',')
            {
                currentColumn++;
                if (currentColumn == columnIndex)
                {
                    return i + 2; // Start position of the desired column
                }
            }
        }

        // If columnIndex is out of range, return the end of the line
        return line.Length + 1;
    }
}

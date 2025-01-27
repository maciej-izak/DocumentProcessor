using DocumentProcessorApi.Exceptions;
using DocumentProcessorApi.Services;
using System.Text;

namespace DocumentProcessorApi.Tests.Services;

public class FileProcessorTests
{
    private readonly FileProcessor _fileProcessor;

    public FileProcessorTests()
    {
        _fileProcessor = new FileProcessor();
    }

    /// <summary>
    /// Tests processing an empty file. Should return a ProcessResult with zero lines and characters, and an empty list of documents.
    /// </summary>
    [Fact]
    public void Process_EmptyFile_ReturnsEmptyResult()
    {
        // Arrange
        var content = "";
        using var stream = GenerateStreamFromString(content);

        // Act
        var result = _fileProcessor.Process(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Documents);
        Assert.Equal(0, result.LineCount);
        Assert.Equal(0, result.CharCount);
    }

    /// <summary>
    /// Tests processing a file containing only a header without positions. If Brutto = 0, it should be accepted.
    /// </summary>
    [Fact]
    public void Process_HeaderOnly_BruttoZero_ReturnsValidResult()
    {
        // Arrange: Header with Brutto = 0
        var content = "H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,0.00,0.00,0.00,0.00,0.00,0.00,";
        using var stream = GenerateStreamFromString(content);

        // Act
        var result = _fileProcessor.Process(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Documents);
        Assert.Equal(1, result.LineCount);
        Assert.Equal(content.Length, result.CharCount);

        var document = result.Documents[0];
        Assert.Equal("5308", document.BACode);
        Assert.Equal("02", document.DocumentType);
        Assert.Equal("00130", document.DocumentNumber);
        Assert.Equal(new DateTime(2015, 1, 29), document.OperationDate);
        Assert.Equal("5222", document.DocumentDayNumber);
        Assert.Equal("10140", document.ContractorCode);
        Assert.Equal("KOL S.A.", document.ContractorName);
        Assert.Equal("20150128099911", document.ExternalDocumentNumber);
        Assert.Equal(new DateTime(2015, 1, 28), document.ExternalDocumentDate);
        Assert.Equal(0.00m, document.Netto);
        Assert.Equal(0.00m, document.Vat);
        Assert.Equal(0.00m, document.Brutto);
        Assert.Equal(0.00m, document.Fl);
        Assert.Equal(0.00m, document.F2);
        Assert.Equal(0.00m, document.F3);
        Assert.Empty(document.Positions);
    }

    /// <summary>
    /// Tests processing a file with a header and valid positions.
    /// </summary>
    [Fact]
    public void Process_ValidFile_ReturnsCorrectResult()
    {
        // Arrange: File with a header and two positions
        var content = 
            @"H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,-34.37,-2.75,-37.12,0.00,0.00,0.00,
B,19556,NASZ DZIENNIK,-3.000,1.63000,-4.89,-0.39,5.000,1.73552,2.000,1.89379,1117,
B,25947,AUTO WIAT CLASSIC,-2.000,14.74000,-29.48,-2.36,3.000,14.74000,1.000,14.74000,1117,";
        using var stream = GenerateStreamFromString(content);

        // Act
        var result = _fileProcessor.Process(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Documents);
        Assert.Equal(3, result.LineCount);
        Assert.Equal(content.Length, result.CharCount);

        var document = result.Documents[0];
        Assert.Equal("5308", document.BACode);
        Assert.Equal("02", document.DocumentType);
        Assert.Equal("00130", document.DocumentNumber);
        Assert.Equal(new DateTime(2015, 1, 29), document.OperationDate);
        Assert.Equal("5222", document.DocumentDayNumber);
        Assert.Equal("10140", document.ContractorCode);
        Assert.Equal("KOL S.A.", document.ContractorName);
        Assert.Equal("20150128099911", document.ExternalDocumentNumber);
        Assert.Equal(new DateTime(2015, 1, 28), document.ExternalDocumentDate);
        Assert.Equal(-34.37m, document.Netto);
        Assert.Equal(-2.75m, document.Vat);
        Assert.Equal(-37.12m, document.Brutto);
        Assert.Equal(0.00m, document.Fl);
        Assert.Equal(0.00m, document.F2);
        Assert.Equal(0.00m, document.F3);

        Assert.Equal(2, document.Positions.Count);

        var firstPosition = document.Positions[0];
        Assert.Equal("19556", firstPosition.ProductCode);
        Assert.Equal("NASZ DZIENNIK", firstPosition.ProductName);
        Assert.Equal(-3.000m, firstPosition.Quantity);
        Assert.Equal(1.63000m, firstPosition.PriceNetto);
        Assert.Equal(-4.89m, firstPosition.ValueNetto);
        Assert.Equal(-0.39m, firstPosition.Vat);
        Assert.Equal(5.000m, firstPosition.LengthBefore);
        Assert.Equal(1.73552m, firstPosition.AvgBefore);
        Assert.Equal(2.000m, firstPosition.LengthAfter);
        Assert.Equal(1.89379m, firstPosition.AvgAfter);
        Assert.Equal("1117", firstPosition.Group);

        var secondPosition = document.Positions[1];
        Assert.Equal("25947", secondPosition.ProductCode);
        Assert.Equal("AUTO WIAT CLASSIC", secondPosition.ProductName);
        Assert.Equal(-2.000m, secondPosition.Quantity);
        Assert.Equal(14.74000m, secondPosition.PriceNetto);
        Assert.Equal(-29.48m, secondPosition.ValueNetto);
        Assert.Equal(-2.36m, secondPosition.Vat);
        Assert.Equal(3.000m, secondPosition.LengthBefore);
        Assert.Equal(14.74000m, secondPosition.AvgBefore);
        Assert.Equal(1.000m, secondPosition.LengthAfter);
        Assert.Equal(14.74000m, secondPosition.AvgAfter);
        Assert.Equal("1117", secondPosition.Group);
    }

    /// <summary>
    /// Tests processing a file with an invalid date format in the header. Should throw a ParseException.
    /// </summary>
    [Fact]
    public void Process_InvalidDateFormat_ThrowsParseException()
    {
        // Arrange: Header with an incorrect date format
        var content = "H,5308,02,00130,29/01/2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,-34.37,-2.75,-37.12,0.00,0.00,0.00,";
        using var stream = GenerateStreamFromString(content);

        // Act & Assert
        var exception = Assert.Throws<ParseException>(() => _fileProcessor.Process(stream));
        Assert.Equal("Invalid date format: '29/01/2015'", exception.Message);
        Assert.Equal(1, exception.LineNumber);
        Assert.Equal(17, exception.CharPosition);
        Assert.Equal(4, exception.ColumnIndex);
    }

    /// <summary>
    /// Tests processing a file with an invalid numeric value in the header. Should throw a ParseException.
    /// </summary>
    [Fact]
    public void Process_InvalidNumberFormat_ThrowsParseException()
    {
        // Arrange: Header with an invalid numeric format in the Netto field
        var content = "H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,abc,-2.75,-37.12,0.00,0.00,0.00,";
        using var stream = GenerateStreamFromString(content);

        // Act & Assert
        var exception = Assert.Throws<ParseException>(() => _fileProcessor.Process(stream));
        Assert.Equal("Invalid number value: 'abc'", exception.Message);
        Assert.Equal(1, exception.LineNumber);
        Assert.Equal(74, exception.CharPosition);
        Assert.Equal(10, exception.ColumnIndex);
    }

    /// <summary>
    /// Tests processing a file with a document without positions when Brutto != 0. Should throw a ParseException.
    /// </summary>
    [Fact]
    public void Process_DocumentWithoutPositions_BruttoNotZero_ThrowsParseException()
    {
        // Arrange: Header without positions, Brutto != 0
        var content = "H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,-34.37,-2.75,-37.12,10.00,0.00,0.00,";
        using var stream = GenerateStreamFromString(content);

        // Act & Assert
        var exception = Assert.Throws<ParseException>(() => _fileProcessor.Process(stream));
        Assert.Equal("Document has no positions but positions are required.", exception.Message);
        Assert.Equal(1, exception.LineNumber);
        Assert.Equal(0, exception.CharPosition);
        Assert.Equal(0, exception.ColumnIndex);
    }

    /// <summary>
    /// Tests processing a file with a document without positions when Brutto = 0. Should return a valid result.
    /// </summary>
    [Fact]
    public void Process_DocumentWithoutPositions_BruttoZero_ReturnsValidResult()
    {
        // Arrange: Header without positions, Brutto = 0
        var content = "H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,0.00,0.00,0.00,0.00,0.00,0.00,";
        using var stream = GenerateStreamFromString(content);

        // Act
        var result = _fileProcessor.Process(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Documents);
        Assert.Equal(1, result.LineCount);
        Assert.Equal(content.Length, result.CharCount);

        var document = result.Documents[0];
        Assert.Equal("5308", document.BACode);
        Assert.Equal("02", document.DocumentType);
        Assert.Equal("00130", document.DocumentNumber);
        Assert.Equal(new DateTime(2015, 1, 29), document.OperationDate);
        Assert.Equal("5222", document.DocumentDayNumber);
        Assert.Equal("10140", document.ContractorCode);
        Assert.Equal("KOL S.A.", document.ContractorName);
        Assert.Equal("20150128099911", document.ExternalDocumentNumber);
        Assert.Equal(new DateTime(2015, 1, 28), document.ExternalDocumentDate);
        Assert.Equal(0.00m, document.Netto);
        Assert.Equal(0.00m, document.Vat);
        Assert.Equal(0.00m, document.Brutto);
        Assert.Equal(0.00m, document.Fl);
        Assert.Equal(0.00m, document.F2);
        Assert.Equal(0.00m, document.F3);
        Assert.Empty(document.Positions);
    }
        
    /// <summary>
    /// Tests processing a file with an unknown line type.
    /// Should throw a ParseException.
    /// </summary>
    [Fact]
    public void Process_UnknownLineType_ThrowsParseException()
    {
        // Arrange: A file containing an unknown line type 'X'
        var content =
            @"H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,-34.37,-2.75,-37.12,0.00,0.00,0.00,
X,Unknown line type, should cause an error";
        using var stream = GenerateStreamFromString(content);

        // Act & Assert
        var exception = Assert.Throws<ParseException>(() => _fileProcessor.Process(stream));
        Assert.Contains("Unknown line type", exception.Message);
    }

    /// <summary>
    /// Tests processing a file with multiple documents, one valid and one invalid.
    /// Should throw a ParseException for the invalid document.
    /// </summary>
    [Fact]
    public void Process_MultipleDocuments_OneInvalid_ThrowsParseException()
    {
        // Arrange: Two headers, first with Brutto != 0 without positions, second valid with Brutto =0
        var content = 
            @"H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,-34.37,-2.75,-37.12,10.00,0.00,0.00,
H,5309,02,00131,30-01-2015,5223,10141,ABC S.A.,20150129099912,29-01-2015,0.00,0.00,0.00,0.00,0.00,0.00,";
        using var stream = GenerateStreamFromString(content);

        // Act & Assert
        var exception = Assert.Throws<ParseException>(() => _fileProcessor.Process(stream));
        Assert.Equal("Document has no positions but positions are required.", exception.Message);
        Assert.Equal(2, exception.LineNumber);
        Assert.Equal(0, exception.CharPosition);
        Assert.Equal(0, exception.ColumnIndex);
    }

    /// <summary>
    /// Helper method to generate a memory stream from a string.
    /// </summary>
    /// <param name="s">String to convert into a stream.</param>
    /// <returns>A MemoryStream instance.</returns>
    private Stream GenerateStreamFromString(string s)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(s));
    }
}
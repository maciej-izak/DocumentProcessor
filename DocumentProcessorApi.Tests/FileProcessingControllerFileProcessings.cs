using DocumentProcessorApi.Controllers;
using DocumentProcessorApi.Models;
using DocumentProcessorApi.Services;
using DocumentProcessorApi.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DocumentProcessorApi.Tests
{
    public class FileProcessingControllerTests
    {
        private readonly FileProcessingController _controller;
        private readonly Mock<IFileProcessor> _fileProcessorMock;

        public FileProcessingControllerTests()
        {
            // Initialize the mock for IFileProcessor
            _fileProcessorMock = new Mock<IFileProcessor>();

            // Inject the mock into the controller
            _controller = new FileProcessingController(_fileProcessorMock.Object);
        }

        /// <summary>
        /// Ensures that a missing file results in a 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task ProcessFile_NoFile_ReturnsBadRequest()
        {
            // Arrange
            int x = 2;
            IFormFile file = null;

            // Act
            var result = await _controller.ProcessFile(x, file);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var responseObject = badRequest.Value;

            // Check if the response contains the 'detail' property
            var detailProperty = responseObject.GetType().GetProperty("detail");
            Assert.NotNull(detailProperty);

            // Verify the content of the 'detail' property
            var detailValue = detailProperty.GetValue(responseObject)?.ToString();
            Assert.Equal("The uploaded file is missing or empty.", detailValue);
        }

        /// <summary>
        /// Ensures that an empty file results in a 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task ProcessFile_EmptyFile_ReturnsBadRequest()
        {
            // Arrange
            int x = 2;
            var content = "";
            var file = CreateFormFile(content);

            // Act
            var result = await _controller.ProcessFile(x, file);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var responseObject = badRequest.Value;

            // Check if the response contains the 'detail' property
            var detailProperty = responseObject.GetType().GetProperty("detail");
            Assert.NotNull(detailProperty);

            // Verify the content of the 'detail' property
            var detailValue = detailProperty.GetValue(responseObject)?.ToString();
            Assert.Equal("The uploaded file is missing or empty.", detailValue);
        }

        /// <summary>
        /// Ensures that an invalid header format triggers a 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task ProcessFile_InvalidHeader_ReturnsBadRequest()
        {
            // Arrange: The file contains an incomplete header
            int x = 2;
            var content = "H,5308"; // Incomplete header
            var file = CreateFormFile(content);

            // Mock the file processor to throw a ParseException due to invalid header format
            _fileProcessorMock.Setup(p => p.Process(It.IsAny<Stream>()))
                .Throws(new ParseException("Invalid header format.", 1, 0, 0));

            // Act
            var result = await _controller.ProcessFile(x, file);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

            var responseObject = badRequest.Value;
            var detailProperty = responseObject.GetType().GetProperty("detail");
            Assert.NotNull(detailProperty);

            var detailValue = detailProperty.GetValue(responseObject)?.ToString();
            Assert.Contains("Invalid header format.", detailValue);
        }

        /// <summary>
        /// Ensures that a valid file with proper documents and positions results in a 200 OK response.
        /// </summary>
        [Fact]
        public async Task ProcessFile_ValidFile_ReturnsOk()
        {
            // Arrange: A valid file with one document containing two positions
            int x = 1;
            var content = """
                          H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,-34.37,-2.75,-37.12,0.00,0.00,0.00,
                          B,19556,NASZ DZIENNIK,-3.000,1.63000,-4.89,-0.39,5.000,1.73552,2.000,1.89379,1117,
                          B,25947,AUTO WIAT CLASSIC,-2.000,14.74000,-29.48,-2.36,3.000,14.74000,1.000,14.74000,1117,
                          """;
            var file = CreateFormFile(content);

            // Mock the file processor to return a valid ProcessResult
            _fileProcessorMock.Setup(p => p.Process(It.IsAny<Stream>()))
                .Returns(new ProcessResult
                {
                    Documents = new List<Document>
                    {
                        new Document
                        {
                            BACode = "5308",
                            DocumentType = "02",
                            DocumentNumber = "00130",
                            OperationDate = new DateTime(2015, 1, 29),
                            DocumentDayNumber = "5222",
                            ContractorCode = "10140",
                            ContractorName = "KOL S.A.",
                            ExternalDocumentNumber = "20150128099911",
                            ExternalDocumentDate = new DateTime(2015, 1, 28),
                            Netto = -34.37m,
                            Vat = -2.75m,
                            Brutto = -37.12m,
                            Fl = 0.00m,
                            F2 = 0.00m,
                            F3 = 0.00m,
                            Positions = new List<Position>
                            {
                                new Position
                                {
                                    ProductCode = "19556",
                                    ProductName = "NASZ DZIENNIK",
                                    Quantity = -3.000m, // Correct first position
                                    PriceNetto = 1.63000m,
                                    ValueNetto = -4.89m,
                                    Vat = -0.39m,
                                    LengthBefore = 5.000m,
                                    AvgBefore = 1.73552m,
                                    LengthAfter = 2.000m,
                                    AvgAfter = 1.89379m,
                                    Group = "1117"
                                },
                                new Position
                                {
                                    ProductCode = "25947",
                                    ProductName = "AUTO WIAT CLASSIC",
                                    Quantity = -2.000m, // Correct second position
                                    PriceNetto = 14.74000m,
                                    ValueNetto = -29.48m,
                                    Vat = -2.36m,
                                    LengthBefore = 3.000m,
                                    AvgBefore = 14.74000m,
                                    LengthAfter = 1.000m,
                                    AvgAfter = 14.74000m,
                                    Group = "1117"
                                }
                            }
                        }
                    },
                    LineCount = 3, // Number of lines in the content
                    CharCount = content.Length // Total number of characters
                });

            // Act
            var result = await _controller.ProcessFile(x, file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            // Serialize the response to JSON with CamelCase naming
            var jsonString = JsonSerializer.Serialize(okResult.Value, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);

            Assert.NotNull(response);
            Assert.True(response.ContainsKey("documents"));
            Assert.True(response.ContainsKey("lineCount"));
            Assert.True(response.ContainsKey("charCount"));

            // Verify the number of documents
            Assert.Equal(1, response["documents"].EnumerateArray().Count());

            // Verify the number of positions in the document
            var firstDocument = response["documents"].EnumerateArray().First();
            Assert.True(firstDocument.TryGetProperty("positions", out var positions));
            Assert.Equal(2, positions.GetArrayLength());

            // Verify LineCount and CharCount
            Assert.Equal(3, response["lineCount"].GetInt32());
            Assert.Equal(content.Length, response["charCount"].GetInt32());
        }

        /// <summary>
        /// Ensures that processing a file with a document that lacks positions 
        /// and has a non-zero Brutto value results in a 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task ProcessFile_DocumentWithoutPositions_ReturnsBadRequest()
        {
            // Arrange: A file with a header and a comment line, containing a document without positions
            int x = 2;
            var content = @"H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,-34.37,-2.75,-37.12,10.00,0.00,0.00,
C,This is a comment line that should be skipped.";
            var file = CreateFormFile(content);

            // Mock the file processor to return a ProcessResult with one document lacking positions
            _fileProcessorMock.Setup(p => p.Process(It.IsAny<Stream>()))
                .Returns(new ProcessResult
                {
                    Documents = new List<Document>
                    {
                        new Document
                        {
                            BACode = "5308",
                            DocumentType = "02",
                            DocumentNumber = "00130",
                            OperationDate = new DateTime(2015, 1, 29),
                            DocumentDayNumber = "5222",
                            ContractorCode = "10140",
                            ContractorName = "KOL S.A.",
                            ExternalDocumentNumber = "20150128099911",
                            ExternalDocumentDate = new DateTime(2015, 1, 28),
                            Netto = -34.37m,
                            Vat = -2.75m,
                            Brutto = -37.12m, // Brutto != 0
                            Fl = 10.00m,      // Example value
                            F2 = 0.00m,
                            F3 = 0.00m,
                            Positions = new List<Position>() // No positions
                        }
                    },
                    LineCount = 2, // Header and comment lines
                    CharCount = content.Length
                });

            // Act
            var result = await _controller.ProcessFile(x, file);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

            var responseObject = badRequest.Value;
            Assert.NotNull(responseObject);

            // Check if the response contains the 'detail' property
            var detailProperty = responseObject.GetType().GetProperty("detail");
            Assert.NotNull(detailProperty);

            // Verify the content of the 'detail' property
            var detailValue = detailProperty.GetValue(responseObject)?.ToString();
            Assert.Equal("Document has no positions but positions are required.", detailValue);
        }

        /// <summary>
        /// Ensures that a document without positions, where `Brutto` is **not zero**, results in a 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task ProcessFile_DocumentWithoutPositions_BruttoNotZero_ReturnsBadRequest()
        {
            // Arrange: A document with Brutto = -37.12 but without positions
            int x = 3;
            var content = """
                          H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,-34.37,-2.75,-37.12,10.00,0.00,0.00,
                          """;
            var file = CreateFormFile(content);

            // Mock the file processor to throw a ParseException due to missing positions
            _fileProcessorMock.Setup(p => p.Process(It.IsAny<Stream>()))
                .Throws(new ParseException("Document has no positions but positions are required.", 1, 0, 0));

            // Act
            var result = await _controller.ProcessFile(x, file);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

            var responseObject = badRequest.Value;
            var detailProperty = responseObject.GetType().GetProperty("detail");
            Assert.NotNull(detailProperty);

            var detailValue = detailProperty.GetValue(responseObject)?.ToString();
            Assert.Contains("Document has no positions but positions are required.", detailValue);
        }

        /// <summary>
        /// Ensures that a document without positions and Brutto equal to zero is accepted and returns a 200 OK.
        /// </summary>
        [Fact]
        public async Task ProcessFile_DocumentWithoutPositions_BruttoZero_ReturnsOk()
        {
            // Arrange: A document with Brutto = 0.00 and without positions
            int x = 4;
            var content = """
                          H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,0.00,0.00,0.00,0.00,0.00,0.00,
                          """;
            var file = CreateFormFile(content);

            // Mock the file processor to return a valid ProcessResult with a document without positions
            _fileProcessorMock.Setup(p => p.Process(It.IsAny<Stream>()))
                .Returns(new ProcessResult
                {
                    Documents = new List<Document>
                    {
                        new Document
                        {
                            BACode = "5308",
                            DocumentType = "02",
                            DocumentNumber = "00130",
                            OperationDate = new DateTime(2015, 1, 29),
                            DocumentDayNumber = "5222",
                            ContractorCode = "10140",
                            ContractorName = "KOL S.A.",
                            ExternalDocumentNumber = "20150128099911",
                            ExternalDocumentDate = new DateTime(2015, 1, 28),
                            Netto = 0.00m,
                            Vat = 0.00m,
                            Brutto = 0.00m, // Brutto = 0
                            Fl = 0.00m,
                            F2 = 0.00m,
                            F3 = 0.00m,
                            Positions = new List<Position>() // No positions
                        }
                    },
                    LineCount = 1,
                    CharCount = content.Length
                });

            // Act
            var result = await _controller.ProcessFile(x, file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            // Serialize the response to JSON with CamelCase naming
            var jsonString = JsonSerializer.Serialize(okResult.Value, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);

            Assert.NotNull(response);
            Assert.True(response.ContainsKey("documents"));
            Assert.True(response.ContainsKey("lineCount"));
            Assert.True(response.ContainsKey("charCount"));

            // Verify the number of documents
            Assert.Equal(1, response["documents"].EnumerateArray().Count());

            // Verify that the document has no positions
            var firstDocument = response["documents"].EnumerateArray().First();
            Assert.True(firstDocument.TryGetProperty("positions", out var positions));
            Assert.Equal(0, positions.GetArrayLength());

            // Verify LineCount and CharCount
            Assert.Equal(1, response["lineCount"].GetInt32());
            Assert.Equal(content.Length, response["charCount"].GetInt32());
        }

        /// <summary>
        /// Ensures that processing multiple documents where at least one document lacks positions and Brutto is not zero results in a 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task ProcessFile_MultipleDocuments_OneInvalid_ReturnsBadRequest()
        {
            // Arrange: Two documents, the first without positions and Brutto != 0, the second valid
            int x = 5;
            var content = """
                          H,5308,02,00130,29-01-2015,5222,10140,KOL S.A.,20150128099911,28-01-2015,-34.37,-2.75,-37.12,10.00,0.00,0.00,
                          H,5309,02,00131,30-01-2015,5223,10141,ABC S.A.,20150129099912,29-01-2015,-20.00,-1.50,-21.50,0.00,0.00,0.00,
                          """;
            var file = CreateFormFile(content);

            // Mock the file processor to throw a ParseException for the first document
            _fileProcessorMock.Setup(p => p.Process(It.IsAny<Stream>()))
                .Throws(new ParseException("Document has no positions but positions are required.", 2, 0, 0));

            // Act
            var result = await _controller.ProcessFile(x, file);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

            var responseObject = badRequest.Value;
            var detailProperty = responseObject.GetType().GetProperty("detail");
            Assert.NotNull(detailProperty);

            var detailValue = detailProperty.GetValue(responseObject)?.ToString();
            Assert.Contains("Document has no positions but positions are required.", detailValue);
        }
        
        /// <summary>
        /// Ensures that a file exceeding the maximum allowed size results in a 400 Bad Request.
        /// </summary>
        [Fact]
        public async Task ProcessFile_TooLargeFile_ReturnsBadRequest()
        {
            // Arrange
            int x = 2;
            var fileName = "largefile.txt";
            var contentType = "text/plain";

            // Simulate a file exceeding the 10MB limit (e.g., 11MB)
            long oversizedFileSize = 11 * 1024 * 1024; // 11MB
            var fakeStream = new MemoryStream(new byte[0]); // Empty stream, but length will be faked

            var largeFile = new FormFile(fakeStream, 0, oversizedFileSize, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            // Act
            var result = await _controller.ProcessFile(x, largeFile);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var responseObject = badRequest.Value;

            // Check if the response contains the expected error message
            var detailProperty = responseObject.GetType().GetProperty("detail");
            Assert.NotNull(detailProperty);

            var detailValue = detailProperty.GetValue(responseObject)?.ToString();
            Assert.Equal("The uploaded file exceeds the maximum allowed size of 10 MB.", detailValue);
        }


        /// <summary>
        /// Utility method to create a mock IFormFile from a string content.
        /// </summary>
        /// <param name="content">The string content to include in the mock file.</param>
        /// <returns>A mock IFormFile instance.</returns>
        private IFormFile CreateFormFile(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", "testfile.PUR")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };
        }
    }
}

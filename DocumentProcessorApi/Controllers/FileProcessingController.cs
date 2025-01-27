using DocumentProcessorApi.Models;
using DocumentProcessorApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using DocumentProcessorApi.Exceptions;

namespace DocumentProcessorApi.Controllers;

[Route("api/test/{x:int}")]
[ApiController]
[Authorize(AuthenticationSchemes = "BasicAuthentication")]
public class FileProcessingController(IFileProcessor fileProcessor) : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    
    [HttpPost]
    public async Task<IActionResult> ProcessFile(int x, [FromForm] IFormFile file)
    {
        // ✅ Ensure file is provided and not empty
        if (file is not { Length: not 0 })
        {
            return BadRequest(new
            {
                type = "https://httpstatuses.io/400",
                title = "Invalid File",
                status = 400,
                detail = "The uploaded file is missing or empty."
            });
        }
        
        // ✅ Check if the file size exceeds the maximum allowed limit
        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                type = "https://httpstatuses.io/400",
                title = "File Too Large",
                status = 400,
                detail = $"The uploaded file exceeds the maximum allowed size of {MaxFileSize / (1024 * 1024)} MB."
            });
        }

        try
        {
            // ✅ Use asynchronous file streaming
            await using var stream = file.OpenReadStream();

            // ✅ Process the file
            var result = fileProcessor.Process(stream);
            
            // ✅ Validate documents: Ensure each document with Brutto != 0 has at least one position
            foreach (var doc in result.Documents)
            {
                if (doc.Brutto != 0 && doc.Positions is not { Count: not 0 })
                {
                    return BadRequest(new
                    {
                        type = "https://httpstatuses.io/400",
                        title = "Invalid Document",
                        status = 400,
                        detail = "Document has no positions but positions are required.",
                        documentNumber = doc.DocumentNumber // Optional: Include document identifier
                    });
                }
            }

            // ✅ Aggregate statistics
            int xCount = 0;
            decimal totalSum = 0;
            decimal maxNettoValue = decimal.MinValue;
            var productsWithMaxSet = new HashSet<string>();

            foreach (var doc in result.Documents)
            {
                if (doc.Positions.Count > x)
                    xCount++;
                totalSum += doc.Brutto;

                foreach (var pos in doc.Positions)
                {
                    var valNetto = Math.Abs(pos.ValueNetto);
                    if (valNetto > maxNettoValue)
                    {
                        maxNettoValue = valNetto;
                        productsWithMaxSet.Clear();
                        productsWithMaxSet.Add(pos.ProductName);
                    }
                    else if (valNetto == maxNettoValue)
                    {
                        productsWithMaxSet.Add(pos.ProductName);
                    }
                }
            }

            var productsWithMaxNames = string.Join(",", productsWithMaxSet);

            var response = new
            {
                documents = result.Documents,
                lineCount = result.LineCount,
                charCount = result.CharCount,
                sum = totalSum,
                xcount = xCount,
                productsWithMaxNetValue = productsWithMaxNames
            };

            return Ok(response);
        }
        catch (ParseException ex)
        {
            // ✅ Return structured JSON error response for parsing errors
            return BadRequest(new
            {
                type = "https://httpstatuses.io/400",
                title = "Parsing Error",
                status = 400,
                detail = ex.Message,
                lineNumber = ex.LineNumber,
                charPosition = ex.CharPosition,
                columnIndex = ex.ColumnIndex
            });
        }
        catch (Exception ex)
        {
            // ✅ Handle unexpected exceptions
            return StatusCode(500, new
            {
                type = "https://httpstatuses.io/500",
                title = "Internal Server Error",
                status = 500,
                detail = $"An unexpected error occurred: {ex.Message}"
            });
        }
    }
}
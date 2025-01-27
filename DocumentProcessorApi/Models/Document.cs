using System.Collections.Generic;

namespace DocumentProcessorApi.Models;

public class Document
{
    public string BACode { get; set; }
    public string DocumentType { get; set; }
    public string DocumentNumber { get; set; }
    public DateTime OperationDate { get; set; }
    public string DocumentDayNumber { get; set; }
    public string ContractorCode { get; set; }
    public string ContractorName { get; set; }
    public string ExternalDocumentNumber { get; set; }
    public DateTime ExternalDocumentDate { get; set; }
    public decimal Netto { get; set; }
    public decimal Vat { get; set; }
    public decimal Brutto { get; set; }
    public decimal Fl { get; set; }
    public decimal F2 { get; set; }
    public decimal F3 { get; set; }
    public List<Position> Positions { get; set; } = new();
}
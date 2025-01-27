namespace DocumentProcessorApi.Models
{
    public class Position
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal PriceNetto { get; set; }
        public decimal ValueNetto { get; set; }
        public decimal Vat { get; set; }
        public decimal? LengthBefore { get; set; }
        public decimal? AvgBefore { get; set; }
        public decimal? LengthAfter { get; set; }
        public decimal? AvgAfter { get; set; }
        public string Group { get; set; }
    }
}
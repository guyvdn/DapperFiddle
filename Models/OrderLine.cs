namespace DapperFiddle.Models
{
    public class OrderLine
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Product { get; set; }
        public decimal Amount { get; set; }
    }
}
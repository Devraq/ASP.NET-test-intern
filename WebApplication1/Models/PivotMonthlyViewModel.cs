namespace WebApplication1.Models
{
    public class PivotMonthlyViewModel
    {
        public string NIK { get; set; }
        public string Nama { get; set; }
        public Dictionary<string, int> MonthCounts { get; set; } = new();
        public int Total { get; set; }
    }
}
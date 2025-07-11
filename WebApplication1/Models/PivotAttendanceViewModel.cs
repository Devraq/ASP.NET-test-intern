namespace WebApplication1.Models
{
    public class PivotAttendanceViewModel
    {
        public string NIK { get; set; }
        public string Nama { get; set; }
        public Dictionary<DateTime, bool> AttendanceDates { get; set; } = new();
        public int Total { get; set; }
    }
}
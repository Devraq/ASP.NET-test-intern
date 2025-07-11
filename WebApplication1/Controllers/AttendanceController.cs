using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using WebApplication1.Models;


namespace YourProjectNamespace.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly IConfiguration _configuration;

        public AttendanceController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: Show input form with dropdown
        [HttpGet]
        public IActionResult SaveAttendance()
        {
            List<Employee> employees = new List<Employee>();

            string connectionString = _configuration.GetConnectionString("MyConnection");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT NIK, Nama FROM Employee";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        employees.Add(new Employee
                        {
                            NIK = reader["NIK"].ToString(),
                            Nama = reader["Nama"].ToString()
                        });
                    }
                }
            }

            ViewBag.Employees = employees;
            return View();
        }

        // POST: Save the attendance
        [HttpPost]
        public IActionResult SaveAttendance(string nik, DateTime? tanggalAbsen)
        {
            if (string.IsNullOrEmpty(nik) || tanggalAbsen == null)
            {
                ViewBag.Message = "Please select an employee and date.";
                return RedirectToAction("SaveAttendance");
            }

            string connectionString = _configuration.GetConnectionString("MyConnection");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO Absen (NIK, TanggalAbsen) VALUES (@NIK, @TanggalAbsen)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@NIK", nik);
                    cmd.Parameters.AddWithValue("@TanggalAbsen", tanggalAbsen);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "Attendance saved!";
            return RedirectToAction("SaveAttendance");
        }



        public IActionResult AttendanceList()
        {
            List<JoinAttendanceViewModel> joinedList = new List<JoinAttendanceViewModel>();

            string connectionString = _configuration.GetConnectionString("MyConnection");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                SELECT e.NIK, e.Nama, a.TanggalAbsen
                FROM Employee e
                JOIN Absen a ON e.NIK = a.NIK
                ORDER BY e.NIK, a.TanggalAbsen";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        joinedList.Add(new JoinAttendanceViewModel
                        {
                            NIK = reader["NIK"].ToString(),
                            Nama = reader["Nama"].ToString(),
                            TanggalAbsen = Convert.ToDateTime(reader["TanggalAbsen"])
                        });
                    }
                }
            }

            return View(joinedList);
        }
        public IActionResult PivotAttendance()
        {
            List<DateTime> allDates = new();
            List<PivotAttendanceViewModel> pivotData = new();

            string connectionString = _configuration.GetConnectionString("MyConnection");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // 1. Get all unique dates
                string dateSql = "SELECT DISTINCT TanggalAbsen FROM Absen ORDER BY TanggalAbsen";
                using (SqlCommand cmd = new SqlCommand(dateSql, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        allDates.Add(Convert.ToDateTime(reader["TanggalAbsen"]));
                    }
                    reader.Close();
                }

                // 2. Get all employees
                string empSql = "SELECT NIK, Nama FROM Employee ORDER BY NIK";
                using (SqlCommand cmd = new SqlCommand(empSql, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        pivotData.Add(new PivotAttendanceViewModel
                        {
                            NIK = reader["NIK"].ToString(),
                            Nama = reader["Nama"].ToString()
                        });
                    }
                    reader.Close();
                }

                // 3. Get all presence data
                string presenceSql = "SELECT NIK, TanggalAbsen FROM Absen";
                using (SqlCommand cmd = new SqlCommand(presenceSql, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string nik = reader["NIK"].ToString();
                        DateTime date = Convert.ToDateTime(reader["TanggalAbsen"]);

                        var employee = pivotData.FirstOrDefault(e => e.NIK == nik);
                        if (employee != null)
                        {
                            employee.AttendanceDates[date] = true;
                        }
                    }
                    reader.Close();
                }

                // 4. Compute totals
                foreach (var emp in pivotData)
                {
                    emp.Total = emp.AttendanceDates.Count(x => x.Value);
                    // Make sure all dates are present in the dict, even if false
                    foreach (var d in allDates)
                    {
                        if (!emp.AttendanceDates.ContainsKey(d))
                        {
                            emp.AttendanceDates[d] = false;
                        }
                    }
                }
            }

            ViewBag.AllDates = allDates;
            return View(pivotData);
        }

        public IActionResult MonthlyPivot()
        {
            List<string> allMonths = new();
            List<PivotMonthlyViewModel> pivotData = new();

            string connectionString = _configuration.GetConnectionString("MyConnection");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // 1. Get all unique year-months
                string monthSql = @"
            SELECT DISTINCT FORMAT(TanggalAbsen, 'yyyyMM') AS YearMonth
            FROM Absen
            ORDER BY YearMonth";
                using (SqlCommand cmd = new SqlCommand(monthSql, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        allMonths.Add(reader["YearMonth"].ToString());
                    }
                    reader.Close();
                }

                // 2. Get all employees
                string empSql = "SELECT NIK, Nama FROM Employee ORDER BY NIK";
                using (SqlCommand cmd = new SqlCommand(empSql, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        pivotData.Add(new PivotMonthlyViewModel
                        {
                            NIK = reader["NIK"].ToString(),
                            Nama = reader["Nama"].ToString()
                        });
                    }
                    reader.Close();
                }

                // 3. Get all presence data
                string presenceSql = "SELECT NIK, FORMAT(TanggalAbsen, 'yyyyMM') AS YearMonth FROM Absen";
                using (SqlCommand cmd = new SqlCommand(presenceSql, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string nik = reader["NIK"].ToString();
                        string ym = reader["YearMonth"].ToString();

                        var emp = pivotData.FirstOrDefault(e => e.NIK == nik);
                        if (emp != null)
                        {
                            if (emp.MonthCounts.ContainsKey(ym))
                                emp.MonthCounts[ym]++;
                            else
                                emp.MonthCounts[ym] = 1;
                        }
                    }
                    reader.Close();
                }

                // 4. Compute totals and fill missing months
                foreach (var emp in pivotData)
                {
                    foreach (var ym in allMonths)
                    {
                        if (!emp.MonthCounts.ContainsKey(ym))
                            emp.MonthCounts[ym] = 0;
                    }
                    emp.Total = emp.MonthCounts.Values.Sum();
                }
            }

            ViewBag.AllMonths = allMonths;
            return View(pivotData);
        }
public IActionResult DeleteAttendanceDate()
{
    List<DateTime> dates = new List<DateTime>();

    string connectionString = _configuration.GetConnectionString("MyConnection");
    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        conn.Open();
        string sql = "SELECT DISTINCT TanggalAbsen FROM Absen ORDER BY TanggalAbsen";
        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                dates.Add(Convert.ToDateTime(reader["TanggalAbsen"]));
            }
        }
    }

    ViewBag.Dates = dates;
    return View();
}

// POST: Delete selected date
[HttpPost]
public IActionResult DeleteAttendanceDate(DateTime tanggalAbsen)
{
    string connectionString = _configuration.GetConnectionString("MyConnection");
    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        conn.Open();
        string sql = "DELETE FROM Absen WHERE TanggalAbsen = @TanggalAbsen";
        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@TanggalAbsen", tanggalAbsen);
            int rowsAffected = cmd.ExecuteNonQuery();
            TempData["Message"] = $"{rowsAffected} record(s) deleted for {tanggalAbsen:yyyy-MM-dd}";
        }
    }

    return RedirectToAction("DeleteAttendanceDate");
}
    }

}

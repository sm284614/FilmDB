using FilmDB.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FilmDB.Controllers
{
    public class DiagnosticsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DiagnosticsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Diagnostics/QueryPerformance
        public IActionResult QueryPerformance()
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                return NotFound(); // Only available in development
            }

            var model = new QueryPerformanceModel
            {
                DatabaseConnectionString = _db.Database.GetConnectionString() ?? "Not configured",
                Queries = new List<QueryTestResult>()
            };

            return View(model);
        }

        // POST: /Diagnostics/RunQueryTests
        [HttpPost]
        public IActionResult RunQueryTests()
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                return NotFound();
            }

            var results = new List<QueryTestResult>();

            // Test 1: Simple film lookup
            results.Add(TestQuery("Simple Film Lookup",
                () => _db.Film.AsNoTracking().FirstOrDefault(f => f.FilmId == 1)));

            // Test 2: Genre bitfield filter
            results.Add(TestQuery("Genre Bitfield Filter (Action films)",
                () => _db.Film.AsNoTracking()
                    .Where(f => (f.GenreBitField & 1) == 1)
                    .Take(100)
                    .ToList()));

            // Test 3: Year range with genre filter
            results.Add(TestQuery("Year Range + Genre Filter",
                () => _db.Film.AsNoTracking()
                    .Where(f => f.Year >= 2000 && f.Year <= 2020)
                    .Where(f => (f.GenreBitField & 1) == 1)
                    .OrderByDescending(f => f.Year)
                    .ToList()));

            // Test 4: Film with cast/crew (complex join)
            results.Add(TestQuery("Film Details with Cast/Crew Join",
                () => (from fp in _db.Film_Person.AsNoTracking()
                       where fp.FilmId == 1
                       join p in _db.Person on fp.PersonId equals p.PersonId
                       join j in _db.Job on fp.JobId equals j.JobId
                       select new { fp, p, j })
                      .ToList()));

            // Test 5: Person filmography with job aggregation
            results.Add(TestQuery("Person Filmography with Aggregation",
                () => (from fp in _db.Film_Person.AsNoTracking()
                       where fp.JobId == 1
                       join f in _db.Film.AsNoTracking() on fp.FilmId equals f.FilmId
                       group f by fp.PersonId into g
                       select new
                       {
                           PersonId = g.Key,
                           JobCount = g.Count(),
                           EarliestYear = g.Min(f => f.Year),
                           LatestYear = g.Max(f => f.Year)
                       })
                      .OrderByDescending(x => x.JobCount)
                      .Take(100)
                      .ToList()));

            // Test 6: Collaboration query (most complex)
            results.Add(TestQuery("Person Collaboration Query",
                () => (from fp1 in _db.Film_Person.AsNoTracking()
                       where fp1.PersonId == 1
                       join fp2 in _db.Film_Person.AsNoTracking()
                           on fp1.FilmId equals fp2.FilmId
                       where fp2.PersonId != 1
                       join p in _db.Person.AsNoTracking()
                           on fp2.PersonId equals p.PersonId
                       join j in _db.Job.AsNoTracking()
                           on fp2.JobId equals j.JobId
                       join f in _db.Film.AsNoTracking()
                           on fp2.FilmId equals f.FilmId
                       select new { fp2.PersonId, p.Name, j.JobName, f.Title })
                      .ToList()
                      .GroupBy(x => x.PersonId)
                      .Select(group => new
                      {
                          PersonId = group.Key,
                          Name = group.First().Name,
                          CollaborationCount = group.Count()
                      })
                      .OrderByDescending(x => x.CollaborationCount)
                      .ToList()));

            // Test 7: Genre aggregation with GroupBy
            results.Add(TestQuery("Genre Film Count by Year",
                () => _db.Film.AsNoTracking()
                    .Where(f => (f.GenreBitField & 1) == 1)
                    .GroupBy(f => f.Year)
                    .Select(group => new
                    {
                        Year = group.Key,
                        FilmCount = group.Count()
                    })
                    .OrderBy(fy => fy.Year)
                    .ToList()));

            var model = new QueryPerformanceModel
            {
                DatabaseConnectionString = _db.Database.GetConnectionString() ?? "Not configured",
                Queries = results
            };

            return View("QueryPerformance", model);
        }

        private QueryTestResult TestQuery(string name, Action queryAction)
        {
            var result = new QueryTestResult { QueryName = name };

            try
            {
                // Warm-up run
                queryAction();

                // Measured runs
                var timings = new List<long>();
                for (int i = 0; i < 5; i++)
                {
                    var sw = Stopwatch.StartNew();
                    queryAction();
                    sw.Stop();
                    timings.Add(sw.ElapsedMilliseconds);
                }

                result.Success = true;
                result.AverageMs = timings.Average();
                result.MinMs = timings.Min();
                result.MaxMs = timings.Max();
                result.MedianMs = timings.OrderBy(t => t).ElementAt(timings.Count / 2);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public class QueryPerformanceModel
        {
            public string DatabaseConnectionString { get; set; } = "";
            public List<QueryTestResult> Queries { get; set; } = new();
        }

        public class QueryTestResult
        {
            public string QueryName { get; set; } = "";
            public bool Success { get; set; }
            public double AverageMs { get; set; }
            public long MinMs { get; set; }
            public long MaxMs { get; set; }
            public double MedianMs { get; set; }
            public string? ErrorMessage { get; set; }
        }
    }
}

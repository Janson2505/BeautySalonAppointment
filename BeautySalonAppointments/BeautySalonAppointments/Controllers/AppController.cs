using BeautySalonAppointments.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BeautySalonAppointments.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // Akcja do wyświetlania listy wizyt
        public async Task<IActionResult> Index(string searchString, string clientFilter,
            DateTime? dateFrom, DateTime? dateTo, string serviceFilter, string sortOrder)
        {
            if (_context.Appointments == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Appointments' is null.");
            }

            // Zapytanie podstawowe
            var appointmentsQuery = _context.Appointments
                .Include(a => a.Client)
                .AsQueryable();

            // Zapisz parametry filtrowania do ViewBag do ponownego użycia przy sortowaniu
            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentClientFilter = clientFilter;
            ViewBag.CurrentDateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.CurrentDateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.CurrentServiceFilter = serviceFilter;

            // Parametry sortowania
            ViewBag.DateSortParam = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewBag.NameSortParam = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.ServiceSortParam = sortOrder == "service" ? "service_desc" : "service";

            // Filtrowanie po wyszukiwanej frazie (w usłudze)
            if (!string.IsNullOrEmpty(searchString))
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Service.Contains(searchString));
            }

            // Filtrowanie po kliencie
            if (!string.IsNullOrEmpty(clientFilter) && clientFilter != "0") 
            {
                int clientId = int.Parse(clientFilter);
                appointmentsQuery = appointmentsQuery.Where(a => a.ClientId == clientId);
            }

            // Filtrowanie po dacie od
            if (dateFrom.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Date >= dateFrom.Value);
            }

            // Filtrowanie po dacie do
            if (dateTo.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Date <= dateTo.Value.AddDays(1).AddSeconds(-1));
            }

            // Filtrowanie po usłudze
            if (!string.IsNullOrEmpty(serviceFilter))
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Service == serviceFilter);
            }

            // Sortowanie
            switch (sortOrder)
            {
                case "date_desc":
                    appointmentsQuery = appointmentsQuery.OrderByDescending(a => a.Date);
                    break;
                case "name":
                    appointmentsQuery = appointmentsQuery.OrderBy(a => a.Client.Name);
                    break;
                case "name_desc":
                    appointmentsQuery = appointmentsQuery.OrderByDescending(a => a.Client.Name);
                    break;
                case "service":
                    appointmentsQuery = appointmentsQuery.OrderBy(a => a.Service);
                    break;
                case "service_desc":
                    appointmentsQuery = appointmentsQuery.OrderByDescending(a => a.Service);
                    break;
                default:
                    appointmentsQuery = appointmentsQuery.OrderBy(a => a.Date);
                    break;
            }

 
            var clients = await _context.Clients.ToListAsync();
            ViewBag.Clients = new SelectList(clients, "Id", "Name");


            var services = await _context.Appointments
                .Select(a => a.Service)
                .Distinct()
                .ToListAsync();
            ViewBag.Services = new SelectList(services);

            return View(await appointmentsQuery.ToListAsync());
        }

        // Akcja do dodawania nowej wizyty
        public IActionResult Create(string presetDate)
        {
            var appointment = new Appointment { Duration = 60 }; 

    
            if (!string.IsNullOrEmpty(presetDate))
            {
                try
                {
                    appointment.Date = DateTime.Parse(presetDate);
                }
                catch{ }
            }

            ViewBag.Clients = new SelectList(_context.Clients.ToList(), "Id", "Name");
            return View(appointment);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Appointment appointment)
        {
            ModelState.Remove("Client");

            Console.WriteLine($"Otrzymane dane wizyty: Data={appointment.Date}, Usługa={appointment.Service}, ID Klienta={appointment.ClientId}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Walidacja modelu nie powiodła się z następującymi błędami:");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"- {state.Key}: {error.ErrorMessage}");
                    }
                }

                ViewBag.Clients = _context.Clients.ToList() ?? new List<Client>();
                return View(appointment);
            }

            try
            {
                _context.Appointments.Add(appointment);
                _context.SaveChanges();
                Console.WriteLine($"Pomyślnie zapisano wizytę o ID: {appointment.Id}");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zapisywania wizyty: {ex.Message}");
                ModelState.AddModelError("", "Wystąpił błąd podczas zapisywania wizyty.");
                ViewBag.Clients = _context.Clients.ToList() ?? new List<Client>();
                return View(appointment);
            }
        }


        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.Clients = new SelectList(_context.Clients.ToList(), "Id", "Name", appointment.ClientId);

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Client");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Appointments.Any(a => a.Id == appointment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = new SelectList(_context.Clients.ToList(), "Id", "Name", appointment.ClientId);

            return View(appointment);
        }

        // Akcja do wyświetlania formularza usuwania wizyty
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }
            return View(appointment);
        }

        // Akcja do potwierdzenia usunięcia wizyty
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        public IActionResult Calendar()
        {
            return View();
        }

  
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetCalendarData(DateTime start, DateTime end)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Client)
                .Where(a => a.Date >= start && a.Date <= end)
                .ToListAsync();

            var events = appointments.Select(a => new
            {
                id = a.Id,
                title = $"{a.Service} - {a.Client?.Name ?? "Nieznany klient"}",
                start = a.Date.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = a.Date.AddMinutes(a.Duration).ToString("yyyy-MM-ddTHH:mm:ss"), 
                backgroundColor = GetColorForService(a.Service),
                extendedProps = new
                {
                    clientId = a.ClientId,
                    clientName = a.Client?.Name,
                    service = a.Service,
                    duration = a.Duration 
                }
            });

            return Json(events);
        }

        private string GetColorForService(string service)
        {
            switch (service?.ToLower())
            {
                case var s when s.Contains("makijaż"):
                    return "#ff7eb9";
                case var s2 when s2.Contains("makijaz"):
                    return "#ff7eb9"; 
                case var s3 when s3.Contains("fryzjer"):
                case var s4 when s4.Contains("włosy"):
                case var s5 when s5.Contains("wlosy"):
                    return "#7afcff";
                case var s6 when s6.Contains("manicure"):
                case var s7 when s7.Contains("paznokcie"):
                    return "#6495ED"; 
                case var s8 when s8.Contains("pedicure"):
                    return "#FF7F50"; 
                case var s9 when s9.Contains("masaż"):
                case var s10 when s10.Contains("masaz"):
                    return "#7effcc"; 
                default:
                    return "#a0c4ff"; 
            }
        }
    }
}
using BeautySalonAppointments.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautySalonAppointments.Controllers
{
    public class ClientsController : Controller
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string sortOrder, string searchString)
        {
            // Parametry sortowania
            ViewBag.NameSortParam = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.PhoneSortParam = sortOrder == "Phone" ? "phone_desc" : "Phone";
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentSearchString = searchString;

            var clients = from c in _context.Clients
                          select c;

            // Filtrowanie
            if (!String.IsNullOrEmpty(searchString))
            {
                clients = clients.Where(c =>
                    c.Name.Contains(searchString) ||
                    c.PhoneNumber.Contains(searchString));
            }

            // Sortowanie
            switch (sortOrder)
            {
                case "name_desc":
                    clients = clients.OrderByDescending(c => c.Name);
                    break;
                case "Phone":
                    clients = clients.OrderBy(c => c.PhoneNumber);
                    break;
                case "phone_desc":
                    clients = clients.OrderByDescending(c => c.PhoneNumber);
                    break;
                default:
                    clients = clients.OrderBy(c => c.Name);
                    break;
            }

            var appointmentCounts = _context.Appointments
                .GroupBy(a => a.ClientId)
                .Select(g => new { ClientId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ClientId, x => x.Count);
            ViewBag.AppointmentCounts = appointmentCounts;

            return View(await clients.ToListAsync());
        }


        public IActionResult Create()
        {
            return View();
        }

        // Obsługa dodawania klienta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Client client)
        {
            if (ModelState.IsValid)
            {
                _context.Clients.Add(client);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // Wyświetlenie formularza edytowania klienta
        public IActionResult Edit(int id)
        {
            var client = _context.Clients.Find(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }

        // Obsługa edytowania klienta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Client client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(client);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // Usunięcie klienta
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var client = _context.Clients.Find(id);
            _context.Clients.Remove(client);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // Wyświetlenie formularza usuwania klienta
        public IActionResult Delete(int id)
        {
            var client = _context.Clients.Find(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }
    }

}

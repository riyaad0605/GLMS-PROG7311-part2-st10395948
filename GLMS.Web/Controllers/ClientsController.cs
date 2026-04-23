using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GLMS.Web.Data;
using GLMS.Web.Models;

namespace GLMS.Web.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly GlmsDbContext _context;

        public ClientsController(GlmsDbContext context)
        {
            _context = context;
        }

        // Both roles can view clients
        public async Task<IActionResult> Index()
        {
            return View(await _context.Clients.Include(c => c.Contracts).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clients
                .Include(c => c.Contracts)
                .FirstOrDefaultAsync(c => c.ClientId == id);
            if (client == null) return NotFound();
            return View(client);
        }

        // Admin only: Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Name,ContactDetails,Region")] Client client)
        {
            if (ModelState.IsValid)
            {
                _context.Add(client);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Client '{client.Name}' created.";
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // Admin only: Edit
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ClientId,Name,ContactDetails,Region")] Client client)
        {
            if (id != client.ClientId) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Client '{client.Name}' updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Clients.AnyAsync(c => c.ClientId == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // Admin only: Delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == id);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Client '{client.Name}' deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

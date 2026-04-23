using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GLMS.Web.Data;
using GLMS.Web.Models;
using GLMS.Web.Services;

namespace GLMS.Web.Controllers
{
    [Authorize]
    public class ServiceRequestsController : Controller
    {
        private readonly GlmsDbContext _context;
        private readonly ICurrencyService _currencyService;
        private readonly IEmailService _emailService;

        public ServiceRequestsController(
            GlmsDbContext context,
            ICurrencyService currencyService,
            IEmailService emailService)
        {
            _context = context;
            _currencyService = currencyService;
            _emailService = emailService;
        }

        // Both roles: view
        public async Task<IActionResult> Index()
        {
            return View(await _context.ServiceRequests
                .Include(sr => sr.Contract)
                    .ThenInclude(c => c!.Client)
                .OrderByDescending(sr => sr.DateCreated)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var req = await _context.ServiceRequests
                .Include(sr => sr.Contract)
                    .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(sr => sr.ServiceRequestId == id);
            if (req == null) return NotFound();
            return View(req);
        }

        // Both roles: create
        public async Task<IActionResult> Create()
        {
            await PopulateContractsDropdown();
            var vm = new ServiceRequestCreateViewModel
            {
                ExchangeRate = await _currencyService.GetUsdToZarRateAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequestCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateContractsDropdown(vm.ContractId);
                return View(vm);
            }

            // Load the contract and validate workflow rule
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.ContractId == vm.ContractId);

            if (contract == null)
            {
                ModelState.AddModelError("ContractId", "Contract not found.");
                await PopulateContractsDropdown(vm.ContractId);
                return View(vm);
            }

            // WORKFLOW: block Expired and OnHold contracts
            if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
            {
                ModelState.AddModelError("ContractId",
                    $"Cannot raise a request against a contract with status '{contract.Status}'. " +
                    "Only Draft or Active contracts are valid.");
                await PopulateContractsDropdown(vm.ContractId);
                return View(vm);
            }

            // Currency conversion
            var rate = await _currencyService.GetUsdToZarRateAsync();
            var costZar = _currencyService.ConvertUsdToZar(vm.CostUsd, rate);

            var request = new ServiceRequest
            {
                ContractId = vm.ContractId,
                Description = vm.Description,
                CostUsd = vm.CostUsd,
                CostZar = costZar,
                Status = "Pending",
                DateCreated = DateTime.Now
            };

            _context.Add(request);
            await _context.SaveChangesAsync();

            // Email notification
            if (contract.Client != null)
            {
                var contactEmail = contract.Client.ContactDetails
                    .Split('|', StringSplitOptions.TrimEntries)
                    .FirstOrDefault(s => s.Contains('@')) ?? string.Empty;

                await _emailService.SendServiceRequestCreatedAsync(
                    contactEmail,
                    contract.Client.Name,
                    request.ServiceRequestId,
                    request.CostUsd,
                    request.CostZar);
            }

            TempData["SuccessMessage"] =
                $"Service request #{request.ServiceRequestId} created. " +
                $"USD {vm.CostUsd:N2} → ZAR {costZar:N2} (rate: {rate:N4}).";

            return RedirectToAction(nameof(Index));
        }

        // Admin only: delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var req = await _context.ServiceRequests
                .Include(sr => sr.Contract)
                    .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(sr => sr.ServiceRequestId == id);
            if (req == null) return NotFound();
            return View(req);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var req = await _context.ServiceRequests.FindAsync(id);
            if (req != null)
            {
                _context.ServiceRequests.Remove(req);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Service request deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateContractsDropdown(int? selectedId = null)
        {
            var valid = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status != ContractStatus.Expired && c.Status != ContractStatus.OnHold)
                .ToListAsync();

            ViewBag.Contracts = new SelectList(
                valid.Select(c => new
                {
                    c.ContractId,
                    Display = $"#{c.ContractId} — {c.Client?.Name} ({c.Status})"
                }),
                "ContractId", "Display", selectedId);

            ViewBag.ExchangeRate = await _currencyService.GetUsdToZarRateAsync();
        }
    }
}

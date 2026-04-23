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
    public class ContractsController : Controller
    {
        private readonly GlmsDbContext _context;
        private readonly IFileService _fileService;
        private readonly IEmailService _emailService;

        public ContractsController(
            GlmsDbContext context,
            IFileService fileService,
            IEmailService emailService)
        {
            _context = context;
            _fileService = fileService;
            _emailService = emailService;
        }

        // GET: Contracts — both roles
        public async Task<IActionResult> Index()
        {
            return View(await _context.Contracts
                .Include(c => c.Client)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync());
        }

        // GET: Contracts/Search — LINQ filter, both roles
        [HttpGet]
        public async Task<IActionResult> Search(ContractSearchViewModel model)
        {
            var query = _context.Contracts.Include(c => c.Client).AsQueryable();

            if (model.StartDateFrom.HasValue)
                query = query.Where(c => c.StartDate >= model.StartDateFrom.Value);

            if (model.StartDateTo.HasValue)
                query = query.Where(c => c.StartDate <= model.StartDateTo.Value);

            if (model.StatusFilter.HasValue)
                query = query.Where(c => c.Status == model.StatusFilter.Value);

            model.Results = await query.OrderByDescending(c => c.StartDate).ToListAsync();
            return View(model);
        }

        // GET: Contracts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.ContractId == id);
            if (contract == null) return NotFound();
            return View(contract);
        }

        // Admin only: Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateClientsDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [Bind("ClientId,StartDate,EndDate,Status,ServiceLevel")] Contract contract,
            IFormFile? signedAgreement)
        {
            if (ModelState.IsValid)
            {
                _context.Add(contract);
                await _context.SaveChangesAsync();

                if (signedAgreement != null)
                {
                    if (!_fileService.ValidateFile(signedAgreement, out var error))
                    {
                        TempData["ErrorMessage"] = error;
                    }
                    else
                    {
                        contract.SignedAgreementPath = await _fileService
                            .SaveSignedAgreementAsync(signedAgreement, contract.ContractId);
                        _context.Update(contract);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Contract created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateClientsDropdown(contract.ClientId);
            return View(contract);
        }

        // Admin only: Edit — triggers email on status change
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return NotFound();
            await PopulateClientsDropdown(contract.ClientId);
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id,
            [Bind("ContractId,ClientId,StartDate,EndDate,Status,ServiceLevel,SignedAgreementPath")] Contract contract)
        {
            if (id != contract.ContractId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Detect status change to trigger email notification
                    var original = await _context.Contracts
                        .AsNoTracking()
                        .Include(c => c.Client)
                        .FirstOrDefaultAsync(c => c.ContractId == id);

                    _context.Update(contract);
                    await _context.SaveChangesAsync();

                    if (original != null && original.Status != contract.Status)
                    {
                        var client = original.Client
                            ?? await _context.Clients.FindAsync(contract.ClientId);

                        if (client != null)
                        {
                            await _emailService.SendContractStatusChangedAsync(
                                client.ContactDetails.Split('|')[0].Trim(),
                                client.Name,
                                contract.ContractId,
                                contract.Status.ToString());
                        }
                    }

                    TempData["SuccessMessage"] = "Contract updated. Email notification sent if status changed.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Contracts.AnyAsync(c => c.ContractId == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateClientsDropdown(contract.ClientId);
            return View(contract);
        }

        // POST: Upload agreement — Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadAgreement(int id, IFormFile signedAgreement)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return NotFound();

            if (!_fileService.ValidateFile(signedAgreement, out var error))
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(Details), new { id });
            }

            contract.SignedAgreementPath = await _fileService
                .SaveSignedAgreementAsync(signedAgreement, contract.ContractId);

            _context.Update(contract);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Signed agreement uploaded.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Download agreement — both roles
        public async Task<IActionResult> DownloadAgreement(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null || string.IsNullOrEmpty(contract.SignedAgreementPath))
                return NotFound();

            var filePath = _fileService.GetFilePath(contract.SignedAgreementPath);
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found on server.");

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "application/pdf", $"SignedAgreement_Contract_{id}.pdf");
        }

        // Admin only: Delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.ContractId == id);
            if (contract == null) return NotFound();
            return View(contract);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
            {
                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Contract deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateClientsDropdown(int? selectedId = null)
        {
            ViewBag.Clients = new SelectList(
                await _context.Clients.ToListAsync(), "ClientId", "Name", selectedId);
        }
    }
}

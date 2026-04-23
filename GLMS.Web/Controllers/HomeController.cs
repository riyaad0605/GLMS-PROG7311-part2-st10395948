using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GLMS.Web.Data;
using GLMS.Web.Models;

namespace GLMS.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly GlmsDbContext _context;

        public HomeController(GlmsDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ClientCount = await _context.Clients.CountAsync();
            ViewBag.ContractCount = await _context.Contracts.CountAsync();
            ViewBag.RequestCount = await _context.ServiceRequests.CountAsync();
            ViewBag.ActiveContracts = await _context.Contracts
                .CountAsync(c => c.Status == ContractStatus.Active);
            ViewBag.ExpiredContracts = await _context.Contracts
                .CountAsync(c => c.Status == ContractStatus.Expired);

            return View();
        }
    }
}

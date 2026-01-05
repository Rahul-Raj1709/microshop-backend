using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProducerAPI.Repositories;
using System.Security.Claims;

namespace ProducerAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
public class DashboardController : ControllerBase
{
    private readonly DashboardRepository _repo;

    public DashboardController(DashboardRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");

        // Logic: If SuperAdmin, pass null (Fetch All). If Admin, pass userId (Fetch Own).
        int? sellerIdFilter = (role == "SuperAdmin") ? null : userId;

        var stats = await _repo.GetStatsAsync(sellerIdFilter);
        return Ok(stats);
    }
}
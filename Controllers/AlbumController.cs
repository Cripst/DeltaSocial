using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeltaSocial.Attributes;

namespace DeltaSocial.Controllers
{
    [Authorize]
    [VisitorAccess]
    public class AlbumController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AlbumController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ... existing code ...
    }
} 
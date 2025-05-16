using Microsoft.AspNetCore.Mvc;

namespace DeltaSocial.Controllers // asigură-te că numele proiectului tău e corect
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

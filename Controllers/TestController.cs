using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace DeltaSocial.Controllers
{
    public class TestController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public TestController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password)
        {
            var client = _clientFactory.CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(new { email, password }),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("https://localhost:7236/api/auth/register", content);
            var result = await response.Content.ReadAsStringAsync();

            ViewBag.Message = result;
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var client = _clientFactory.CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(new { email, password }),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("https://localhost:7236/api/auth/login", content);
            var result = await response.Content.ReadAsStringAsync();

            ViewBag.Message = result;
            return View("Index");
        }
    }
}

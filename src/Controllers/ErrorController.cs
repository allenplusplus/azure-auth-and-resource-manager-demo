using Microsoft.AspNetCore.Mvc;

namespace azure_auth_and_arm_demo.Controllers
{
    public class ErrorController : Controller
    {
        [Route("/error")]
        public IActionResult Error()
        {
            return View("Views/Error/Error.cshtml");
        }
    }
}
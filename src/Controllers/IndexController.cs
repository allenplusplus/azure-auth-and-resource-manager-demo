using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using azure_auth_and_arm_demo.Models;
using Microsoft.Extensions.Configuration;
using azure_auth_and_arm_demo.Caches;

namespace azure_auth_and_arm_demo.Controllers
{
    public class IndexController : Controller
    {
        private readonly ILogger<IndexController> _logger;
        private readonly IConfiguration _configuration;
        private readonly SessionCache _sessionCache;
        private readonly UrlGenerator _urlGenerator;

        public IndexController(ILogger<IndexController> logger, IConfiguration configuration,
            SessionCache sessionCache, UrlGenerator urlGenerator)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionCache = sessionCache;
            _urlGenerator = urlGenerator;
        }

        [Route("/")]
        public IActionResult Index()
        {
            Session session = _sessionCache.GetSessionIfAuthenticated(Request, _logger);
            if (session == null)
            {
                _logger.LogInformation("not logged in");
                ViewData["LoggedIn"] = "false";
            }
            else
            {
                _logger.LogInformation("logged in");
                ViewData["LoggedIn"] = "true";
            }

            ViewData["SignoutUrl"] = _urlGenerator.GenerateSignoutUrl();
            return View("Views/Index/Index.cshtml");
        }
    }
}

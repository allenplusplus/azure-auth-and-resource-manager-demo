using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using azure_auth_and_arm_demo.Caches;
using azure_auth_and_arm_demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace azure_auth_and_arm_demo.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ILogger<SubscriptionController> _logger;
        private readonly SessionCache _sessionCache;
        private readonly HttpClient _httpClient;
        private readonly UrlGenerator _urlGenerator;

        public SubscriptionController(ILogger<SubscriptionController> logger, SessionCache sessionCache,
            IHttpClientFactory _httpClientFactory, UrlGenerator urlGenerator)
        {
            _logger = logger;
            _sessionCache = sessionCache;
            _httpClient = _httpClientFactory.CreateClient();
            _urlGenerator = urlGenerator;
        }

        [Route("/subscriptions")]
        public async Task<IActionResult> Subscriptions()
        {
            Session session = _sessionCache.GetSessionIfAuthenticated(Request, _logger);
            if (session == null)
            {
                return Unauthorized("unauthorized");
            }

            string getSubscriptionsContent;
            using (HttpRequestMessage getSubscriptionsRequest = new HttpRequestMessage(
                HttpMethod.Get, _urlGenerator.GenerateSubscriptionsUrl()))
            {
                getSubscriptionsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                    session.Token.AccessToken);

                using (HttpResponseMessage getSubscriptionsResponse = await _httpClient.SendAsync(
                    getSubscriptionsRequest))
                {
                    _logger.LogInformation("[subscriptions] getSubscriptions statusCode="
                        + getSubscriptionsResponse.StatusCode.ToString());
                    
                    if (getSubscriptionsResponse.StatusCode != HttpStatusCode.OK)
                    {
                        return StatusCode(500);
                    }

                    getSubscriptionsContent = await getSubscriptionsResponse.Content.ReadAsStringAsync();

                    _logger.LogInformation("[subscriptions] getSubscriptions content=" + getSubscriptionsContent);
                }                
            }

            SubscriptionsModel subscriptions = JsonSerializer.Deserialize<SubscriptionsModel>(getSubscriptionsContent);
            return Ok(subscriptions);
        }
    }
}
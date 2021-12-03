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
    public class ResourceGroupController : Controller
    {
        private readonly ILogger<ResourceGroupController> _logger;
        private readonly SessionCache _sessionCache;
        private readonly HttpClient _httpClient;
        private readonly UrlGenerator _urlGenerator;

        public ResourceGroupController(ILogger<ResourceGroupController> logger, SessionCache sessionCache,
            IHttpClientFactory _httpClientFactory, UrlGenerator urlGenerator)
        {
            _logger = logger;
            _sessionCache = sessionCache;
            _httpClient = _httpClientFactory.CreateClient();
            _urlGenerator = urlGenerator;
        }

        [Route("/resourceGroups")]
        public async Task<IActionResult> resourceGroups(string subscriptionId)
        {
            Session session = _sessionCache.GetSessionIfAuthenticated(Request, _logger);
            if (session == null)
            {
                return Unauthorized("unauthorized");
            }

            string getResourceGroupsContent;
            using (HttpRequestMessage getResourceGroupsRequest = new HttpRequestMessage(
                HttpMethod.Get, _urlGenerator.GenerateResourceGroupsUrl(subscriptionId)))
            {
                getResourceGroupsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                    session.Token.AccessToken);

                using (HttpResponseMessage getResourceGroupsResponse = await _httpClient.SendAsync(
                    getResourceGroupsRequest))
                {
                    _logger.LogInformation("[resourceGroups] getResourceGroups statusCode="
                        + getResourceGroupsResponse.StatusCode.ToString());

                    if (getResourceGroupsResponse.StatusCode != HttpStatusCode.OK)
                    {
                        return StatusCode(500);
                    }

                    getResourceGroupsContent = await getResourceGroupsResponse.Content.ReadAsStringAsync();

                    _logger.LogInformation("[resourceGroups] getResourceGroups content=" + getResourceGroupsContent);
                }                
            }

            ResourceGroupsModel resourceGroups = JsonSerializer.Deserialize<ResourceGroupsModel>(
                getResourceGroupsContent);
            return Ok(resourceGroups);
        }
    }
}
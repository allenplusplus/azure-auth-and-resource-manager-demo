using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System;
using azure_auth_and_arm_demo.Models;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using System.Text;
using azure_auth_and_arm_demo.Caches;

namespace azure_auth_and_arm_demo.Controllers
{
    public class DeployController : Controller
    {
        private readonly ILogger<DeployController> _logger;
        private readonly SessionCache _sessionCache;
        private readonly HttpClient _httpClient;
        private readonly UrlGenerator _urlGenerator;

        public DeployController(ILogger<DeployController> logger, SessionCache sessionCache,
            IHttpClientFactory _httpClientFactory, UrlGenerator urlGenerator)
        {
            _logger = logger;
            _sessionCache = sessionCache;
            _httpClient = _httpClientFactory.CreateClient();
            _urlGenerator = urlGenerator;
        }

        [Route("/deploy")]
        public IActionResult Deploy()
        {
            Session session = _sessionCache.GetSessionIfAuthenticated(Request, _logger);
            if (session == null)
            {
                return Unauthorized("unauthorized");
            }

            return View("Views/Deploy/Deploy.cshtml");
        }

        [Route("/deploy/submit")]
        public async Task<IActionResult> Submit(string subscriptionId, string resourceGroupName, string instanceName)
        {
            Session session = _sessionCache.GetSessionIfAuthenticated(Request, _logger);
            if (session == null)
            {
                return Unauthorized("unauthorized");
            }

            if (subscriptionId == null || subscriptionId.Length == 0 || resourceGroupName == null
                || resourceGroupName.Length == 0 || instanceName == null || instanceName.Length == 0)
            {
                _logger.LogInformation("invalid query parameters");
                return BadRequest("bad request");
            }

            string deploymentName = "azure_auth_and_arm_demo_" + Guid.NewGuid().ToString();
            string putDeploymentContent;
            using (HttpRequestMessage putDeploymentRequest = new HttpRequestMessage(
                HttpMethod.Put, _urlGenerator.GenerateGetOrCreateOrUpdateDeploymentUrl(subscriptionId,
                resourceGroupName, deploymentName)))
            {
                string template = $@"{{
                    ""properties"":
                    {{
                        ""template"":
                        {{
                            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                            ""contentVersion"": ""1.0.0.0"",
                            ""resources"":
                            [
                                {{
                                    ""type"": ""Microsoft.Communication/CommunicationServices"",
                                    ""apiVersion"": ""2020-08-20"",
                                    ""name"": ""{instanceName}"",
                                    ""location"": ""global"",
                                    ""properties"":
                                    {{
                                        ""dataLocation"": ""United States""
                                    }}
                                }}
                            ]
                        }},
                        ""mode"": ""Incremental""
                    }}
                }}";
                putDeploymentRequest.Content = new StringContent(template, Encoding.UTF8, "application/json");
                putDeploymentRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                    session.Token.AccessToken);
                _logger.LogInformation("template=" + template);

                using (HttpResponseMessage putDeploymentResponse = await _httpClient.SendAsync(putDeploymentRequest))
                {
                    _logger.LogInformation("putDeployment statusCode="
                        + putDeploymentResponse.StatusCode.ToString());

                    if (putDeploymentResponse.StatusCode != HttpStatusCode.OK
                        && putDeploymentResponse.StatusCode != HttpStatusCode.Created)
                    {
                        return StatusCode(500, "failed to create deployment");
                    }

                    putDeploymentContent = await putDeploymentResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("putDeployment content=" + putDeploymentContent);
                }
            }

            return Redirect("/deploy/post?subscriptionId=" + subscriptionId + "&resourceGroupName=" + resourceGroupName
                + "&deploymentName=" + deploymentName);
        }

        [Route("/deploy/post")]
        public IActionResult Post(string subscriptionId, string resourceGroupName, string deploymentName)
        {
            Session session = _sessionCache.GetSessionIfAuthenticated(Request, _logger);
            if (session == null)
            {
                return Unauthorized("unauthorized");
            }

            if (subscriptionId == null || subscriptionId.Length == 0 || resourceGroupName == null
                || resourceGroupName.Length == 0 || deploymentName == null || deploymentName.Length == 0)
            {
                _logger.LogInformation("invalid query parameters");
                return BadRequest("bad request");
            }
            
            ViewData["SubscriptionId"] = subscriptionId;
            ViewData["ResourceGroupName"] = resourceGroupName;
            ViewData["DeploymentName"] = deploymentName;
            return View("Views/Deploy/Post.cshtml");
        }

        [Route("/deploy/state")]
        public async Task<IActionResult> State(string subscriptionId, string resourceGroupName, string deploymentName)
        {
            Session session = _sessionCache.GetSessionIfAuthenticated(Request, _logger);
            if (session == null)
            {
                return Unauthorized("unauthorized");
            }

            if (subscriptionId == null || subscriptionId.Length == 0 || resourceGroupName == null
                || resourceGroupName.Length == 0 || deploymentName == null || deploymentName.Length == 0)
            {
                _logger.LogInformation("invalid query parameters");
                return BadRequest("bad request");
            }

            string getDeploymentContent;
            using (HttpRequestMessage getDeploymentRequest = new HttpRequestMessage(
                HttpMethod.Get, _urlGenerator.GenerateGetOrCreateOrUpdateDeploymentUrl(subscriptionId, resourceGroupName,
                deploymentName)))
            {
                getDeploymentRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
                    session.Token.AccessToken);

                using (HttpResponseMessage getDeploymentResponse = await _httpClient.SendAsync(getDeploymentRequest))
                {
                    _logger.LogInformation("getDeployment statusCode="
                        + getDeploymentResponse.StatusCode.ToString());

                    if (getDeploymentResponse.StatusCode != HttpStatusCode.OK)
                    {
                        return StatusCode(500);
                    }

                    getDeploymentContent = await getDeploymentResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("getDeployment content=" + getDeploymentContent);
                }
            }

            DeploymentModel deployment = JsonSerializer.Deserialize<DeploymentModel>(getDeploymentContent);
            return Ok(deployment);
        }
    }
}

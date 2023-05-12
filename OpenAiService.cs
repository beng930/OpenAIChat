using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SmartFormat;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OpenAIRequestExample
{
    public class OpenAiService
    {
        private readonly string? _endpoint;
        private readonly string? _apiKey;
        private readonly string? _deployment;
        private readonly ILogger _logger;

        public OpenAiService(ILogger<OpenAiService> logger)
        {

            _logger = logger;
            _endpoint = Constants.endpoint;
            _apiKey = Constants.apiKey;
            _deployment = Constants.deployment;

        }

        public async Task<string> AskWithFormatAsync(PromptTemplate promptTemplate, object templateData)
        {
            try
            {
                HttpClient client = new();
                var requestBody = new
                {
                    prompt = Smart.Format(promptTemplate.Template, templateData),
                    promptTemplate.TunningParameters.max_tokens,
                    promptTemplate.TunningParameters.temperature,
                    promptTemplate.TunningParameters.top_p,
                    promptTemplate.TunningParameters.best_of,
                    promptTemplate.TunningParameters.presence_penalty,
                    promptTemplate.TunningParameters.frequency_penalty,
                };
                var request = new HttpRequestMessage(new HttpMethod("POST"), $"{_endpoint}/openai/deployments/{_deployment}/completions?api-version=2022-12-01");
                request.Headers.TryAddWithoutValidation("api-key", _apiKey);
                request.Content = JsonContent.Create(requestBody);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                var response = await client.SendAsync(request);
                var promptResponse = await ParseResponseAsync(response);
                return promptResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Constants.TooManyRequestsErrorMessage;
            }
        }

        private static async Task<string?> ParseResponseAsync(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            JObject joResponse = JObject.Parse(responseBody);
            var promptResponse = (string?)joResponse?["choices"]?[0]?["text"];
            return promptResponse;
        }
    }
}

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GrammarLlama
{
    /// <summary>
    /// Provides a client for interacting with the Ollama API.
    /// </summary>
    public class OllamaApiClient
    {
        private readonly HttpClient _httpClient;
        private string _apiEndpoint;
        private string _model;
        private string _system;
        private const int TimeoutSeconds = 300; // 5 minutes timeout

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaApiClient"/> class.
        /// </summary>
        public OllamaApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
        }

        /// <summary>
        /// Updates the client settings with new values.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint URL.</param>
        /// <param name="model">The model to use for requests.</param>
        /// <param name="system">The system prompt to use (optional).</param>
        /// <exception cref="ArgumentException">Thrown when apiEndpoint or model is null or empty.</exception>
        public void UpdateSettings(string apiEndpoint, string model, string system)
        {
            if (string.IsNullOrWhiteSpace(apiEndpoint))
                throw new ArgumentException("API endpoint cannot be null or empty.", nameof(apiEndpoint));
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model cannot be null or empty.", nameof(model));

            _apiEndpoint = apiEndpoint;
            _model = model;
            _system = system;
        }

        /// <summary>
        /// Sends a message to the Ollama API and returns the response.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The response from the API.</returns>
        /// <exception cref="ArgumentException">Thrown when message is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when API endpoint or model is not set.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during the API request or response parsing.</exception>
        public async Task<string> SendMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));

            if (string.IsNullOrWhiteSpace(_apiEndpoint) || string.IsNullOrWhiteSpace(_model))
                throw new InvalidOperationException("API endpoint and model must be set before sending a message.");

            try
            {
                var requestBody = new
                {
                    model = _model,
                    prompt = message,
                    stream = false,
                    options = new
                    {
                        num_predict = -1 // Set to -1 for unlimited token generation
                    },
                    system = _system
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiEndpoint, content);

                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    throw new HttpRequestException("The server returned a 500 Internal Server Error. This might be due to a timeout or server-side issue.");
                }

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(responseBody);

                if (jsonResponse["response"] == null)
                    throw new InvalidOperationException("The API response does not contain a 'response' field.");

                return jsonResponse["response"].ToString();
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"The request timed out after {TimeoutSeconds} seconds.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Failed to communicate with the API.", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse the API response.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while processing the message.", ex);
            }
        }
    }
}
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayWeb.Models.Api;

namespace RaceDayWeb.Services
{
    public class ApiResult
    {
        public bool Success { get; init; }
        public string Error { get; init; } = string.Empty;
        public HttpStatusCode? Status { get; init; }
    }

    public class ApiResult<T> : ApiResult
    {
        public T? Data { get; init; }
    }

    // Every call to the Part 2 API goes through this class.
    // It adds the logged in user's API cookie (kept in their session) and turns errors into friendly messages.
    public class ApiClient
    {
        private const string CookieKey = "ApiCookie";
        private const string ApiCookieName = "RaceDay.Session";

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;

        public ApiClient(HttpClient http, IHttpContextAccessor accessor)
        {
            _http = http;
            _accessor = accessor;
        }

        private ISession Session => _accessor.HttpContext!.Session;

        // ---------- login and logout

        public async Task<ApiResult> LoginAsync(string email, string password)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
                {
                    Content = JsonContent.Create(new { email, password })
                };
                using var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var message = response.StatusCode == HttpStatusCode.Unauthorized
                        ? "Invalid email or password."
                        : await FriendlyMessageAsync(response);
                    return new ApiResult { Success = false, Error = message, Status = response.StatusCode };
                }

                // the API answers with a cookie, we keep it in this user's session and send it back on later calls
                if (response.Headers.TryGetValues("Set-Cookie", out var values))
                {
                    var cookie = values
                        .Select(v => v.Split(';')[0])
                        .FirstOrDefault(v => v.StartsWith(ApiCookieName + "="));

                    if (cookie != null)
                    {
                        Session.SetString(CookieKey, cookie);
                        return new ApiResult { Success = true, Status = response.StatusCode };
                    }
                }

                return new ApiResult { Success = false, Error = "We could not start your session. Please try again." };
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult { Success = false, Error = ServiceDown };
            }
        }

        public async Task LogoutAsync()
        {
            await SendAsync<object>(Build(HttpMethod.Post, "api/auth/logout"), readBody: false);
        }

        // ---------- normal calls

        public Task<ApiResult<T>> GetAsync<T>(string url)
        {
            return SendAsync<T>(Build(HttpMethod.Get, url));
        }

        public Task<ApiResult<T>> PostAsync<T>(string url, object? body)
        {
            var content = body == null ? null : JsonContent.Create(body);
            return SendAsync<T>(Build(HttpMethod.Post, url, content));
        }

        public Task<ApiResult<T>> PutAsync<T>(string url, object body)
        {
            return SendAsync<T>(Build(HttpMethod.Put, url, JsonContent.Create(body)));
        }

        public Task<ApiResult<object>> DeleteAsync(string url)
        {
            return SendAsync<object>(Build(HttpMethod.Delete, url), readBody: false);
        }

        // sends an image to the API, the API is the one that saves it in Azure Blob Storage
        public async Task<ApiResult<ImageUploadResponseDto>> UploadImageAsync(string url, IFormFile file)
        {
            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.FileName);

            return await SendAsync<ImageUploadResponseDto>(Build(HttpMethod.Post, url, form));
        }

        // ---------- internals

        private const string ServiceDown = "The RaceDay service is not available right now. Please try again in a moment.";

        private HttpRequestMessage Build(HttpMethod method, string url, HttpContent? content = null)
        {
            var request = new HttpRequestMessage(method, url) { Content = content };

            var cookie = Session.GetString(CookieKey);
            if (!string.IsNullOrEmpty(cookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookie);
            }

            return request;
        }

        private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, bool readBody = true)
        {
            try
            {
                using (request)
                using (var response = await _http.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        T? data = default;
                        if (readBody)
                        {
                            var text = await response.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                data = JsonSerializer.Deserialize<T>(text, JsonOptions);
                            }
                        }

                        return new ApiResult<T> { Success = true, Data = data, Status = response.StatusCode };
                    }

                    // the API cookie is no longer valid, so forget it
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Session.Remove(CookieKey);
                    }

                    return new ApiResult<T>
                    {
                        Success = false,
                        Error = await FriendlyMessageAsync(response),
                        Status = response.StatusCode
                    };
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new ApiResult<T> { Success = false, Error = ServiceDown };
            }
            catch (JsonException)
            {
                return new ApiResult<T> { Success = false, Error = "We received an answer we could not read. Please try again." };
            }
        }

        // never show raw errors, only messages that are safe and useful
        private static async Task<string> FriendlyMessageAsync(HttpResponseMessage response)
        {
            var body = (await response.Content.ReadAsStringAsync()).Trim();

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return "Your session has ended. Please log in again.";
                case HttpStatusCode.Forbidden:
                    return "You do not have permission to do that.";
                case HttpStatusCode.NotFound:
                    return LooksLikePlainText(body) ? body : "We could not find what you were looking for.";
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Conflict:
                    if (LooksLikePlainText(body))
                    {
                        return body;
                    }
                    return ReadValidationErrors(body) ?? "Some of the details are not valid. Please check them and try again.";
                default:
                    return "Something went wrong on our side. Please try again.";
            }
        }

        // the API sends its own short messages as plain text, for example "This category is full."
        private static bool LooksLikePlainText(string body)
        {
            return body.Length > 0 && body.Length < 200 && !body.StartsWith("{") && !body.StartsWith("<");
        }

        // the API sends validation problems as JSON with an "errors" list
        private static string? ReadValidationErrors(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();
                    foreach (var field in errors.EnumerateObject())
                    {
                        foreach (var message in field.Value.EnumerateArray())
                        {
                            var text = message.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                messages.Add(text);
                            }
                        }
                    }

                    if (messages.Count > 0)
                    {
                        return string.Join(" ", messages);
                    }
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }
    }
}

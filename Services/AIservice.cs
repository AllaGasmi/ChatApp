using System.Net.Http;
using System.Net.Http.Json;

public class AiService : IAIService
{
    private readonly HttpClient _http;

    public AiService(HttpClient http)
    {
        _http = http;
    }

    public string GetAIResponse(string message)
    {
        var response = _http.PostAsJsonAsync("http://127.0.0.1:8000/chat", new { message })
                            .GetAwaiter().GetResult();

        response.EnsureSuccessStatusCode();

        var result = response.Content.ReadFromJsonAsync<AiResponse>()
                            .GetAwaiter().GetResult();

        return result?.reply ?? "Sorry, I didn't understand that.";
    }

    private class AiResponse
    {
        public string reply { get; set; } = "";
    }
}


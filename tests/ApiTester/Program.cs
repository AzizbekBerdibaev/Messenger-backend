using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace ApiTester;

public class Program
{
    private static readonly string RedisConnectionString = "bursting-ibex-91927.upstash.io:6379,password=gQAAAAAAAWcXAAIgcDI1NTIwYzVmZGFlNmY0OWRmYmQ3YjI4NzNhNTBmMWNlOA,ssl=True,abortConnect=False";
    private static readonly string ApiBaseUrl = "http://localhost:5279";

    // Reusable HttpClients
    private static HttpClient _clientA = new();
    private static HttpClient _clientB = new();

    private static ConnectionMultiplexer _redis;
    private static IDatabase _db;

    public static async Task Main()
    {
        Console.WriteLine("Starting API E2E tests...");

        // Setup Redis
        _redis = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);
        _db = _redis.GetDatabase();

        // Setup HttpClients
        _clientA.BaseAddress = new Uri(ApiBaseUrl);
        _clientB.BaseAddress = new Uri(ApiBaseUrl);

        try
        {
            await RunTests();
            Console.WriteLine("✅ All tests passed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private static async Task RunTests()
    {
        string phoneA = "+79990001111";
        string phoneB = "+79990002222";

        // 1. Auth Flow
        Console.WriteLine("1. Testing Auth...");
        var userAId = await Authenticate(_clientA, phoneA);
        var userBId = await Authenticate(_clientB, phoneB);

        // 2. Profile
        Console.WriteLine("2. Testing Profile...");
        await _clientA.PutAsJsonAsync("/api/profile", new { firstName = "User", lastName = "A", bio = "Testing A", avatarUrl = "http://example.com/urla.jpg" }).EnsureSuccess();
        await _clientB.PutAsJsonAsync("/api/profile", new { firstName = "User", lastName = "B", bio = "Testing B", avatarUrl = "http://example.com/urlb.jpg" }).EnsureSuccess();
        
        var profileResponse = await _clientA.GetAsync("/api/profile");
        profileResponse.EnsureSuccessStatusCode();

        // 3. Contacts
        Console.WriteLine("3. Testing Contacts...");
        await _clientA.PostAsJsonAsync("/api/contacts", new { phoneNumber = phoneB, firstName = "Test", lastName = "B" }).EnsureSuccess();
        var contactsResp = await _clientA.GetAsync("/api/contacts");
        contactsResp.EnsureSuccessStatusCode();

        // 4. Private Chat
        Console.WriteLine("4. Testing Private Chat...");
        var createChatResp = await _clientA.PostAsJsonAsync("/api/chats/private", new { otherUserId = userBId });
        createChatResp.EnsureSuccessStatusCode();
        var chatData = await createChatResp.Content.ReadFromJsonAsync<JsonElement>();
        var privateChatId = chatData.GetProperty("chatId").GetGuid();

        // Get Chats list
        await _clientA.GetAsync("/api/chats").EnsureSuccess();

        // 5. Messages
        Console.WriteLine("5. Testing Messages...");
        // Text
        var sendMsgResp = await _clientA.PostAsJsonAsync($"/api/chats/{privateChatId}/messages", 
            new { type = 1, content = "Hello from A" });
        sendMsgResp.EnsureSuccessStatusCode();
        var msgData = await sendMsgResp.Content.ReadFromJsonAsync<JsonElement>();
        var msgId = msgData.GetProperty("messageId").GetGuid();
        Console.WriteLine($"   Message created: {msgId}");

        // Read by B
        var readResp = await _clientB.PutAsJsonAsync($"/api/chats/{privateChatId}/messages/read", new { upToMessageId = msgId });
        readResp.EnsureSuccessStatusCode();

        // Audio
        await _clientA.PostAsJsonAsync($"/api/chats/{privateChatId}/messages", 
            new { type = 2, mediaUrl = "http://example.com/audio.mp3", duration = 45 }).EnsureSuccess();

        // Get Messages
        await _clientA.GetAsync($"/api/chats/{privateChatId}/messages").EnsureSuccess();

        // Mark as Read
        await _clientB.PutAsJsonAsync($"/api/chats/{privateChatId}/messages/read", new { upToMessageId = msgId }).EnsureSuccess();

        // 6. Group Chat
        Console.WriteLine("6. Testing Group Chat...");
        await _clientA.PostAsJsonAsync("/api/chats/group", new { participantIds = new[] { userBId } }).EnsureSuccess();

        // 7. Privacy
        Console.WriteLine("7. Testing Privacy...");
        await _clientA.PostAsJsonAsync("/api/privacy/block", new { userIdToBlock = userBId }).EnsureSuccess();
        await _clientA.GetAsync("/api/privacy/blocked-users").EnsureSuccess();
        await _clientA.PostAsJsonAsync("/api/privacy/unblock", new { userIdToUnblock = userBId }).EnsureSuccess();

        // 8. Search
        Console.WriteLine("8. Testing Search...");
        await _clientA.GetAsync("/api/search/users?query=User").EnsureSuccess();
        await _clientA.GetAsync($"/api/chats/{privateChatId}/messages/search?query=Hello").EnsureSuccess();

        // 9. Stories
        Console.WriteLine("9. Testing Stories...");
        await _clientA.PostAsJsonAsync("/api/stories", new { mediaUrl = "http://example.com/story.jpg" }).EnsureSuccess();
        await _clientA.GetAsync("/api/stories").EnsureSuccess();

        // 10. Devices
        Console.WriteLine("10. Testing Devices...");
        await _clientA.PostAsJsonAsync("/api/devices/register", new { deviceToken = "fcm_token_test", deviceType = 1 }).EnsureSuccess();

        // 11. Storage
        Console.WriteLine("11. Testing Storage...");
        await _clientA.PostAsJsonAsync("/api/storage/upload-url", new { fileName = "test.jpg", contentType = "image/jpeg" }).EnsureSuccess();
    }

    private static async Task<Guid> Authenticate(HttpClient client, string phone)
    {
        // Send OTP
        await client.PostAsJsonAsync("/api/auth/send-otp", new { phoneNumber = phone }).EnsureSuccess();

        // Peek Redis
        string code = await _db.StringGetAsync($"otp:{phone}");
        if (string.IsNullOrEmpty(code)) throw new Exception($"OTP code not found in Redis for {phone}");

        // Verify OTP
        var verifyResp = await client.PostAsJsonAsync("/api/auth/verify-otp", new { phoneNumber = phone, code = code });
        verifyResp.EnsureSuccessStatusCode();
        var tokenData = await verifyResp.Content.ReadFromJsonAsync<JsonElement>();

        var accessToken = tokenData.GetProperty("accessToken").GetString();
        var userId = tokenData.GetProperty("userId").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return userId;
    }
}

public static class HttpExtensions
{
    public static async Task EnsureSuccess(this Task<HttpResponseMessage> task)
    {
        var resp = await task;
        if (!resp.IsSuccessStatusCode)
        {
            var content = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with {resp.StatusCode}: {content}");
        }
    }
}

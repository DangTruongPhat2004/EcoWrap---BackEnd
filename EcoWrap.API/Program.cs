using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Linq;


using EcoWrap.DAL.Entities;
using EcoWrap.DAL.Repositories;
using EcoWrap.BLL.Services;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.IRepositories;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;

namespace EcoWrap.API
{
    // ===== DTOs cho Swagger =====
    public sealed class ChatMessage
    {
        public string role { get; set; } = "user";   // "user" | "assistant"
        public string content { get; set; } = "";
    }

    public sealed class ChatRequest
    {
        public List<ChatMessage> messages { get; set; } = new();
    }

    public sealed class ChatResponse
    {
        public string reply { get; set; } = "";
        public bool? error { get; set; }
        public string? message { get; set; }
        public int? status { get; set; }
        public string? detail { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();

            // ===== CORS cho FE Vite (http/https 5173) =====
            var DevCors = "DevCors";
            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy(DevCors, p =>
                    p.WithOrigins(
                        "http://localhost:5173",
                        "http://127.0.0.1:5173",
                        "https://localhost:5173",
                        "https://127.0.0.1:5173"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                );
            });

            // ===== Repositories =====
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRegisterAccountRepository, RegisterAccountRepository>();
            builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
            builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

            // ===== Services =====
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRegisterAccountService, RegisterAccountService>();
            builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            // ===== JWT Auth =====
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
                    };
                });

            // ===== DbContext (đọc AIChatbotDB trong appsettings.json) =====
            builder.Services.AddDbContext<EcoWrapDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("AIChatbotDB")));

            // ===== Swagger + JWT =====
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "EcoWrap API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập: Bearer {token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ===== Gemini (Google AI Studio) HttpClient =====
            builder.Services.AddHttpClient("googleai", c =>
            {
                c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            var app = builder.Build();

            // ===== Middleware =====
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors(DevCors);
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // ===== GET /ai/models — liệt kê model mà API key có =====
            app.MapGet("/ai/models", async (IHttpClientFactory httpClientFactory, IConfiguration cfg) =>
            {
                var key = cfg["GoogleAI:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
                if (string.IsNullOrWhiteSpace(key))
                    return Results.BadRequest(new { error = true, message = "Missing GOOGLE_API_KEY" });

                var client = httpClientFactory.CreateClient("googleai");
                var res = await client.GetAsync($"v1/models?key={key}");
                var text = await res.Content.ReadAsStringAsync();
                return Results.Text(text, "application/json");
            })
            .WithOpenApi(op =>
            {
                op.Summary = "List available Google AI Studio models";
                return op;
            });

            // ===== POST /ai/chat — proxy tới Gemini (auto-pick model hợp lệ) =====
            app.MapPost("/ai/chat",
            async (ChatRequest req, IHttpClientFactory httpClientFactory, IConfiguration cfg) =>
            {
                var googleApiKey = cfg["GoogleAI:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
                if (string.IsNullOrWhiteSpace(googleApiKey))
                    return Results.BadRequest(new ChatResponse { error = true, message = "Missing GOOGLE_API_KEY (GoogleAI:ApiKey)." });

                if (req?.messages == null || req.messages.Count == 0)
                    return Results.BadRequest(new ChatResponse { error = true, message = "Missing 'messages' array" });

                // Map sang schema Gemini
                var contents = req.messages.Select(m => new
                {
                    role = (m.role == "customer") ? "model" : "user",
                    parts = new[] { new { text = m.content ?? "" } }
                });

                var payload = new { contents };

                var client = httpClientFactory.CreateClient("googleai");

                // --- Chọn model hợp lệ dựa trên danh sách key có ---
                var preferences = new[]
 {
    "models/gemini-2.5-flash",
    "models/gemini-2.5-pro",
    "models/gemini-2.5-flash-lite",
    "models/gemini-2.0-flash",
    "models/gemini-2.0-flash-001",
    "models/gemini-2.0-flash-lite",
    "models/gemini-2.0-flash-lite-001"

};


                string chosen = preferences[0];
                try
                {
                    var listRes = await client.GetAsync($"v1/models?key={googleApiKey}");
                    var listText = await listRes.Content.ReadAsStringAsync();

                    using var listJson = JsonDocument.Parse(listText);
                    if (listJson.RootElement.TryGetProperty("models", out var arr) &&
                        arr.ValueKind == JsonValueKind.Array)
                    {
                        var names = arr.EnumerateArray()
                                       .Select(m => m.GetProperty("name").GetString() ?? "")
                                       .ToHashSet(StringComparer.Ordinal);

                        foreach (var m in preferences)
                            if (names.Contains(m)) { chosen = m; break; }
                    }
                }
                catch
                {
                    // nếu parse lỗi hoặc gọi list thất bại, dùng mặc định
                }

                var httpReq = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"v1/{chosen}:generateContent?key={googleApiKey}") // Lưu ý: phải có prefix "models/..."
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                try
                {
                    var res = await client.SendAsync(httpReq);
                    var text = await res.Content.ReadAsStringAsync();

                    if (!res.IsSuccessStatusCode)
                    {
                        return Results.Json(new ChatResponse
                        {
                            error = true,
                            message = "Gemini error",
                            status = (int)res.StatusCode,
                            detail = text
                        }, statusCode: 500);
                    }

                    using var json = JsonDocument.Parse(text);
                    string reply = "";

                    // 1) Lấy candidate đầu
                    if (json.RootElement.TryGetProperty("candidates", out var cands) &&
                        cands.ValueKind == JsonValueKind.Array &&
                        cands.GetArrayLength() > 0)
                    {
                        var first = cands[0];

                        // 1a) Nếu bị chặn bởi safety → trả message rõ ràng
                        if (first.TryGetProperty("finishReason", out var fr) &&
                            fr.ValueKind == JsonValueKind.String &&
                            fr.GetString()?.Equals("SAFETY", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            var reason = "Trả lời bị chặn bởi bộ lọc an toàn của model.";
                            if (first.TryGetProperty("safetyRatings", out var sr))
                                reason += " (safetyRatings present)";
                            return Results.Json(new ChatResponse
                            {
                                error = true,
                                message = "Safety blocked",
                                detail = reason
                            }, statusCode: 400);
                        }

                        // 1b) Gom tất cả phần text trong parts
                        if (first.TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts) &&
                            parts.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var p in parts.EnumerateArray())
                            {
                                if (p.TryGetProperty("text", out var tt) && tt.ValueKind == JsonValueKind.String)
                                {
                                    reply += tt.GetString();
                                }
                                else if (p.TryGetProperty("executableCode", out var codeObj) && codeObj.TryGetProperty("code", out var codeVal))
                                {
                                    // một số model có thể trả code block
                                    reply += "```\n" + (codeVal.GetString() ?? "") + "\n```\n";
                                }
                                else if (p.TryGetProperty("functionCall", out var fc) && fc.TryGetProperty("name", out var fname))
                                {
                                    reply += $"[Function call: {fname.GetString()}]\n";
                                }
                                // Có thể bổ sung các kiểu parts khác nếu bạn dùng tool-calls...
                            }
                        }
                    }

                    // 2) Nếu vẫn rỗng, thử đoán nguyên nhân
                    if (string.IsNullOrWhiteSpace(reply))
                    {
                        // promptFeedback: thông tin block ở cấp request
                        if (json.RootElement.TryGetProperty("promptFeedback", out var pf) &&
                            pf.TryGetProperty("blockReason", out var br) &&
                            br.ValueKind == JsonValueKind.String)
                        {
                            return Results.Json(new ChatResponse
                            {
                                error = true,
                                message = "Prompt bị chặn",
                                detail = $"blockReason={br.GetString()}"
                            }, statusCode: 400);
                        }

                        reply = "(Model không trả văn bản. Có thể bị safety-filter hoặc format parts khác.)";
                    }

                    return Results.Ok(new ChatResponse { reply = reply });

                }
                catch (Exception ex)
                {
                    return Results.Json(new ChatResponse
                    {
                        error = true,
                        message = "Server error",
                        detail = ex.Message
                    }, statusCode: 500);
                }
            })
            .Accepts<ChatRequest>("application/json")
            .Produces<ChatResponse>(StatusCodes.Status200OK)
            .Produces<ChatResponse>(StatusCodes.Status400BadRequest)
            .Produces<ChatResponse>(StatusCodes.Status500InternalServerError)
            .WithOpenApi(op =>
            {
                op.Summary = "Proxy Gemini chat (Google AI Studio)";
                op.Description = "Body: { \"messages\": [{\"role\":\"user|assistant\",\"content\":\"...\"}] }";
                return op;
            });

            app.Run();
        }
    }
}

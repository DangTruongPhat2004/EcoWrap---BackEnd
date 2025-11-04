using Amazon.S3;
using Azure.Storage.Blobs;
using EcoWrap.API.Realtime;
using EcoWrap.BLL.IServices;
using EcoWrap.BLL.Services;
using EcoWrap.BLL.Services.Chat;
using EcoWrap.BLL.Validation;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.DAL.Repositories;
using EcoWrap.DAL.Repositories.Chat;
using EcoWrap.Infrastructure.Sanitization;
using EcoWrap.Infrastructure.Storage;
using EcoWrap.Models.AI; // DTO: ChatRequest/ChatResponse/RagChatRequest/KbUpsertRequest
using EcoWrap.Models.Templates;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiChatRequest = EcoWrap.Models.AI.ChatRequest;
using AiChatResponse = EcoWrap.Models.AI.ChatResponse;
using CloudinaryDotNet;
using EcoWrap.Models.Designs;




using System.Security.Claims;
using EcoWrap.Models.Config;
using Hangfire;
using Hangfire.SqlServer;

namespace EcoWrap.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

            // ===== CORS cho FE Vite (http/https 5173) =====
            var DevCors = "DevCors";
            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy(DevCors, p =>
                    p.WithOrigins(
                        "http://localhost:5173",
                        "http://127.0.0.1:5173",
                        "https://localhost:5173",
                        "https://127.0.0.1:5173",
                        "https://ecowrap.vercel.app"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                );
            });

            var cloudinaryAccount = new Account(
    builder.Configuration["Storage:Cloudinary:CloudName"],
    builder.Configuration["Storage:Cloudinary:ApiKey"],
    builder.Configuration["Storage:Cloudinary:ApiSecret"]
);
            var cloudinary = new Cloudinary(cloudinaryAccount);
            builder.Services.AddSingleton(new Cloudinary(cloudinaryAccount));

            // ===== Repositories =====
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
            builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IRegisterAccountRepository, RegisterAccountRepository>();
            builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
            builder.Services.AddScoped<ISellerRepository, SellerRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();

            // ===== Services =====
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IAddressService, AddressService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<ICartItemService, CartItemService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            builder.Services.AddScoped<IEmbeddingService, EmbeddingService>(); // Embeddings
            builder.Services.AddScoped<IFeedbackService, FeedbackService>();
            builder.Services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IRegisterAccountService, RegisterAccountService>();
            builder.Services.AddScoped<ISellerRegistrationService, SellerRegistrationService>();
            builder.Services.AddScoped<ISellerService, SellerService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IWarehouseService, WarehouseService>();
            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            //Hangfire SERVICE
            var connectionString = builder.Configuration.GetConnectionString("EcoWrapDBContext")
                ?? throw new InvalidOperationException("Connection string 'EcoWrapDBContext' not found.");
            //Cấu hình Hangfire Services ---
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170) // Hoặc Version_180 nếu dùng .NET 8
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true // Khuyến nghị cho SQL Server
                }));

            // Thêm Hangfire Server để xử lý các job
            builder.Services.AddHangfireServer();
            builder.Services.AddScoped<IBackgroundJobService, HangfireService>();

            //SHIPPING SERVICE
            builder.Services.AddHttpClient<IGhtkService, GhtkService>(); // Sử dụng HttpClientFactory

            //Payment SERVICE
            builder.Services.AddScoped<IPaymentService, PayosService>();

            builder.Services.AddHttpContextAccessor();

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
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "")),
                        RoleClaimType = ClaimTypes.Role
                    };

                    // ✅ THIS IS THE FIX FOR SIGNALR AUTHENTICATION
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            // If the request is for our hub...
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/hubs/chat"))) // Must match your app.MapHub path
                            {
                                // Read the token from the query string
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });


            builder.Services.AddControllers()
                .AddJsonOptions(o =>
                    o.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter()
                    )
                );


            // ===== DbContext =====
            builder.Services.AddDbContext<EcoWrapDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("EcoWrapDBContext")));

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

            // ===== Cloudinary Api =====
            builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Storage:Cloudinary"));
            builder.Services.AddSingleton<CloudinaryService>();

            // ======= Chat ///////

            builder.Services.AddSignalR();
            builder.Services.AddScoped<IChatBus, ChatBus>();
            // Note: You have a second AddCors call here. It's better to combine them,
            // but this won't break anything. The `.RequireCors("chat")` on the hub endpoint will use this one.
            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("chat", p => p
                    .WithOrigins("http://localhost:5173", "http://localhost:3000", "https://ecowrap.vercel.app")
                    .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
            });

            // Repositories
            builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
            builder.Services.AddScoped<IMessageRepository, MessageRepository>();

            // Services
            builder.Services.AddScoped<IConversationService, ConversationService>();
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddScoped<ITemplateService, TemplateService>();
            builder.Services.AddSingleton<ISvgSanitizer, SvgSanitizer>();
            builder.Services.AddScoped<IValidator<MyAssetsQueryDto>, MyAssetsQueryValidator>();
            builder.Services.AddScoped<IValidator<CreateDesignDto>, CreateDesignValidator>();
            builder.Services.AddScoped<IValidator<UpdateDesignDto>, UpdateDesignValidator>();
            builder.Services.AddScoped<ICustomDesignsService, CustomDesignsService>();



            // ===== Gemini HttpClient =====
            builder.Services.AddHttpClient("googleai", c =>
            {
                c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            // ===== S3 Client (cho Storage) =====
            builder.Services.AddSingleton<IFileStorage>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>().GetSection("Storage");
                var cl = cfg.GetSection("Cloudinary");
                var cloudName = cl["CloudName"]!;
                var apiKey = cl["ApiKey"]!;
                var apiSecret = cl["ApiSecret"]!;
                var folder = cl["Folder"];

                var account = new Account(cloudName, apiKey, apiSecret);
                var cld = new Cloudinary(account);
                // (tuỳ) cld.Api.Secure = true;  // đảm bảo https

                var immutable = int.TryParse(cfg["Cache:ImmutableSeconds"], out var im) ? im : 31536000;
                var statc = int.TryParse(cfg["Cache:StaticSeconds"], out var st) ? st : 86400;

                return new CloudinaryFileStorage(cld, folder, immutable, statc);
            });

            // storage options + validate

            builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
           

            builder.Services.AddSingleton<IAssetUrlBuilder, AssetUrlBuilder>();
            builder.Services.AddScoped<IValidator<LibraryQueryDto>, LibraryQueryValidator>();

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<AssetSyncService>();
            builder.Services.AddScoped<UserAssetUploader>();


            // Validator for admin
            builder.Services.AddValidatorsFromAssembly(typeof(AdminCreateTemplateValidator).Assembly);


            var app = builder.Build();
           
            //if (app.Environment.IsDevelopment())
            //{
                app.UseDeveloperExceptionPage(); // show stacktrace trong browser
            //}

            // ===== Middleware =====
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
            //}

            //app.UseHttpsRedirection();

            // Use the general DevCors policy first
            app.UseCors(DevCors);

            // Authentication middleware must come before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // The 'chat' CORS policy is applied specifically to this endpoint
            app.MapHub<ChatHub>("/hubs/chat").RequireCors("chat");




            // ===== GET /ai/models =====
            app.MapGet("/ai/models", async (IHttpClientFactory httpClientFactory, IConfiguration cfg) =>
            {
                var key = cfg["GoogleAI:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
                if (string.IsNullOrWhiteSpace(key))
                    return Results.BadRequest(new { error = true, message = "Missing GOOGLE_API_KEY" });

                var client = httpClientFactory.CreateClient("googleai");
                var res = await client.GetAsync($"v1beta/models?key={key}");
                var text = await res.Content.ReadAsStringAsync();
                return Results.Text(text, "application/json");
            })
            .WithOpenApi(op =>
            {
                op.Summary = "List available Google AI Studio models";
                return op;
            });

            // ===== POST /ai/kb/index — nạp tri thức vào KB =====
            app.MapPost("/ai/kb/index",
            async (KbUpsertRequest req, EcoWrapDbContext db, IEmbeddingService emb) =>
            {
                if (req?.items == null || req.items.Count == 0)
                    return Results.BadRequest(new { error = true, message = "No items" });

                int inserted = 0;
                foreach (var it in req.items)
                {
                    var text = (it.text ?? "").Trim();
                    if (text.Length == 0) continue;

                    var sha = Sha256(text);
                    var dup = db.Set<KbChunk>().FirstOrDefault(x => x.Sha256 == sha);
                    if (dup != null) continue;

                    var vec = await emb.EmbedAsync(text);
                    db.Add(new KbChunk
                    {
                        Id = string.IsNullOrWhiteSpace(it.id) ? Guid.NewGuid().ToString("N") : it.id!,
                        Title = it.title,
                        Source = it.source,
                        Tags = it.tags,
                        Text = text,
                        Embedding = vec,
                        Sha256 = sha
                    });
                    inserted++;
                }
                await db.SaveChangesAsync();
                return Results.Ok(new { inserted });

                static string Sha256(string s)
                {
                    using var sha = SHA256.Create();
                    return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)))
                            .Replace("-", "").ToLowerInvariant();
                }
            })
            .WithOpenApi(op => { op.Summary = "Index knowledge items into KB (with embeddings)"; return op; });

            // ===== POST /ai/chat-rag — chat có RAG =====
            app.MapPost("/ai/chat-rag",
            async (RagChatRequest req,
                    EcoWrapDbContext db,
                    IEmbeddingService emb,
                    IHttpClientFactory http,
                    IConfiguration cfg) =>
            {
                var key = cfg["GoogleAI:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
                if (string.IsNullOrWhiteSpace(key))
                    return Results.BadRequest(new AiChatResponse { error = true, message = "Missing GOOGLE_API_KEY" });

                if (req?.messages == null || req.messages.Count == 0)
                    return Results.BadRequest(new AiChatResponse { error = true, message = "Missing 'messages'" });

                var lastUser = req.messages.LastOrDefault(m => (m.role ?? "user") != "assistant")?.content
                                        ?? req.messages.LastOrDefault()?.content ?? "";

                var qvec = await emb.EmbedAsync(lastUser, "RETRIEVAL_QUERY");
                var all = db.Set<KbChunk>().AsNoTracking().ToList();
                var minScore = 0.28;
                var scored = all.Where(x => x.Embedding != null)
    .Select(x => new { item = x, score = IEmbeddingService.CosineSimilarity(qvec, x.Embedding!) })
    .Where(x => x.score >= minScore)
    .OrderByDescending(x => x.score)
    .Take(Math.Max(3, Math.Min(req.k, 8)))
    .ToList();

                var ctx = string.Join("\n---\n", scored.Select((x, i) =>
    $"[C{i + 1} | SCORE:{x.score:F3} | TITLE:{x.item.Title} | SOURCE:{x.item.Source}]\n{x.item.Text}"));

                var system = req.system ?? "Bạn là EcoWrap AI – trợ lý bao bì thân thiện môi trường. Chỉ dùng NGỮ CẢNH để trả lời.";
                var userAugmented = $@"
# Nhiệm vụ
Bạn là EcoWrap AI – trợ lý bao bì bền vững. Trả lời ngắn gọn, **Markdown đẹp**, chỉ dùng NGỮ CẢNH.

# Câu hỏi
{lastUser}

# NGỮ CẢNH (Top-K | C# | SCORE | TITLE — SOURCE)
{ctx}

# YÊU CẦU ĐỊNH DẠNG
- Mở đầu một dòng: **TL;DR:** <tóm tắt 1–2 câu>.
- Sau đó 3–6 bullet **hành động** (mỗi bullet bắt đầu bằng động từ).
- Kết thúc bằng mục **Nguồn trích dẫn** liệt kê các C# bạn đã dùng, format: `[C#, TITLE — SOURCE]`.
- Không bịa ngoài ngữ cảnh; nếu thiếu dữ liệu, nêu rõ ""thiếu gì"".

# Lưu ý phong cách
- Tránh thuật ngữ khó; nêu rõ trade-off (chống dầu vs tái chế…).
- Không lạm dụng số liệu trừ khi NGỮ CẢNH có.
";

                var client = http.CreateClient("googleai");
                var apiVersion = "v1";
                var model = "models/gemini-2.5-flash";
                var payload = new
                {
                    contents = new object[] {
                        new { role = "user", parts = new[]{ new { text = system } } },
                        new { role = "user", parts = new[]{ new { text = userAugmented } } }
                    }
                };
                var httpReq = new HttpRequestMessage(
     System.Net.Http.HttpMethod.Post,
     $"{apiVersion}/{model}:generateContent?key={key}"
 )
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                var res = await client.SendAsync(httpReq);
                var text = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                    return Results.Json(new AiChatResponse { error = true, message = "Gemini error", status = (int)res.StatusCode, detail = text }, statusCode: 500);

                using var json = JsonDocument.Parse(text);
                string reply = "";
                if (json.RootElement.TryGetProperty("candidates", out var cands) &&
                    cands.ValueKind == JsonValueKind.Array && cands.GetArrayLength() > 0)
                {
                    var parts = cands[0].GetProperty("content").GetProperty("parts").EnumerateArray();
                    foreach (var p in parts)
                        if (p.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String) reply += t.GetString();
                }
                if (string.IsNullOrWhiteSpace(reply)) reply = "(Model không trả văn bản.)";
                return Results.Ok(new AiChatResponse { reply = reply });
            })
            .WithOpenApi(op => { op.Summary = "Chat with RAG over EcoWrap KB"; return op; });

            // ===== POST /ai/chat — proxy Gemini thường =====
            app.MapPost("/ai/chat",
            async (AiChatRequest req, IHttpClientFactory httpClientFactory, IConfiguration cfg) =>
            {
                var googleApiKey = cfg["GoogleAI:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
                if (string.IsNullOrWhiteSpace(googleApiKey))
                    return Results.BadRequest(new AiChatResponse { error = true, message = "Missing GOOGLE_API_KEY (GoogleAI:ApiKey)." });

                if (req?.messages == null || req.messages.Count == 0)
                    return Results.BadRequest(new AiChatResponse { error = true, message = "Missing 'messages' array" });

                // Map sang schema Gemini (assistant -> model; còn lại là user)
                var contents = req.messages.Select(m => new
                {
                    role = (m.role == "assistant") ? "model" : "user",
                    parts = new[] { new { text = m.content ?? "" } }
                });

                var payload = new { contents };

                var client = httpClientFactory.CreateClient("googleai");
                var model = "models/gemini-2.5-flash"; // Default model

                var httpReq = new HttpRequestMessage(
    System.Net.Http.HttpMethod.Post,
    $"v1/{model}:generateContent?key={googleApiKey}")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                try
                {
                    var res = await client.SendAsync(httpReq);
                    var text = await res.Content.ReadAsStringAsync();

                    if (!res.IsSuccessStatusCode)
                    {
                        return Results.Json(new AiChatResponse
                        {
                            error = true,
                            message = "Gemini error",
                            status = (int)res.StatusCode,
                            detail = text
                        }, statusCode: 500);
                    }

                    using var json = JsonDocument.Parse(text);
                    string reply = "";

                    if (json.RootElement.TryGetProperty("candidates", out var cands) &&
                        cands.ValueKind == JsonValueKind.Array &&
                        cands.GetArrayLength() > 0)
                    {
                        var first = cands[0];

                        if (first.TryGetProperty("finishReason", out var fr) &&
                            fr.ValueKind == JsonValueKind.String &&
                            fr.GetString()?.Equals("SAFETY", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            var reason = "Trả lời bị chặn bởi bộ lọc an toàn của model.";
                            if (first.TryGetProperty("safetyRatings", out var sr))
                                reason += " (safetyRatings present)";
                            return Results.Json(new AiChatResponse
                            {
                                error = true,
                                message = "Safety blocked",
                                detail = reason
                            }, statusCode: 400);
                        }

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
                                    reply += "```\n" + (codeVal.GetString() ?? "") + "\n```\n";
                                }
                                else if (p.TryGetProperty("functionCall", out var fc) && fc.TryGetProperty("name", out var fname))
                                {
                                    reply += $"[Function call: {fname.GetString()}]\n";
                                }
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(reply))
                    {
                        if (json.RootElement.TryGetProperty("promptFeedback", out var pf) &&
                            pf.TryGetProperty("blockReason", out var br) &&
                            br.ValueKind == JsonValueKind.String)
                        {
                            return Results.Json(new AiChatResponse
                            {
                                error = true,
                                message = "Prompt bị chặn",
                                detail = $"blockReason={br.GetString()}"
                            }, statusCode: 400);
                        }

                        reply = "(Model không trả văn bản. Có thể bị safety-filter hoặc format parts khác.)";
                    }

                    return Results.Ok(new AiChatResponse { reply = reply });

                }
                catch (Exception ex)
                {
                    return Results.Json(new AiChatResponse
                    {
                        error = true,
                        message = "Server error",
                        detail = ex.Message
                    }, statusCode: 500);
                }
            })
            .Accepts<AiChatRequest>("application/json")
            .Produces<AiChatResponse>(StatusCodes.Status200OK)
            .Produces<AiChatResponse>(StatusCodes.Status400BadRequest)
            .Produces<AiChatResponse>(StatusCodes.Status500InternalServerError)
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

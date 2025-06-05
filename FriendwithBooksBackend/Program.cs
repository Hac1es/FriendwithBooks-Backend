using FriendwithBooksBackend.Data;
using FriendwithBooksBackend.Interfaces;
using FriendwithBooksBackend.Repositories;
using FriendwithBooksBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Google;

AppContext.SetSwitch("System.Net.DisableIPv6", false);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database with enhanced resilience
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions
            .EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(90),
                errorCodesToAdd: null)
    )
);

// Configure background service behavior - prevent app termination on failure
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<CachingServices>();

// Thêm cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Đúng với port frontend của bạn
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Thêm cấu hình JWT
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.IncludeErrorDetails = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
 {
     options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
     options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
 });

var app = builder.Build();

// Test the DB connection with better error handling
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
try
{
    using var conn = new NpgsqlConnection(connString);
    conn.Open();
    Console.WriteLine("Connect to Supabase successful.");
}
catch (Npgsql.NpgsqlException ex) when (ex.InnerException is System.Net.Sockets.SocketException socketEx && socketEx.ErrorCode == 11004)
{
    Console.WriteLine($"DNS resolution error with Supabase host. Please check connection string: {ex.Message}");
    // You might want to log this more formally
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to connect to Supabase: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

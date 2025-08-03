using Timetablegenerator.Connection;
using Microsoft.EntityFrameworkCore;
using Timetablegenerator.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS setup - restrict origins properly
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register DatabaseConnection singleton (if thread safe)
builder.Services.AddSingleton<DatabaseConnection>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    return new DatabaseConnection(config);
});

// Register Entity Framework Core DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Seed initial data on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!dbContext.Admins.Any(a => a.DepartmentId == "ADMIN"))
    {
        dbContext.Admins.Add(new Admin
        {
            DepartmentId = "ADMIN",
            DepartmentName = "admin"
        });

        dbContext.Logins.Add(new Login
        {
            Username = "ADMIN",
            Password = "admin123",  // TODO: Hash on production
            Role = "Admin"
        });

        dbContext.SaveChanges();
    }
}

// Middleware & Routing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();

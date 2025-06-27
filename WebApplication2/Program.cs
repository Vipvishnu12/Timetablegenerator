using Timetablegenerator.Connection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ CORS setup with correct frontend origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")  // ONLY allow Vite dev frontend
              .AllowAnyHeader()
              .AllowAnyOrigin()
              .AllowAnyMethod();
    });
});

// ✅ Register DatabaseConnection for DI
builder.Services.AddSingleton<DatabaseConnection>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    return new DatabaseConnection(config);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ✅ Apply CORS BEFORE routing
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

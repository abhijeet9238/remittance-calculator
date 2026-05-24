using RemittanceCalculatorApi;

var builder = WebApplication.CreateBuilder(args);

// Register standard MVC Controllers & HTTP Client
builder.Services.AddControllers();
builder.Services.AddHttpClient<RemittanceEngine>();
builder.Services.AddCors();

// Enable Swagger UI documentation engines
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Turn on Swagger interface when testing locally
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Allow browser-based frontend applications to read this API safely
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Read controller files automatically
app.MapControllers();

app.Run();
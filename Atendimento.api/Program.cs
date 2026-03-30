using Atendimento.API.Repositories;
using Atendimento.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

//Injeção de dependência para BaseDeConhecimentoService (RAG)
builder.Services.AddSingleton<BaseDeConhecimentoService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();

    var relativePath = config["BaseDeConhecimento:Path"];

    var fullPath = Path.Combine(env.ContentRootPath, relativePath!);

    return new BaseDeConhecimentoService(fullPath);
});

//Injeção de dependência para OpenAIService
builder.Services.AddScoped<OpenAIService>();

//Injeção de dependência para o repositório
builder.Services.AddScoped<AtendimentoRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();

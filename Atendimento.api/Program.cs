using Atendimento.Application.UseCases;
using Atendimento.Domain.Contracts;
using Atendimento.Infrastructure.Repositories;
using Atendimento.Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IKnowledgeBaseService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();

    var relativePath = config["BaseDeConhecimento:Path"];
    if (string.IsNullOrWhiteSpace(relativePath))
        throw new InvalidOperationException("Configuração BaseDeConhecimento:Path não encontrada.");

    var fullPath = Path.Combine(env.ContentRootPath, relativePath);
    return new BaseDeConhecimentoService(fullPath);
});

builder.Services.AddSingleton<IAtendimentoAiService, OpenAIService>();
builder.Services.AddScoped<IAtendimentoRepository, AtendimentoRepository>();
builder.Services.AddScoped<IAtendimentoUseCase, AtendimentoUseCase>();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;

        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            DirectoryNotFoundException => (StatusCodes.Status500InternalServerError, "Base de conhecimento indisponível"),
            InvalidOperationException => (StatusCodes.Status500InternalServerError, "Configuração inválida"),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = app.Environment.IsDevelopment() ? exception?.Message : "Consulte o traceId para suporte."
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.UseAuthorization();
app.MapControllers();
app.Run();

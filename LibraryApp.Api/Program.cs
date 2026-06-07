using FluentValidation;
using FluentValidation.AspNetCore;
using LibraryApp.Api.Middleware;
using LibraryApp.Api.Validators;
using LibraryApp.Core.DTOs;
using LibraryApp.Core.Interfaces;
using LibraryApp.Data;
using LibraryApp.Data.Repositories;
using LibraryApp.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

// Database
builder.Services.AddDbContext<LibraryContext>(opts =>
{
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("LibraryDb"),
        sqlOpts => sqlOpts.CommandTimeout(60)
    );
});

// Dependency Injection
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IMemberService, MemberService>();

// CORS for Angular dev server
builder.Services.AddCors(opts => opts.AddPolicy("Angular", p =>
    p.WithOrigins("http://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()));

builder.Services.AddControllers(options =>
    {
        options.ModelValidatorProviders.Clear();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

            return new ObjectResult(new ValidationProblemDetails(errors)
            {
                Status = 422,
                Title = "One or more validation errors occurred."
            })
            { StatusCode = 422 };
        };
    });
// Program.cs — add after builder.Services.AddControllers()
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateBookCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<DeleteBookCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateAuthorCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateAuthorCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateMemberCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateMemberCommandValidator>();
builder.Services.AddEndpointsApiExplorer();

// Swagger: add doc and include XML comments
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LibraryApp API", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseExceptionMiddleware();  // must be first
app.UseSerilogRequestLogging();
app.UseCors("Angular");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryApp API v1");
    c.RoutePrefix = string.Empty; // serve UI at application root: https://localhost:5001/
});
app.MapControllers();
app.Run();
using Cadastro.Gateway.API.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configura��es do app
var configuration = builder.Configuration;
var jwtKey = configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt Key is missing");

// Configura��o do banco de dados
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseInMemoryDatabase("AuthDB"));

// Configura��o do Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// Configura��o do JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Configura��o do Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot();

// Configura��o dos Controllers e API Explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configura��o do Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gateway API",
        Version = "v3",
        Description = "API para Gateway e Auth",
    });

    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        // Exclui endpoints do Ocelot
        return !(apiDesc.RelativePath?.StartsWith("configuration") ?? false) &&
               !(apiDesc.RelativePath?.StartsWith("outputcache") ?? false);
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "Insira o token JWT (com prefixo Bearer) abaixo. Exemplo: 'Bearer meuToken123'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Inclui coment�rios XML para documenta��o no Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

// Constru��o do app
var app = builder.Build();

// Configura��o do ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware de requisi��o
app.UseHttpsRedirection();

// Ocelot deve ser configurado ap�s o UseRouting e antes do MapControllers
app.UseRouting();
app.UseOcelot().Wait();
app.MapControllers();

// Executa o aplicativo
app.Run();

/*
// Middleware de requisi��o
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Configura��o do Ocelot ap�s o UseRouting
app.UseRouting();
app.UseOcelot().Wait();
//app.MapControllers();

// Executa o aplicativo
app.Run();
*/
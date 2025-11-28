using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Banco de dados em memória (para simplificar)
builder.Services.AddSingleton<Database>();

var app = builder.Build();

// Configuração do pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// ============ ENDPOINTS - PACIENTES ============

app.MapGet("/api/pacientes", (Database db) =>
{
    return Results.Ok(new { sucesso = true, dados = db.Pacientes });
});

app.MapGet("/api/pacientes/{id}", (int id, Database db) =>
{
    var paciente = db.Pacientes.FirstOrDefault(p => p.Id == id);
    return paciente != null
        ? Results.Ok(new { sucesso = true, dados = paciente })
        : Results.NotFound(new { sucesso = false, mensagem = "Paciente não encontrado" });
});

app.MapPost("/api/pacientes", (Paciente paciente, Database db) =>
{
    paciente.Id = db.Pacientes.Any() ? db.Pacientes.Max(p => p.Id) + 1 : 1;
    paciente.DataCadastro = DateTime.Now;
    db.Pacientes.Add(paciente);
    return Results.Created($"/api/pacientes/{paciente.Id}", new { sucesso = true, dados = paciente });
});

app.MapPut("/api/pacientes/{id}", (int id, Paciente pacienteAtualizado, Database db) =>
{
    var paciente = db.Pacientes.FirstOrDefault(p => p.Id == id);
    if (paciente == null)
        return Results.NotFound(new { sucesso = false, mensagem = "Paciente não encontrado" });

    paciente.Nome = pacienteAtualizado.Nome;
    paciente.Email = pacienteAtualizado.Email;
    paciente.Telefone = pacienteAtualizado.Telefone;
    paciente.DataNascimento = pacienteAtualizado.DataNascimento;

    return Results.Ok(new { sucesso = true, dados = paciente });
});

app.MapDelete("/api/pacientes/{id}", (int id, Database db) =>
{
    var paciente = db.Pacientes.FirstOrDefault(p => p.Id == id);
    if (paciente == null)
        return Results.NotFound(new { sucesso = false, mensagem = "Paciente não encontrado" });

    db.Pacientes.Remove(paciente);
    return Results.Ok(new { sucesso = true, mensagem = "Paciente removido com sucesso" });
});

// ============ ENDPOINTS - MÉDICOS ============

app.MapGet("/api/medicos", (Database db) =>
{
    return Results.Ok(new { sucesso = true, dados = db.Medicos });
});

app.MapGet("/api/medicos/{id}", (int id, Database db) =>
{
    var medico = db.Medicos.FirstOrDefault(m => m.Id == id);
    return medico != null
        ? Results.Ok(new { sucesso = true, dados = medico })
        : Results.NotFound(new { sucesso = false, mensagem = "Médico não encontrado" });
});

app.MapPost("/api/medicos", (Medico medico, Database db) =>
{
    medico.Id = db.Medicos.Any() ? db.Medicos.Max(m => m.Id) + 1 : 1;
    db.Medicos.Add(medico);
    return Results.Created($"/api/medicos/{medico.Id}", new { sucesso = true, dados = medico });
});

// ============ ENDPOINTS - CONSULTAS ============

app.MapGet("/api/consultas", (Database db) =>
{
    return Results.Ok(new { sucesso = true, dados = db.Consultas });
});

app.MapGet("/api/consultas/{id}", (int id, Database db) =>
{
    var consulta = db.Consultas.FirstOrDefault(c => c.Id == id);
    return consulta != null
        ? Results.Ok(new { sucesso = true, dados = consulta })
        : Results.NotFound(new { sucesso = false, mensagem = "Consulta não encontrada" });
});

app.MapPost("/api/consultas", (Consulta consulta, Database db) =>
{
    // Validações
    if (!db.Pacientes.Any(p => p.Id == consulta.PacienteId))
        return Results.BadRequest(new { sucesso = false, mensagem = "Paciente não encontrado" });

    if (!db.Medicos.Any(m => m.Id == consulta.MedicoId))
        return Results.BadRequest(new { sucesso = false, mensagem = "Médico não encontrado" });

    consulta.Id = db.Consultas.Any() ? db.Consultas.Max(c => c.Id) + 1 : 1;
    consulta.DataCriacao = DateTime.Now;
    consulta.Status = "Agendada";

    db.Consultas.Add(consulta);
    return Results.Created($"/api/consultas/{consulta.Id}", new { sucesso = true, dados = consulta });
});

app.MapPatch("/api/consultas/{id}/status", (int id, string novoStatus, Database db) =>
{
    var consulta = db.Consultas.FirstOrDefault(c => c.Id == id);
    if (consulta == null)
        return Results.NotFound(new { sucesso = false, mensagem = "Consulta não encontrada" });

    var statusValidos = new[] { "Agendada", "EmAndamento", "Concluída", "Cancelada" };
    if (!statusValidos.Contains(novoStatus))
        return Results.BadRequest(new { sucesso = false, mensagem = "Status inválido" });

    consulta.Status = novoStatus;
    return Results.Ok(new { sucesso = true, dados = consulta });
});

// ============ ENDPOINTS - DASHBOARD ============

app.MapGet("/api/dashboard/estatisticas", (Database db) =>
{
    var stats = new
    {
        totalPacientes = db.Pacientes.Count,
        totalMedicos = db.Medicos.Count,
        totalConsultas = db.Consultas.Count,
        consultasHoje = db.Consultas.Count(c => c.DataHoraConsulta.Date == DateTime.Today),
        consultasAgendadas = db.Consultas.Count(c => c.Status == "Agendada"),
        consultasConcluidas = db.Consultas.Count(c => c.Status == "Concluída")
    };

    return Results.Ok(new { sucesso = true, dados = stats });
});

// ============ ENDPOINT DE SAÚDE ============

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.Now,
        servico = "HealthFlow TeleAtendimento API"
    });
});

Console.WriteLine("🏥 HealthFlow API iniciada!");
Console.WriteLine("📍 Swagger UI disponível em: http://localhost:5000/swagger");
app.Run();

// ============ MODELOS ============

public class Paciente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public DateTime DataCadastro { get; set; }
}

public class Medico
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CRM { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public bool Disponivel { get; set; } = true;
}

public class Consulta
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public int MedicoId { get; set; }
    public DateTime DataHoraConsulta { get; set; }
    public string TipoConsulta { get; set; } = "Teleconsulta"; // Teleconsulta, Presencial
    public string Status { get; set; } = "Agendada"; // Agendada, EmAndamento, Concluída, Cancelada
    public string? Observacoes { get; set; }
    public DateTime DataCriacao { get; set; }
}

// ============ BANCO DE DADOS EM MEMÓRIA ============

public class Database
{
    public List<Paciente> Pacientes { get; set; } = new()
    {
        new Paciente
        {
            Id = 1,
            Nome = "João Silva",
            Email = "joao.silva@email.com",
            Telefone = "(11) 98765-4321",
            DataNascimento = new DateTime(1985, 5, 15),
            DataCadastro = DateTime.Now.AddDays(-30)
        },
        new Paciente
        {
            Id = 2,
            Nome = "Maria Santos",
            Email = "maria.santos@email.com",
            Telefone = "(11) 97654-3210",
            DataNascimento = new DateTime(1990, 8, 22),
            DataCadastro = DateTime.Now.AddDays(-15)
        }
    };

    public List<Medico> Medicos { get; set; } = new()
    {
        new Medico
        {
            Id = 1,
            Nome = "Dr. Carlos Mendes",
            CRM = "123456-SP",
            Especialidade = "Cardiologia",
            Email = "carlos.mendes@healthflow.com",
            Telefone = "(11) 3456-7890",
            Disponivel = true
        },
        new Medico
        {
            Id = 2,
            Nome = "Dra. Ana Paula",
            CRM = "789012-SP",
            Especialidade = "Clínica Geral",
            Email = "ana.paula@healthflow.com",
            Telefone = "(11) 3456-7891",
            Disponivel = true
        }
    };

    public List<Consulta> Consultas { get; set; } = new()
    {
        new Consulta
        {
            Id = 1,
            PacienteId = 1,
            MedicoId = 1,
            DataHoraConsulta = DateTime.Now.AddDays(2).Date.AddHours(14),
            TipoConsulta = "Teleconsulta",
            Status = "Agendada",
            Observacoes = "Consulta de rotina",
            DataCriacao = DateTime.Now
        }
    };
}
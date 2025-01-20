using Microsoft.EntityFrameworkCore;
using seguros.Data;
using seguros.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<InsuranceDB>(options => options.UseNpgsql(connectionString));

//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<InsuranceDB>(options =>options.UseNpgsql(connectionString).EnableSensitiveDataLogging().LogTo(Console.WriteLine));

var AllowedHosts = builder.Configuration.GetValue<string>("PermitirConection")!.Split(',');

// Add server controller

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("New policy", builder =>
        builder.WithOrigins("http://localhost:4200").AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("New policy"); // Habilitar la política de CORS

app.UseAuthorization();

app.MapControllers();

app.MapPost("/insureds", async (Insured insured, InsuranceDB db) =>
{
    // Validación de campos obligatorios
    if (string.IsNullOrEmpty(insured.FirstName))
    {
        return Results.BadRequest("First name is required.");
    }
    if (string.IsNullOrEmpty(insured.LastName))
    {
        return Results.BadRequest("Last name is required.");
    }
    if (string.IsNullOrEmpty(insured.SecondLastName))
    {
        return Results.BadRequest("Second last name is required.");
    }
    if (string.IsNullOrEmpty(insured.PhoneNumber))
    {
        return Results.BadRequest("Phone number is required.");
    }
    if (string.IsNullOrEmpty(insured.Email))
    {
        return Results.BadRequest("Email is required.");
    }
    if (string.IsNullOrEmpty(insured.BrithDate))
    {
        return Results.BadRequest("Birth date is required.");
    }
    if (insured.EstimatedValue <= 0)
    {
        return Results.BadRequest("Estimated value must be greater than zero.");
    }

    // Validación de que EstimatedValue no contenga letras
    if (!int.TryParse(insured.EstimatedValue.ToString(), out _))
    {
        return Results.BadRequest("Estimated value must be a valid number.");
    }

    // Validación de que la fecha de nacimiento no sea posterior a la fecha actual
    if (DateTime.TryParse(insured.BrithDate, out DateTime birthDate))
    {
        if (birthDate > DateTime.Now)
        {
            return Results.BadRequest("Birth date cannot be in the future.");
        }
    }
    else
    {
        return Results.BadRequest("Invalid birth date format.");
    }

    // Validar si el ID ya existe en la base de datos
    var existingInsured = await db.insureds.FindAsync(insured.Id);
    if (existingInsured != null)
    {
        return Results.BadRequest($"Insured with ID {insured.Id} already exists.");
    }

    // Validar si el correo electrónico ya está en uso
    var existingEmail = await db.insureds.FirstOrDefaultAsync(i => i.Email == insured.Email);
    if (existingEmail != null)
    {
        return Results.BadRequest($"Email {insured.Email} is already in use.");
    }

    // Validar si el número de teléfono ya está en uso
    var existingPhone = await db.insureds.FirstOrDefaultAsync(i => i.PhoneNumber == insured.PhoneNumber);
    if (existingPhone != null)
    {
        return Results.BadRequest($"Phone number {insured.PhoneNumber} is already in use.");
    }

    // Si todas las validaciones pasan, agrega el nuevo asegurado a la base de datos
    db.insureds.Add(insured);
    await db.SaveChangesAsync();

    // Devolver una respuesta con el asegurado creado
    return Results.Created($"/insureds/{insured.Id}", insured);
});

app.MapGet("/insureds", async (int? id, InsuranceDB db) =>
{
    if (id.HasValue)
    {
        // Buscar un asegurado por ID
        var insured = await db.insureds.FindAsync(id.Value);

        if (insured == null)
        {
            return Results.NotFound($"Insured with ID {id} not found.");
        }

        return Results.Ok(insured);
    }
    else
    {
        // Obtener todos los asegurados
        var insureds = await db.insureds.ToListAsync();
        return Results.Ok(insureds);
    }
});

// Endpoint PUT para actualizar un asegurado existente
app.MapPut("/insureds/{id}", async (int id, Insured insured, InsuranceDB db) =>
{
    var existingInsured = await db.insureds.FindAsync(id);
    if (existingInsured == null)
    {
        return Results.NotFound($"Insured with ID {id} not found.");
    }

    // Validaciones
    if (string.IsNullOrEmpty(insured.FirstName) || string.IsNullOrEmpty(insured.LastName) || string.IsNullOrEmpty(insured.PhoneNumber) || string.IsNullOrEmpty(insured.Email) || string.IsNullOrEmpty(insured.BrithDate) || insured.EstimatedValue <= 0)
    {
        return Results.BadRequest("All fields except 'Notes' are required and 'EstimatedValue' must be greater than 0.");
    }

    // Validar que el correo electrónico no esté en uso
    if (await db.insureds.AnyAsync(i => i.Email == insured.Email && i.Id != id))
    {
        return Results.BadRequest("The email address is already in use.");
    }

    // Validar que el número de teléfono no esté en uso
    if (await db.insureds.AnyAsync(i => i.PhoneNumber == insured.PhoneNumber && i.Id != id))
    {
        return Results.BadRequest("The phone number is already in use.");
    }

    // Validar que la fecha de nacimiento no sea mayor que la fecha actual
    if (DateTime.TryParse(insured.BrithDate, out var birthDate))
    {
        if (birthDate > DateTime.Now)
        {
            return Results.BadRequest("The birth date cannot be in the future.");
        }
    }
    else
    {
        return Results.BadRequest("Invalid birth date.");
    }

    // Actualizar los campos del asegurado
    existingInsured.FirstName = insured.FirstName;
    existingInsured.SecondName = insured.SecondName;
    existingInsured.LastName = insured.LastName;
    existingInsured.SecondLastName = insured.SecondLastName;
    existingInsured.PhoneNumber = insured.PhoneNumber;
    existingInsured.Email = insured.Email;
    existingInsured.BrithDate = insured.BrithDate;
    existingInsured.EstimatedValue = insured.EstimatedValue;
    existingInsured.Notes = insured.Notes;

    await db.SaveChangesAsync();
    return Results.Ok(existingInsured);
});

// Endpoint DELETE para eliminar un asegurado
app.MapDelete("/insureds/{id}", async (int id, InsuranceDB db) =>
{
    var insured = await db.insureds.FindAsync(id);
    if (insured == null)
    {
        return Results.NotFound($"Insured with ID {id} not found.");
    }

    // Eliminar el asegurado
    db.insureds.Remove(insured);
    await db.SaveChangesAsync();
    return Results.Ok($"Insured with ID {id} has been deleted.");
});


app.Run();

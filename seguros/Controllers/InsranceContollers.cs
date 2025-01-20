using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seguros.Data;
using seguros.Models;

namespace seguros.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceControllers : ControllerBase
    {
        private readonly InsuranceDB _context;

        public InsuranceControllers(InsuranceDB context)
        {
            _context = context;
        }

        private async Task<IActionResult> ValidateInsuredAsync(Insured insured, bool isUpdate = false, int? id = null)
        {
            //r
            // Validar modelo recibido
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validación de campos obligatorios
            if (string.IsNullOrEmpty(insured.FirstName)) return BadRequest("First name is required.");
            if (string.IsNullOrEmpty(insured.LastName)) return BadRequest("Last name is required.");
            if (string.IsNullOrEmpty(insured.PhoneNumber)) return BadRequest("Phone number is required.");
            if (string.IsNullOrEmpty(insured.Email)) return BadRequest("Email is required.");
            if (string.IsNullOrEmpty(insured.BrithDate)) return BadRequest("Birth date is required.");
            if (insured.EstimatedValue <= 0) return BadRequest("Estimated value must be greater than zero.");

            // Validación de formato de fecha de nacimiento
            if (!DateTime.TryParse(insured.BrithDate, out DateTime birthDate))
            {
                return BadRequest("Invalid birth date format.");
            }

            // Validación de que la fecha de nacimiento no sea en el futuro
            if (birthDate > DateTime.Now)
            {
                return BadRequest("Birth date cannot be in the future.");
            }

            // Validación de datos únicos: Email y PhoneNumber
            if (await _context.insureds.AnyAsync(i => i.Email == insured.Email && (!isUpdate || i.Id != id)))
            {
                return BadRequest($"Email {insured.Email} is already in use.");
            }

            if (await _context.insureds.AnyAsync(i => i.PhoneNumber == insured.PhoneNumber && (!isUpdate || i.Id != id)))
            {
                return BadRequest($"Phone number {insured.PhoneNumber} is already in use.");
            }

            return null;
        }

        // POST: api/insurance
        [HttpPost]
        public async Task<IActionResult> CreateInsured([FromBody] Insured insured)
        {
            var validationResult = await ValidateInsuredAsync(insured);
            if (validationResult != null) return validationResult;

            try
            {
                _context.insureds.Add(insured);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetInsuredById), new { id = insured.Id }, insured);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/insurance/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInsuredById(int id)
        {
            var insured = await _context.insureds.FindAsync(id);
            if (insured == null)
            {
                return NotFound($"Insured with ID {id} not found.");
            }

            return Ok(insured);
        }

        // GET: api/insurance
        [HttpGet]
        public async Task<IActionResult> GetAllInsureds()
        {
            var insureds = await _context.insureds.ToListAsync();
            return Ok(insureds);
        }

        // PUT: api/insurance/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInsured(int id, [FromBody] Insured insured)
        {
            var validationResult = await ValidateInsuredAsync(insured, true, id);
            if (validationResult != null) return validationResult;

            var existingInsured = await _context.insureds.FindAsync(id);
            if (existingInsured == null)
            {
                return NotFound($"Insured with ID {id} not found.");
            }

            try
            {
                // Actualizar los datos
                existingInsured.FirstName = insured.FirstName;
                existingInsured.SecondName = insured.SecondName;
                existingInsured.LastName = insured.LastName;
                existingInsured.SecondLastName = insured.SecondLastName;
                existingInsured.PhoneNumber = insured.PhoneNumber;
                existingInsured.Email = insured.Email;
                existingInsured.BrithDate = insured.BrithDate;
                existingInsured.EstimatedValue = insured.EstimatedValue;
                existingInsured.Notes = insured.Notes;

                await _context.SaveChangesAsync();
                return Ok(existingInsured);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/insurance/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInsured(int id)
        {
            var insured = await _context.insureds.FindAsync(id);
            if (insured == null)
            {
                return NotFound($"Insured with ID {id} not found.");
            }

            try
            {
                _context.insureds.Remove(insured);
                await _context.SaveChangesAsync();

                return Ok($"Insured with ID {id} has been deleted.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

namespace seguros.Models
{
    public class Insured
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string? SecondName { get; set; }
        public string LastName { get; set; }
        public string SecondLastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string BrithDate { get; set; }
        public int EstimatedValue { get; set; }
        public string? Notes { get; set; }
    }
}

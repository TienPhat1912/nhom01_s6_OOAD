namespace Hospital.Domain.Entities;

public class DoctorSpecialty
{
    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;

    public bool IsPrimary { get; set; } = false;
}

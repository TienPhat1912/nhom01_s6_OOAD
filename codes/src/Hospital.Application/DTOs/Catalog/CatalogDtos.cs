namespace Hospital.Application.DTOs.Catalog;

public class SpecialtyDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DoctorCount { get; set; }
}

public class DoctorDto
{
    // Only populated for Admin; enables selecting an unlinked profile when provisioning accounts.
    public Guid? LinkedUserId { get; set; }
    public Guid Id { get; set; }
    public string DoctorCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public List<string> Specialties { get; set; } = new();
}

public class DoctorDetailDto
{
    public int? PrimarySpecialtyId { get; set; }
    public Guid Id { get; set; }
    public string DoctorCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public List<SpecialtyDto> Specialties { get; set; } = new();
    public List<DateOnly> AvailableDates { get; set; } = new();
}

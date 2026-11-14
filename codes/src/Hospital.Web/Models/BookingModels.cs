using System.ComponentModel.DataAnnotations;
using Hospital.Application.Common.Models;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.DTOs.Catalog;
using Hospital.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Hospital.Web.Models;

public record BookingContext(Guid ActorId, Guid PatientId, Guid? AppointmentId, byte[] RowVersion);
public class SlotSelection
{
    [Required] public string Context { get; set; } = "";
    public int SpecialtyId { get; set; }
    public Guid DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public Guid TimeSlotId { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
    [StringLength(500)] public string? Reason { get; set; }
}
public class BookingPage : SlotSelection
{
    [ValidateNever] public BookingContext State { get; set; } = null!;
    [ValidateNever] public UserProfileDto Patient { get; set; } = null!;
    [ValidateNever] public PagedResult<SpecialtyDto> Specialties { get; set; } = new([], 0, 1, 9);
    [ValidateNever] public PagedResult<DoctorDto> Doctors { get; set; } = new([], 0, 1, 9);
    [ValidateNever] public DoctorDetailDto Doctor { get; set; } = null!;
    [ValidateNever] public SpecialtyDto Specialty { get; set; } = null!;
    [ValidateNever] public List<TimeSlotDto> Slots { get; set; } = [];
}
public record BookingDraft(BookingContext State, Guid RequestKey, Guid TimeSlotId, int SpecialtyId,
    Guid DoctorId, string PatientName, string? PatientPhone, string DoctorName, string SpecialtyName,
    DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, string? Notes, string? Reason);
public record BookingReview(string DraftToken, BookingDraft Draft);

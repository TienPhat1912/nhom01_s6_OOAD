using System.ComponentModel.DataAnnotations;
using Hospital.Application.DTOs.Appointments;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Hospital.Web.Models;

public class AppointmentActionModel
{
    [Required, StringLength(100)] public string RowVersion { get; set; } = "";
    [StringLength(500)] public string? Reason { get; set; }
    public bool PatientVerified { get; set; }
    [ValidateNever] public AppointmentDto Appointment { get; set; } = null!;
    [ValidateNever] public string Operation { get; set; } = "";
    public string Title => Operation switch { "Cancel" => "Hủy lịch hẹn", "CheckIn" => "Tiếp nhận bệnh nhân", _ => "Ghi nhận không đến" };
}

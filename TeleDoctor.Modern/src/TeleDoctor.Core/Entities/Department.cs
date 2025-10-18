using System.ComponentModel.DataAnnotations;
using TeleDoctor.Core.Enums;

namespace TeleDoctor.Core.Entities;

public class Department : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string NameNorwegian { get; set; } = string.Empty; // Norwegian translation
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(500)]
    public string? DescriptionNorwegian { get; set; } // Norwegian translation
    
    public Specialization Specialization { get; set; }
    
    public string? IconPath { get; set; }
    
    public string? ColorCode { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int SortOrder { get; set; }
    
    // Navigation properties
    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    
    public virtual ICollection<DepartmentService> Services { get; set; } = new List<DepartmentService>();
}

public class DepartmentService : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string ServiceNameNorwegian { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public decimal Price { get; set; }
    
    public int DurationMinutes { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Department Department { get; set; } = null!;
    
    public int DepartmentId { get; set; }
}

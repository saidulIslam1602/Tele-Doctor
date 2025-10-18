using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeleDoctor.Core.Entities;
using TeleDoctor.Core.Enums;
using TeleDoctor.Core.ValueObjects;

namespace TeleDoctor.Infrastructure.Data;

/// <summary>
/// Database seeder for initial data and development/testing purposes
/// Creates sample data including departments, users, and reference data
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds the database with initial data
    /// Creates roles, departments, and sample users for development
    /// </summary>
    public static async Task SeedAsync(
        TeleDoctorDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Ensure database is created and migrated
        await context.Database.MigrateAsync();

        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed departments
        await SeedDepartmentsAsync(context);

        // Seed sample users (only in development)
        await SeedSampleUsersAsync(context, userManager);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates standard user roles
    /// </summary>
    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Patient", "Doctor", "Admin", "Coordinator" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    /// <summary>
    /// Creates medical departments with Norwegian names
    /// </summary>
    private static async Task SeedDepartmentsAsync(TeleDoctorDbContext context)
    {
        if (await context.Departments.AnyAsync())
        {
            return; // Already seeded
        }

        var departments = new List<Department>
        {
            new Department
            {
                Name = "General Practice",
                NameNorwegian = "Allmennmedisin",
                Description = "Primary healthcare and general medical services",
                DescriptionNorwegian = "Primærhelsetjeneste og allmennmedisinske tjenester",
                Specialization = Specialization.GeneralPractice,
                IsActive = true,
                SortOrder = 1
            },
            new Department
            {
                Name = "Cardiology",
                NameNorwegian = "Kardiologi",
                Description = "Heart and cardiovascular system care",
                DescriptionNorwegian = "Hjerte- og karsystembehandling",
                Specialization = Specialization.Cardiology,
                IsActive = true,
                SortOrder = 2
            },
            new Department
            {
                Name = "Dermatology",
                NameNorwegian = "Dermatologi",
                Description = "Skin, hair, and nail conditions",
                DescriptionNorwegian = "Hud-, hår- og negletilstander",
                Specialization = Specialization.Dermatology,
                IsActive = true,
                SortOrder = 3
            },
            new Department
            {
                Name = "Pediatrics",
                NameNorwegian = "Pediatri",
                Description = "Medical care for infants, children, and adolescents",
                DescriptionNorwegian = "Medisinsk behandling for spedbarn, barn og ungdom",
                Specialization = Specialization.Pediatrics,
                IsActive = true,
                SortOrder = 4
            },
            new Department
            {
                Name = "Psychiatry",
                NameNorwegian = "Psykiatri",
                Description = "Mental health and psychiatric care",
                DescriptionNorwegian = "Psykisk helse og psykiatrisk behandling",
                Specialization = Specialization.Psychiatry,
                IsActive = true,
                SortOrder = 5
            }
        };

        await context.Departments.AddRangeAsync(departments);
    }

    /// <summary>
    /// Creates sample users for development and testing
    /// Only called in development environment
    /// </summary>
    private static async Task SeedSampleUsersAsync(
        TeleDoctorDbContext context,
        UserManager<IdentityUser> userManager)
    {
        // Check if any patients exist
        if (await context.Patients.AnyAsync())
        {
            return; // Already seeded
        }

        // Create sample patient
        var patientUser = new IdentityUser
        {
            UserName = "patient@test.com",
            Email = "patient@test.com",
            EmailConfirmed = true
        };

        var patientResult = await userManager.CreateAsync(patientUser, "Patient123!");
        if (patientResult.Succeeded)
        {
            await userManager.AddToRoleAsync(patientUser, "Patient");

            var patient = new Patient
            {
                PersonalInfo = new PersonalInfo
                {
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateTime(1985, 5, 15),
                    Gender = "Male"
                },
                ContactInfo = new ContactInfo
                {
                    Email = "patient@test.com",
                    PhoneNumber = "+47 123 45 678",
                    City = "Oslo",
                    Country = "Norway"
                },
                MedicalInfo = new MedicalInfo
                {
                    BloodGroup = "A+",
                    Allergies = "None known",
                    ChronicConditions = "None"
                },
                PasswordHash = patientUser.PasswordHash!,
                IsEmailVerified = true,
                IsActive = true
            };

            await context.Patients.AddAsync(patient);
        }

        // Create sample doctor
        var department = await context.Departments.FirstOrDefaultAsync();
        if (department != null)
        {
            var doctorUser = new IdentityUser
            {
                UserName = "doctor@test.com",
                Email = "doctor@test.com",
                EmailConfirmed = true
            };

            var doctorResult = await userManager.CreateAsync(doctorUser, "Doctor123!");
            if (doctorResult.Succeeded)
            {
                await userManager.AddToRoleAsync(doctorUser, "Doctor");

                var doctor = new Doctor
                {
                    PersonalInfo = new PersonalInfo
                    {
                        FirstName = "Anna",
                        LastName = "Hansen",
                        DateOfBirth = new DateTime(1980, 3, 20),
                        Gender = "Female"
                    },
                    ContactInfo = new ContactInfo
                    {
                        Email = "doctor@test.com",
                        PhoneNumber = "+47 987 65 432",
                        City = "Oslo",
                        Country = "Norway"
                    },
                    PasswordHash = doctorUser.PasswordHash!,
                    LicenseNumber = "NO-DOC-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    Specialization = Specialization.GeneralPractice,
                    Qualifications = "MD, Specialist in General Practice",
                    Biography = "Experienced general practitioner with 15 years of clinical experience",
                    YearsOfExperience = 15,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    ConsultationFee = 500,
                    IsAvailable = true,
                    IsEmailVerified = true,
                    IsActive = true,
                    DepartmentId = department.Id,
                    IsNorwegianLicensed = true
                };

                await context.Doctors.AddAsync(doctor);
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Models;
using SmartELibrary.Services;
using SmartELibrary.ViewModels;
using System.Text.Json;

namespace SmartELibrary.Controllers;

[Route("Account")]
public class AccountController(ApplicationDbContext dbContext, IOtpService otpService) : Controller
{
    private const string PendingRegistrationKey = "PendingRegistration";

    [HttpGet("RegisterTeacher")]
    public IActionResult RegisterTeacher() => View(new RegisterTeacherViewModel());

    [HttpPost("RegisterTeacher")]
    public async Task<IActionResult> RegisterTeacher(RegisterTeacherViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == model.PhoneNumber);
        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Phone number already registered.");
            return View(model);
        }

        var existingEmail = await dbContext.Users
            .AnyAsync(x => x.Email != null && x.Email.ToLower() == normalizedEmail);
        if (existingEmail)
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
            return View(model);
        }

        var pending = new PendingRegistrationViewModel
        {
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordService.HashPassword(model.Password),
            Role = UserRole.Teacher
        };

        HttpContext.Session.SetString(PendingRegistrationKey, JsonSerializer.Serialize(pending));

        var otp = await otpService.GenerateOtpAsync(model.PhoneNumber);
        await otpService.SendOtpSmsPlaceholderAsync(model.PhoneNumber, otp);
        TempData["DebugOtp"] = otp;
        TempData["PhoneNumber"] = model.PhoneNumber;
        TempData["Success"] = "OTP sent. Verify to complete registration.";

        return RedirectToAction("VerifyOtp", "Auth");
    }

    [HttpGet("RegisterStudent")]
    public IActionResult RegisterStudent() => View(new RegisterStudentViewModel());

    [HttpPost("RegisterStudent")]
    public async Task<IActionResult> RegisterStudent(RegisterStudentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(model.EnrollmentNumber))
        {
            ModelState.AddModelError(nameof(model.EnrollmentNumber), "Enrollment Number is required.");
            return View(model);
        }

        var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == model.PhoneNumber);
        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Phone number already registered.");
            return View(model);
        }

        var existingEmail = await dbContext.Users
            .AnyAsync(x => x.Email != null && x.Email.ToLower() == normalizedEmail);
        if (existingEmail)
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
            return View(model);
        }

        var pending = new PendingRegistrationViewModel
        {
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordService.HashPassword(model.Password),
            EnrollmentNumber = model.EnrollmentNumber.Trim(),
            Role = UserRole.Student
        };

        HttpContext.Session.SetString(PendingRegistrationKey, JsonSerializer.Serialize(pending));

        var otp = await otpService.GenerateOtpAsync(model.PhoneNumber);
        await otpService.SendOtpSmsPlaceholderAsync(model.PhoneNumber, otp);
        TempData["DebugOtp"] = otp;
        TempData["PhoneNumber"] = model.PhoneNumber;
        TempData["Success"] = "OTP sent. Verify to complete registration.";

        return RedirectToAction("VerifyOtp", "Auth");
    }
}

using System;
using CEMETRIX.Application.DTOs.Auth;
using FluentValidation;

namespace CEMETRIX.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3).MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().Must(e => e.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) || e.EndsWith("@googlemail.com", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Registration requires a Gmail address.");
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Otp).NotEmpty().Length(6).Matches(@"^\d{6}$").WithMessage("Enter the 6-digit code from your email.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
}

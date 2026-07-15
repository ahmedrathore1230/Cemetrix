using CEMETRIX.Application.DTOs.Bookings;
using CEMETRIX.Application.DTOs.Deceased;
using CEMETRIX.Application.DTOs.Graves;
using CEMETRIX.Application.DTOs.Notifications;
using CEMETRIX.Application.DTOs.Visitors;
using FluentValidation;

namespace CEMETRIX.Application.Validators;

public class CreateGraveValidator : AbstractValidator<CreateGraveDto>
{
    public CreateGraveValidator()
    {
        RuleFor(x => x.GraveNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Section).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Row).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Column).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class UpdateGraveValidator : AbstractValidator<UpdateGraveDto>
{
    public UpdateGraveValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        Include(new CreateGraveValidator());
    }
}

public class CreateDeceasedValidator : AbstractValidator<CreateDeceasedPersonDto>
{
    public CreateDeceasedValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DOB).LessThan(x => x.DOD).WithMessage("DOB must be before DOD.");
        RuleFor(x => x.Religion).MaximumLength(100);
        RuleFor(x => x.GraveId).GreaterThan(0);
    }
}

public class UpdateDeceasedValidator : AbstractValidator<UpdateDeceasedPersonDto>
{
    public UpdateDeceasedValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        Include(new CreateDeceasedValidator());
    }
}

public class CreateBookingValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.GraveId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
    }
}

public class CreateNotificationValidator : AbstractValidator<CreateNotificationDto>
{
    public CreateNotificationValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty();
    }
}

public class CreateVisitorValidator : AbstractValidator<CreateVisitorDto>
{
    public CreateVisitorValidator()
    {
        RuleFor(x => x.VisitorName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Purpose).NotEmpty();
    }
}

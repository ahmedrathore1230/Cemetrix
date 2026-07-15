using System;
using AutoMapper;
using CEMETRIX.Application.DTOs.Bookings;
using CEMETRIX.Application.DTOs.Deceased;
using CEMETRIX.Application.DTOs.Notifications;
using CEMETRIX.Application.Interfaces.Repositories;
using CEMETRIX.Application.Interfaces.Services;
using CEMETRIX.Domain.Entities;
using CEMETRIX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CEMETRIX.Application.Services;

public class BurialWorkflowService : IBurialWorkflowService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public BurialWorkflowService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<CompleteBurialBookingResult> CompleteBurialBookingAsync(CompleteBurialBookingRequest request, string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("You must be signed in to confirm a booking. Please log out and sign in again.");
        if (string.IsNullOrWhiteSpace(request.Deceased.FullName))
            throw new InvalidOperationException("Full name is required.");
        if (request.Deceased.GraveId <= 0)
            throw new InvalidOperationException("Please select a grave.");
        if (request.Deceased.DOB.Date >= request.Deceased.DOD.Date)
            throw new InvalidOperationException("Date of birth must be before date of death.");

        var grave = await _uow.Graves.GetByIdAsync(request.Deceased.GraveId)
            ?? throw new InvalidOperationException("Selected grave was not found.");
        if (grave.IsDeleted)
            throw new InvalidOperationException("Selected grave is no longer available.");
        if (grave.Status != GraveStatus.Available && grave.Status != GraveStatus.Reserved)
            throw new InvalidOperationException($"Grave {grave.GraveNumber} is not available for booking (status: {grave.Status}).");

        var existingBurial = await _uow.Deceased.Query()
            .AnyAsync(d => d.GraveId == grave.Id && !d.IsDeleted);
        if (existingBurial)
            throw new InvalidOperationException($"Grave {grave.GraveNumber} already has a burial record.");

        var deceased = _mapper.Map<DeceasedPerson>(request.Deceased);
        deceased.FullName = deceased.FullName.Trim();
        deceased.Religion = string.IsNullOrWhiteSpace(deceased.Religion) ? "Muslim" : deceased.Religion.Trim();
        deceased.DOB = DateTime.SpecifyKind(deceased.DOB.Date, DateTimeKind.Utc);
        deceased.DOD = DateTime.SpecifyKind(deceased.DOD.Date, DateTimeKind.Utc);
        deceased.CreatedBy = userId;
        deceased.CreatedAt = DateTime.UtcNow;
        deceased.PhotoUrl = string.IsNullOrWhiteSpace(request.Deceased.PhotoUrl) ? null : request.Deceased.PhotoUrl.Trim();
        deceased.FamilyContact = request.Deceased.FamilyContact?.Trim();
        deceased.MemorialNotes = request.Deceased.MemorialNotes?.Trim();
        deceased.Nationality = request.Deceased.Nationality?.Trim();
        deceased.CauseOfDeath = request.Deceased.CauseOfDeath?.Trim();

        await _uow.Deceased.AddAsync(deceased);

        grave.Status = GraveStatus.Occupied;
        grave.BurialDate = request.Deceased.DOD;
        grave.ExpirationDate = request.Deceased.DOD.AddYears(7);
        grave.UpdatedAt = DateTime.UtcNow;
        grave.UpdatedBy = userId;
        _uow.Graves.Update(grave);

        var amount = request.Amount > 0 ? request.Amount : grave.Price;
        var booking = new Booking
        {
            UserId = userId,
            GraveId = grave.Id,
            BookingDate = DateTime.SpecifyKind(request.BookingDate.Date, DateTimeKind.Utc),
            PaymentStatus = request.PaymentStatus,
            Amount = amount,
            ReferenceNumber = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            ContactPerson = request.ContactPerson,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            Notes = request.Notes,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Bookings.AddAsync(booking);

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            UserId = userId,
            Action = "Burial Booked",
            Description = $"Burial registered for {deceased.FullName} at grave {grave.GraveNumber}.",
            Timestamp = DateTime.UtcNow
        });

        await _uow.Notifications.AddAsync(new Notification
        {
            Title = "New burial booking",
            Message = $"{deceased.FullName} was registered at grave {grave.GraveNumber}.",
            Type = NotificationType.Booking,
            Icon = "bi-bookmark-check-fill",
            Link = $"/grave/{grave.Id}",
            UserId = userId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        var saved = await _uow.SaveChangesAsync();
        if (saved < 1)
            throw new InvalidOperationException("Burial record could not be saved to the database. Please try again.");

        var savedDeceased = await _uow.Deceased.Query()
            .Include(d => d.Grave)
            .FirstAsync(d => d.Id == deceased.Id);

        var savedBooking = await _uow.Bookings.Query()
            .Include(b => b.Grave)
            .Include(b => b.User)
            .FirstAsync(b => b.Id == booking.Id);

        return new CompleteBurialBookingResult
        {
            Deceased = _mapper.Map<DeceasedPersonDto>(savedDeceased),
            Booking = _mapper.Map<BookingDto>(savedBooking),
            GraveNumber = grave.GraveNumber
        };
    }
}

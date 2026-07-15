using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CEMETRIX.Application.DTOs.Dashboard;
using CEMETRIX.Application.Interfaces.Repositories;
using CEMETRIX.Application.Interfaces.Services;
using CEMETRIX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CEMETRIX.Application.Services;

public class DashboardService : IDashboardService, IReportService
{
    private readonly IUnitOfWork _uow;

    public DashboardService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var graves = _uow.Graves.Query().Where(g => !g.IsDeleted);
        var deceased = _uow.Deceased.Query().Where(d => !d.IsDeleted);
        var bookings = _uow.Bookings.Query().Where(b => !b.IsDeleted);
        var visitors = _uow.Visitors.Query().Where(v => !v.IsDeleted);
        var notifications = _uow.Notifications.Query().Where(n => !n.IsDeleted);

        var total = await graves.CountAsync();
        var available = await graves.CountAsync(g => g.Status == GraveStatus.Available);
        var occupied = await graves.CountAsync(g => g.Status == GraveStatus.Occupied);
        var reserved = await graves.CountAsync(g => g.Status == GraveStatus.Reserved);

        var cutoff = DateTime.UtcNow.AddDays(90);
        var expiring = await graves.CountAsync(g => g.ExpirationDate != null && g.ExpirationDate <= cutoff && g.ExpirationDate >= DateTime.UtcNow);

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentBurials = await deceased.CountAsync(d => d.DOD >= thirtyDaysAgo);
        var totalDeceased = await deceased.CountAsync();

        var totalBookings = await bookings.CountAsync();
        var totalRevenue = await bookings.Where(b => b.PaymentStatus == PaymentStatus.Paid).SumAsync(b => (decimal?)b.Amount) ?? 0m;
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthlyRevenue = await bookings.Where(b => b.PaymentStatus == PaymentStatus.Paid && b.BookingDate >= startOfMonth)
            .SumAsync(b => (decimal?)b.Amount) ?? 0m;

        var totalVisitors = await visitors.CountAsync();
        var unread = await notifications.CountAsync(n => !n.IsRead);

        return new DashboardStatsDto
        {
            TotalGraves = total,
            AvailableGraves = available,
            OccupiedGraves = occupied,
            ReservedGraves = reserved,
            ExpiringGraves = expiring,
            RecentBurials = recentBurials,
            TotalDeceased = totalDeceased,
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            TotalVisitors = totalVisitors,
            UnreadNotifications = unread,
            OccupancyRate = total == 0 ? 0 : Math.Round((occupied + reserved) * 100.0 / total, 1)
        };
    }

    public async Task<DashboardChartsDto> GetChartsAsync()
    {
        var deceased = _uow.Deceased.Query().Where(d => !d.IsDeleted);
        var bookings = _uow.Bookings.Query().Where(b => !b.IsDeleted);
        var graves = _uow.Graves.Query().Where(g => !g.IsDeleted);

        var since = DateTime.UtcNow.AddMonths(-11);
        since = new DateTime(since.Year, since.Month, 1);

        var burialsRaw = await deceased
            .Where(d => d.DOD >= since)
            .GroupBy(d => new { d.DOD.Year, d.DOD.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync();

        var revenueRaw = await bookings
            .Where(b => b.PaymentStatus == PaymentStatus.Paid && b.BookingDate >= since)
            .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Sum = g.Sum(x => x.Amount) })
            .ToListAsync();

        var burialsPerMonth = new List<ChartDataPointDto>();
        var revenuePerMonth = new List<ChartDataPointDto>();
        for (int i = 0; i < 12; i++)
        {
            var dt = since.AddMonths(i);
            var label = dt.ToString("MMM yy", CultureInfo.InvariantCulture);
            burialsPerMonth.Add(new ChartDataPointDto { Label = label, Value = burialsRaw.FirstOrDefault(b => b.Year == dt.Year && b.Month == dt.Month)?.Count ?? 0 });
            revenuePerMonth.Add(new ChartDataPointDto { Label = label, Value = revenueRaw.FirstOrDefault(b => b.Year == dt.Year && b.Month == dt.Month)?.Sum ?? 0m });
        }

        var statusBreakdown = await graves
            .GroupBy(g => g.Status)
            .Select(g => new ChartDataPointDto { Label = g.Key.ToString(), Value = g.Count() })
            .ToListAsync();

        var bySection = await graves
            .Where(g => g.Status == GraveStatus.Occupied)
            .GroupBy(g => g.Section)
            .Select(g => new ChartDataPointDto { Label = g.Key, Value = g.Count() })
            .OrderBy(x => x.Label)
            .ToListAsync();

        return new DashboardChartsDto
        {
            BurialsPerMonth = burialsPerMonth,
            RevenuePerMonth = revenuePerMonth,
            GraveStatusBreakdown = statusBreakdown,
            BurialsPerSection = bySection
        };
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync()
    {
        var stats = await GetStatsAsync();
        var charts = await GetChartsAsync();

        var recent = await _uow.ActivityLogs.Query()
            .OrderByDescending(x => x.Timestamp)
            .Take(15)
            .Select(x => new RecentActivityDto
            {
                Id = x.Id,
                Action = x.Action,
                Description = x.Description,
                Timestamp = x.Timestamp,
                UserName = x.User != null ? x.User.FullName : null
            })
            .ToListAsync();

        var cutoff = DateTime.UtcNow.AddDays(90);
        var expiring = await _uow.Graves.Query()
            .Where(g => !g.IsDeleted && g.ExpirationDate != null && g.ExpirationDate <= cutoff && g.ExpirationDate >= DateTime.UtcNow)
            .OrderBy(g => g.ExpirationDate)
            .Take(10)
            .Select(g => new ChartDataPointDto { Label = g.GraveNumber + " — " + g.Section, Value = (decimal)(g.ExpirationDate!.Value - DateTime.UtcNow).TotalDays })
            .ToListAsync();

        return new DashboardOverviewDto
        {
            Stats = stats,
            Charts = charts,
            RecentActivity = recent,
            ExpiringGravesList = expiring
        };
    }

    public Task<DashboardStatsDto> GetSummaryAsync() => GetStatsAsync();

    public async Task<List<ChartDataPointDto>> GetBurialsPerYearAsync()
    {
        return await _uow.Deceased.Query()
            .Where(d => !d.IsDeleted)
            .GroupBy(d => d.DOD.Year)
            .Select(g => new ChartDataPointDto { Label = g.Key.ToString(), Value = g.Count() })
            .OrderBy(x => x.Label)
            .ToListAsync();
    }

    public async Task<List<ChartDataPointDto>> GetRevenueBySectionAsync()
    {
        var data = await _uow.Bookings.Query()
            .Where(b => !b.IsDeleted && b.PaymentStatus == PaymentStatus.Paid && b.Grave != null)
            .GroupBy(b => b.Grave!.Section)
            .Select(g => new ChartDataPointDto { Label = g.Key, Value = g.Sum(x => x.Amount) })
            .ToListAsync();
        return data.OrderBy(x => x.Label).ToList();
    }
}

# CEMETRIX — Database & Gmail setup

## Database (SQL Server)

CEMETRIX uses **Microsoft SQL Server** via Entity Framework Core. All devices share the same data when they use the **same connection string**.

### Visual Studio (default)

1. Open `CEMETRIX.sln`
2. Set **CEMETRIX.Web** as startup project → **F5**
3. Default connection uses **LocalDB**: `(localdb)\mssqllocaldb`, database `CEMETRIX_Db`
4. Migrations run automatically on startup

### Multiple devices (office / LAN)

1. Install **SQL Server Express** on one machine
2. Enable TCP/IP, create login `cemetrix_app` with access to `CEMETRIX_Db`
3. Set connection string (User Secrets recommended):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=192.168.1.10\\SQLEXPRESS;Database=CEMETRIX_Db;User Id=cemetrix_app;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

4. Deploy or run the web app on a machine all clients can reach (`http://server-ip:5099`)

### Azure SQL (cloud)

Use the Azure portal connection string in `appsettings.Production.json` or environment variables.

**User Secrets in Visual Studio:** right-click **CEMETRIX.Web** → **Manage User Secrets** and add:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING"
  },
  "Email": {
    "Username": "your@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your@gmail.com",
    "EnableSending": true
  }
}
```

---

## Gmail OTP & email confirmation

1. Use a Google account with **2-Step Verification**
2. Create an **App Password**: Google Account → Security → App passwords → Mail
3. Set in User Secrets or `appsettings.Development.json`:

```json
"Email": {
  "SmtpServer": "smtp.gmail.com",
  "Port": 587,
  "Username": "your@gmail.com",
  "Password": "xxxx xxxx xxxx xxxx",
  "FromEmail": "your@gmail.com",
  "FromName": "CEMETRIX",
  "EnableSending": true,
  "BaseUrl": "http://localhost:5099"
}
```

When `EnableSending` is **false**, OTP codes and confirm links are written to the **application log** (`logs/cemetrix-web-*.log`) for local testing.

### Features

- **Register:** Gmail only; welcome email sent once to prove the address (when SMTP is configured). Sign in immediately — no link required on each login.
- **Forgot password:** 6-digit OTP to registered email (only if account exists)
- **Change password (Settings):** current password only
- **All cemetery data** (burials, bookings, graves, visitors) is persisted in SQL Server via EF Core

---

## Demo accounts

| Role | Email | Name | Password |
|------|-------|------|----------|
| Admin | muhammad.ahmed.rathore@gmail.com | Muhammad Ahmed Rathore | Admin@12345 |
| Manager | muhammad.ayan.ali@gmail.com | Muhammad Ayan Ali | Admin@12345 |
| Staff | muhammad.bilal.nasir@gmail.com | Muhammad Bilal Nasir | Admin@12345 |
| Viewer | muhammad.ali.rathore@gmail.com | Muhammad Ali | Admin@12345 |

Use real Gmail addresses you control, or update emails in `DbSeeder.cs` and re-run the app.

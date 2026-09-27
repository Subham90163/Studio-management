# Studio Management System Backend

This is a .NET 8 Web API written in VB.NET for managing a studio (bookings, services, payments).

## Requirements
- .NET 8 SDK
- MongoDB Server running locally (or adjust `appsettings.json`)

## Running the Application

1. Open a terminal in this directory.
2. Run `dotnet restore` to install dependencies.
3. Run `dotnet run` to start the application.

## Seed Data
Navigate to `/api/admin/seed` in your browser or Postman to create an initial admin user and sample data.

Admin Credentials:
- Email: admin@studio.com
- Password: admin123

## API Documentation
Once running, navigate to `http://localhost:<port>/swagger` to view the API documentation and test endpoints.

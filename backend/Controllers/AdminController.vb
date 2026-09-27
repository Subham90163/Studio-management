Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc
Imports MongoDB.Driver
Imports StudioAPI.DTOs
Imports StudioAPI.Models
Imports StudioAPI.Services
Imports System.Security.Claims

Namespace Controllers
    <Route("api/[controller]")>
    <ApiController>
    <Authorize(Roles:="ADMIN")>
    Public Class AdminController
        Inherits ControllerBase

        Private ReadOnly _mongoService As MongoDbService

        Public Sub New(mongoService As MongoDbService)
            _mongoService = mongoService
        End Sub

        ' ── Users ────────────────────────────────────────────────────
        <HttpGet("users")>
        Public Async Function GetUsers() As Task(Of IActionResult)
            Dim filter = Builders(Of User).Filter.Empty
            Dim users = Await _mongoService.Users.Find(filter).ToListAsync()
            Return Ok(New ApiResponse(Of Object)(True, "Users retrieved", users))
        End Function

        <HttpPut("users/{id}/toggle")>
        Public Async Function ToggleUser(id As String) As Task(Of IActionResult)
            Dim filter = Builders(Of User).Filter.Eq(Function(u) u.Id, id)
            Dim user = Await _mongoService.Users.Find(filter).FirstOrDefaultAsync()
            If user Is Nothing Then Return NotFound(New ApiResponse(Of Object)(False, "User not found"))

            Dim update = Builders(Of User).Update.Set(Function(u) u.IsActive, Not user.IsActive)
            Await _mongoService.Users.UpdateOneAsync(filter, update)
            Return Ok(New ApiResponse(Of Object)(True, "User status toggled"))
        End Function

        <HttpPost("users")>
        Public Async Function CreateUser(<FromBody> request As CreateUserRequest) As Task(Of IActionResult)
            Try
                If String.IsNullOrWhiteSpace(request.Name) OrElse
                   String.IsNullOrWhiteSpace(request.Email) OrElse
                   String.IsNullOrWhiteSpace(request.Password) Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Name, email, and password are required."))
                End If

                If String.IsNullOrWhiteSpace(request.Phone) Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Phone number is mandatory."))
                End If

                Dim filter = Builders(Of User).Filter.Eq(Function(u) u.Email, request.Email.Trim().ToLower())
                Dim existing = Await _mongoService.Users.Find(filter).FirstOrDefaultAsync()
                If existing IsNot Nothing Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "A user with this email already exists."))
                End If

                Dim role = If(String.Equals(request.Role, "ADMIN", StringComparison.OrdinalIgnoreCase), "ADMIN", "USER")

                Dim newUser As New User With {
                    .Name = request.Name.Trim(),
                    .Email = request.Email.Trim().ToLower(),
                    .Phone = request.Phone.Trim(),
                    .PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    .Role = role,
                    .IsActive = True,
                    .CreatedAt = DateTime.UtcNow
                }

                Await _mongoService.Users.InsertOneAsync(newUser)
                Return Ok(New ApiResponse(Of Object)(True, "User created successfully", newUser))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function

        <HttpDelete("users/{id}")>
        Public Async Function DeleteUser(id As String) As Task(Of IActionResult)
            Try
                Dim currentUserId = MyBase.User.FindFirstValue(ClaimTypes.NameIdentifier)
                If String.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase) Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "You cannot delete your own account."))
                End If

                Dim filter = Builders(Of User).Filter.Eq(Function(u) u.Id, id)
                Dim user = Await _mongoService.Users.Find(filter).FirstOrDefaultAsync()
                If user Is Nothing Then
                    Return NotFound(New ApiResponse(Of Object)(False, "User not found"))
                End If

                Await _mongoService.Users.DeleteOneAsync(filter)
                Return Ok(New ApiResponse(Of Object)(True, "User deleted successfully"))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function

        ' ── Studios ──────────────────────────────────────────────────
        <HttpGet("studios")>
        Public Async Function GetStudios() As Task(Of IActionResult)
            Dim filter = Builders(Of Studio).Filter.Empty
            Dim studios = Await _mongoService.Studios.Find(filter).ToListAsync()
            Return Ok(New ApiResponse(Of Object)(True, "Studios retrieved", studios))
        End Function

        <HttpPost("studios")>
        Public Async Function CreateStudio(<FromBody> studio As Studio) As Task(Of IActionResult)
            studio.Id = Nothing
            Await _mongoService.Studios.InsertOneAsync(studio)
            Return Ok(New ApiResponse(Of Object)(True, "Studio created", studio))
        End Function

        <HttpPut("studios/{id}")>
        Public Async Function UpdateStudio(id As String, <FromBody> studio As Studio) As Task(Of IActionResult)
            studio.Id = id
            Dim filter = Builders(Of Studio).Filter.Eq(Function(s) s.Id, id)
            Await _mongoService.Studios.ReplaceOneAsync(filter, studio)
            Return Ok(New ApiResponse(Of Object)(True, "Studio updated"))
        End Function

        <HttpDelete("studios/{id}")>
        Public Async Function DeleteStudio(id As String) As Task(Of IActionResult)
            Dim filter = Builders(Of Studio).Filter.Eq(Function(s) s.Id, id)
            Await _mongoService.Studios.DeleteOneAsync(filter)
            Return Ok(New ApiResponse(Of Object)(True, "Studio deleted"))
        End Function

        ' ── Services ─────────────────────────────────────────────────
        <HttpGet("services")>
        Public Async Function GetServices() As Task(Of IActionResult)
            Dim filter = Builders(Of Service).Filter.Empty
            Dim services = Await _mongoService.Services.Find(filter).ToListAsync()
            Return Ok(New ApiResponse(Of Object)(True, "Services retrieved", services))
        End Function

        <HttpPost("services")>
        Public Async Function CreateService(<FromBody> service As Service) As Task(Of IActionResult)
            service.Id = Nothing
            Await _mongoService.Services.InsertOneAsync(service)
            Return Ok(New ApiResponse(Of Object)(True, "Service created", service))
        End Function

        <HttpPut("services/{id}")>
        Public Async Function UpdateService(id As String, <FromBody> service As Service) As Task(Of IActionResult)
            service.Id = id
            Dim filter = Builders(Of Service).Filter.Eq(Function(s) s.Id, id)
            Await _mongoService.Services.ReplaceOneAsync(filter, service)
            Return Ok(New ApiResponse(Of Object)(True, "Service updated"))
        End Function

        <HttpDelete("services/{id}")>
        Public Async Function DeleteService(id As String) As Task(Of IActionResult)
            Dim filter = Builders(Of Service).Filter.Eq(Function(s) s.Id, id)
            Await _mongoService.Services.DeleteOneAsync(filter)
            Return Ok(New ApiResponse(Of Object)(True, "Service deleted"))
        End Function

        ' ── Bookings ─────────────────────────────────────────────────
        <HttpGet("bookings")>
        Public Async Function GetBookings() As Task(Of IActionResult)
            Dim filter = Builders(Of Booking).Filter.Empty
            Dim bookings = Await _mongoService.Bookings.Find(filter).SortByDescending(Function(b) b.CreatedAt).ToListAsync()
            Return Ok(New ApiResponse(Of Object)(True, "Bookings retrieved", bookings))
        End Function

        <HttpPut("bookings/{id}/confirm")>
        Public Async Function ConfirmBooking(id As String) As Task(Of IActionResult)
            Return Await SetBookingStatus(id, "Confirmed")
        End Function

        <HttpPut("bookings/{id}/cancel")>
        Public Async Function CancelBookingAdmin(id As String) As Task(Of IActionResult)
            Return Await SetBookingStatus(id, "Cancelled")
        End Function

        <HttpPut("bookings/{id}/complete")>
        Public Async Function CompleteBooking(id As String) As Task(Of IActionResult)
            Dim filter = Builders(Of Booking).Filter.Eq(Function(b) b.Id, id)
            Dim update = Builders(Of Booking).Update _
                .Set(Function(b) b.Status, "Completed") _
                .Set(Function(b) b.PaymentStatus, "Paid")
            Await _mongoService.Bookings.UpdateOneAsync(filter, update)

            Dim payFilter = Builders(Of Payment).Filter.Eq(Function(p) p.BookingId, id)
            Dim paymentUpdate = Builders(Of Payment).Update.Set(Function(p) p.Status, "Paid")
            Await _mongoService.Payments.UpdateOneAsync(payFilter, paymentUpdate)

            Return Ok(New ApiResponse(Of Object)(True, "Booking marked as Completed and payment set to Paid"))
        End Function

        Private Async Function SetBookingStatus(id As String, status As String) As Task(Of IActionResult)
            Dim filter = Builders(Of Booking).Filter.Eq(Function(b) b.Id, id)
            Dim update = Builders(Of Booking).Update.Set(Function(b) b.Status, status)
            Await _mongoService.Bookings.UpdateOneAsync(filter, update)
            Return Ok(New ApiResponse(Of Object)(True, $"Booking marked as {status}"))
        End Function

        ' ── Payments ─────────────────────────────────────────────────
        <HttpGet("payments")>
        Public Async Function GetPayments() As Task(Of IActionResult)
            Dim filter = Builders(Of Payment).Filter.Empty
            Dim payments = Await _mongoService.Payments.Find(filter).SortByDescending(Function(p) p.CreatedAt).ToListAsync()
            Return Ok(New ApiResponse(Of Object)(True, "Payments retrieved", payments))
        End Function

        <HttpPut("payments/{id}/mark-paid")>
        Public Async Function MarkPaymentPaid(id As String) As Task(Of IActionResult)
            Dim filter = Builders(Of Payment).Filter.Eq(Function(p) p.Id, id)
            Dim payment = Await _mongoService.Payments.Find(filter).FirstOrDefaultAsync()
            If payment Is Nothing Then Return NotFound(New ApiResponse(Of Object)(False, "Payment not found"))

            Dim update = Builders(Of Payment).Update.Set(Function(p) p.Status, "Paid")
            Await _mongoService.Payments.UpdateOneAsync(filter, update)

            If Not String.IsNullOrEmpty(payment.BookingId) Then
                Dim bFilter = Builders(Of Booking).Filter.Eq(Function(b) b.Id, payment.BookingId)
                Dim bUpdate = Builders(Of Booking).Update _
                    .Set(Function(b) b.PaymentStatus, "Paid") _
                    .Set(Function(b) b.Status, "Confirmed")
                Await _mongoService.Bookings.UpdateOneAsync(bFilter, bUpdate)
            End If

            Return Ok(New ApiResponse(Of Object)(True, "Payment marked as Paid"))
        End Function

        ' ── Dashboard ────────────────────────────────────────────────
        <HttpGet("dashboard")>
        Public Async Function GetDashboard() As Task(Of IActionResult)
            Dim totalUsers = Await _mongoService.Users.CountDocumentsAsync(Builders(Of User).Filter.Empty)
            Dim currentUsers = Await _mongoService.Users.CountDocumentsAsync(Builders(Of User).Filter.Eq(Function(u) u.IsActive, True))
            
            Dim totalBookings = Await _mongoService.Bookings.CountDocumentsAsync(Builders(Of Booking).Filter.Empty)
            Dim confirmedBookings = Await _mongoService.Bookings.CountDocumentsAsync(Builders(Of Booking).Filter.Eq(Function(b) b.Status, "Confirmed"))
            Dim pendingBookings = Await _mongoService.Bookings.CountDocumentsAsync(Builders(Of Booking).Filter.Eq(Function(b) b.Status, "Pending"))
            Dim completedBookings = Await _mongoService.Bookings.CountDocumentsAsync(Builders(Of Booking).Filter.Eq(Function(b) b.Status, "Completed"))

            Dim paidFilter = Builders(Of Payment).Filter.Eq(Function(p) p.Status, "Paid")
            Dim paidPayments = Await _mongoService.Payments.Find(paidFilter).ToListAsync()
            Dim totalRevenue = paidPayments.Sum(Function(p) p.Amount)

            Dim pendingFilter = Builders(Of Payment).Filter.Eq(Function(p) p.Status, "Pending")
            Dim pendingPaymentsList = Await _mongoService.Payments.Find(pendingFilter).ToListAsync()
            Dim pendingRevenue = pendingPaymentsList.Sum(Function(p) p.Amount)
            Dim pendingPayments = pendingPaymentsList.Count
            
            Dim rawRecentBookings = Await _mongoService.Bookings.Find(Builders(Of Booking).Filter.Empty) _
                .SortByDescending(Function(b) b.CreatedAt).Limit(5).ToListAsync()

            Dim studios = Await _mongoService.Studios.Find(Builders(Of Studio).Filter.Empty).ToListAsync()
            Dim users = Await _mongoService.Users.Find(Builders(Of User).Filter.Empty).ToListAsync()

            Dim recentBookingsWithDetails = rawRecentBookings.Select(Function(b)
                Dim u = users.FirstOrDefault(Function(x) x.Id = b.UserId)
                Dim s = studios.FirstOrDefault(Function(x) x.Id = b.StudioId)
                Return New With {
                    Key .Id = b.Id,
                    Key .UserId = b.UserId,
                    Key .UserName = If(u IsNot Nothing, u.Name, "User"),
                    Key .UserEmail = If(u IsNot Nothing, u.Email, ""),
                    Key .StudioId = b.StudioId,
                    Key .StudioName = If(s IsNot Nothing, s.Name, "Studio"),
                    Key .Date = b.Date,
                    Key .StartTime = b.StartTime,
                    Key .EndTime = b.EndTime,
                    Key .TotalAmount = b.TotalAmount,
                    Key .Status = b.Status,
                    Key .PaymentStatus = b.PaymentStatus,
                    Key .CreatedAt = b.CreatedAt
                }
            End Function).ToList()

            Dim data = New With {
                .TotalUsers = totalUsers,
                .CurrentUsers = currentUsers,
                .TotalBookings = totalBookings,
                .ConfirmedBookings = confirmedBookings,
                .PendingBookings = pendingBookings,
                .CompletedBookings = completedBookings,
                .TotalRevenue = totalRevenue,
                .PendingRevenue = pendingRevenue,
                .PendingPayments = pendingPayments,
                .RecentBookings = recentBookingsWithDetails
            }
            Return Ok(New ApiResponse(Of Object)(True, "Dashboard data", data))
        End Function

        ' ── Settings ─────────────────────────────────────────────────
        <HttpGet("settings")>
        Public Async Function GetSettings() As Task(Of IActionResult)
            Dim settings = Await _mongoService.Settings.Find(Builders(Of Settings).Filter.Empty).FirstOrDefaultAsync()
            If settings Is Nothing Then settings = New Settings()
            Return Ok(New ApiResponse(Of Object)(True, "Settings retrieved", settings))
        End Function

        <HttpPut("settings")>
        Public Async Function UpdateSettings(<FromBody> dto As Settings) As Task(Of IActionResult)
            Dim existing = Await _mongoService.Settings.Find(Builders(Of Settings).Filter.Empty).FirstOrDefaultAsync()
            If existing Is Nothing Then
                dto.Id = Nothing
                Await _mongoService.Settings.InsertOneAsync(dto)
            Else
                dto.Id = existing.Id
                Dim filter = Builders(Of Settings).Filter.Eq(Function(s) s.Id, existing.Id)
                Await _mongoService.Settings.ReplaceOneAsync(filter, dto)
            End If
            Return Ok(New ApiResponse(Of Object)(True, "Settings saved"))
        End Function

        ' ── Seed ─────────────────────────────────────────────────────
        <HttpGet("seed")>
        <AllowAnonymous>
        Public Async Function SeedData() As Task(Of IActionResult)
            Dim adminFilter = Builders(Of User).Filter.Eq(Function(u) u.Email, "admin@studio.com")
            Dim adminCount = Await _mongoService.Users.CountDocumentsAsync(adminFilter)
            If adminCount > 0 Then
                Return Ok(New ApiResponse(Of Object)(True, "Database already seeded. Admin: admin@studio.com / admin123"))
            End If

            Await _mongoService.Users.InsertOneAsync(New User With {
                .Name = "Admin User",
                .Email = "admin@studio.com",
                .PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                .Role = "ADMIN",
                .IsActive = True,
                .CreatedAt = DateTime.UtcNow
            })

            Await _mongoService.Studios.InsertManyAsync(New List(Of Studio) From {
                New Studio With {
                    .Name = "Studio A - The White Room",
                    .Description = "Sunlit minimalist space with natural south-facing light and cyclorama wall.",
                    .Image = "https://images.unsplash.com/photo-1621784563330-caee0b138a00?w=1000&q=80",
                    .Price = 150D,
                    .Capacity = 10,
                    .IsActive = True
                },
                New Studio With {
                    .Name = "Studio B - The Dark Room",
                    .Description = "Blackout studio with overhead tube rigging, acoustically treated for video & audio.",
                    .Image = "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=1000&q=80",
                    .Price = 200D,
                    .Capacity = 25,
                    .IsActive = True
                }
            })

            Await _mongoService.Services.InsertManyAsync(New List(Of Service) From {
                New Service With {
                    .Name = "Photography",
                    .Description = "Professional studio photography sessions for portraits, fashion, and editorials.",
                    .Image = "https://images.unsplash.com/photo-1554048612-b6a482bc67e5?w=600&q=80",
                    .Price = 100D,
                    .Duration = 60,
                    .IsActive = True
                },
                New Service With {
                    .Name = "Videography",
                    .Description = "High definition 4K cinematic video production with pro lighting and stabilization.",
                    .Image = "https://images.unsplash.com/photo-1598300042247-d088f8ab3a91?w=600&q=80",
                    .Price = 200D,
                    .Duration = 120,
                    .IsActive = True
                },
                New Service With {
                    .Name = "Studio Rental",
                    .Description = "Private studio access for your own team, equipment, and production crew.",
                    .Image = "https://images.unsplash.com/photo-1621784563330-caee0b138a00?w=600&q=80",
                    .Price = 80D,
                    .Duration = 60,
                    .IsActive = True
                },
                New Service With {
                    .Name = "Product Shoot",
                    .Description = "Crisp, studio-lit commercial product photography and tabletop staging.",
                    .Image = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&q=80",
                    .Price = 150D,
                    .Duration = 90,
                    .IsActive = True
                }
            })

            Return Ok(New ApiResponse(Of Object)(True, "Seed complete! Admin: admin@studio.com / admin123"))
        End Function
    End Class
End Namespace

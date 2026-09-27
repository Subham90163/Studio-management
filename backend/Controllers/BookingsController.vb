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
    <Authorize>
    Public Class BookingsController
        Inherits ControllerBase

        Private ReadOnly _mongoService As MongoDbService

        Public Sub New(mongoService As MongoDbService)
            _mongoService = mongoService
        End Sub

        <HttpGet>
        Public Async Function GetMyBookings() As Task(Of IActionResult)
            Try
                Dim userId = MyBase.User.FindFirstValue(ClaimTypes.NameIdentifier)
                Dim filter = Builders(Of Booking).Filter.Eq(Function(b) b.UserId, userId)
                Dim bookings = Await _mongoService.Bookings.Find(filter).SortByDescending(Function(b) b.CreatedAt).ToListAsync()

                ' Fetch studios & services to enrich booking info for the user
                Dim studioIds = bookings.Select(Function(b) b.StudioId).Where(Function(id) Not String.IsNullOrEmpty(id)).Distinct().ToList()
                Dim allStudios = Await _mongoService.Studios.Find(Builders(Of Studio).Filter.In(Function(s) s.Id, studioIds)).ToListAsync()
                Dim studioMap = allStudios.ToDictionary(Function(s) s.Id, Function(s) s.Name)

                Dim allServices = Await _mongoService.Services.Find(Builders(Of Service).Filter.Empty).ToListAsync()
                Dim serviceMap = allServices.ToDictionary(Function(s) s.Id, Function(s) s)

                Dim enriched = bookings.Select(Function(b)
                    Dim sName = If(Not String.IsNullOrEmpty(b.StudioId) AndAlso studioMap.ContainsKey(b.StudioId), studioMap(b.StudioId), "Studio Space")
                    Dim svcNames As New List(Of String)()
                    If b.ServiceIds IsNot Nothing AndAlso b.ServiceIds.Count > 0 Then
                        For Each sId In b.ServiceIds
                            If serviceMap.ContainsKey(sId) Then
                                svcNames.Add(serviceMap(sId).Name)
                            End If
                        Next
                    ElseIf Not String.IsNullOrEmpty(b.ServiceId) AndAlso serviceMap.ContainsKey(b.ServiceId) Then
                        svcNames.Add(serviceMap(b.ServiceId).Name)
                    End If

                    Return New With {
                        Key .Id = b.Id,
                        Key .UserId = b.UserId,
                        Key .StudioId = b.StudioId,
                        Key .StudioName = sName,
                        Key .ServiceId = b.ServiceId,
                        Key .ServiceIds = b.ServiceIds,
                        Key .ServicesBooked = svcNames,
                        Key .Date = b.Date,
                        Key .StartTime = b.StartTime,
                        Key .EndTime = b.EndTime,
                        Key .TotalAmount = b.TotalAmount,
                        Key .PaymentStatus = b.PaymentStatus,
                        Key .Status = b.Status,
                        Key .CreatedAt = b.CreatedAt
                    }
                End Function).ToList()

                Return Ok(New ApiResponse(Of Object)(True, "Bookings retrieved successfully", enriched))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function

        <HttpPost>
        Public Async Function CreateBooking(<FromBody> request As CreateBookingRequest) As Task(Of IActionResult)
            Try
                Dim userId = MyBase.User.FindFirstValue(ClaimTypes.NameIdentifier)
                
                Dim studioFilter = Builders(Of Studio).Filter.Eq(Function(s) s.Id, request.StudioId)
                Dim studio = Await _mongoService.Studios.Find(studioFilter).FirstOrDefaultAsync()
                If studio Is Nothing Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Studio not found"))
                End If

                ' Calculate hours
                Dim startT = TimeSpan.Parse(request.StartTime)
                Dim endT = TimeSpan.Parse(request.EndTime)
                Dim hours = (endT - startT).TotalHours
                
                If hours <= 0 Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Invalid time range. End time must be after start time."))
                End If

                ' Base studio cost: hours * studio.Price
                Dim studioTotal = Convert.ToDecimal(hours) * studio.Price

                ' Consolidate service IDs
                Dim selectedSvcIds As New List(Of String)()
                If request.ServiceIds IsNot Nothing AndAlso request.ServiceIds.Count > 0 Then
                    selectedSvcIds = request.ServiceIds.Distinct().ToList()
                ElseIf Not String.IsNullOrEmpty(request.ServiceId) Then
                    selectedSvcIds.Add(request.ServiceId)
                End If

                ' Calculate services total: OneTime adds once, PerHour scales with hours
                Dim servicesTotal As Decimal = 0D
                If selectedSvcIds.Count > 0 Then
                    Dim svcFilter = Builders(Of Service).Filter.In(Function(s) s.Id, selectedSvcIds)
                    Dim foundServices = Await _mongoService.Services.Find(svcFilter).ToListAsync()
                    For Each svc In foundServices
                        If String.Equals(svc.PriceType, "PerHour", StringComparison.OrdinalIgnoreCase) Then
                            servicesTotal += Convert.ToDecimal(hours) * svc.Price
                        Else
                            servicesTotal += svc.Price
                        End If
                    Next
                End If

                Dim totalAmount = studioTotal + servicesTotal

                ' Check conflict: same studio, same date, not cancelled, and intervals overlap
                Dim fb = Builders(Of Booking).Filter
                Dim conflictFilter = fb.And(
                    fb.Eq(Function(b) b.StudioId, request.StudioId),
                    fb.Eq(Function(b) b.[Date], request.[Date]),
                    fb.Ne(Function(b) b.Status, "Cancelled"),
                    fb.Lt(Function(b) b.StartTime, request.EndTime),
                    fb.Gt(Function(b) b.EndTime, request.StartTime)
                )

                Dim conflictingBookings = Await _mongoService.Bookings.Find(conflictFilter).ToListAsync()

                If conflictingBookings.Count > 0 Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Studio is already booked for this time"))
                End If

                Dim newBooking As New Booking With {
                    .UserId = userId,
                    .StudioId = request.StudioId,
                    .ServiceId = If(selectedSvcIds.Count > 0, selectedSvcIds(0), Nothing),
                    .ServiceIds = selectedSvcIds,
                    .[Date] = request.[Date],
                    .StartTime = request.StartTime,
                    .EndTime = request.EndTime,
                    .TotalAmount = totalAmount,
                    .Status = "Pending",
                    .PaymentStatus = "Pending",
                    .CreatedAt = DateTime.UtcNow
                }

                Await _mongoService.Bookings.InsertOneAsync(newBooking)

                Dim newPayment As New Payment With {
                    .BookingId = newBooking.Id,
                    .UserId = userId,
                    .Amount = totalAmount,
                    .Status = "Pending",
                    .CreatedAt = DateTime.UtcNow
                }

                Await _mongoService.Payments.InsertOneAsync(newPayment)

                Return Ok(New ApiResponse(Of Object)(True, "Booking created successfully", newBooking))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function

        <HttpPut("{id}/cancel")>
        Public Async Function CancelBooking(id As String) As Task(Of IActionResult)
            Try
                Dim userId = MyBase.User.FindFirstValue(ClaimTypes.NameIdentifier)
                Dim fb = Builders(Of Booking).Filter
                Dim filter = fb.And(fb.Eq(Function(b) b.Id, id), fb.Eq(Function(b) b.UserId, userId))
                Dim booking = Await _mongoService.Bookings.Find(filter).FirstOrDefaultAsync()
                
                If booking Is Nothing Then
                    Return NotFound(New ApiResponse(Of Object)(False, "Booking not found"))
                End If

                If booking.Status <> "Pending" AndAlso booking.Status <> "Confirmed" Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Cannot cancel booking in current status"))
                End If

                Dim update = Builders(Of Booking).Update.Set(Function(b) b.Status, "Cancelled")
                Await _mongoService.Bookings.UpdateOneAsync(filter, update)

                ' If payment exists and was pending, mark failed/cancelled
                Dim pFilter = Builders(Of Payment).Filter.And(
                    Builders(Of Payment).Filter.Eq(Function(p) p.BookingId, id),
                    Builders(Of Payment).Filter.Eq(Function(p) p.Status, "Pending")
                )
                Dim pUpdate = Builders(Of Payment).Update.Set(Function(p) p.Status, "Failed")
                Await _mongoService.Payments.UpdateOneAsync(pFilter, pUpdate)

                Return Ok(New ApiResponse(Of Object)(True, "Booking cancelled successfully"))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function
    End Class
End Namespace

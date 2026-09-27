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
    Public Class PaymentsController
        Inherits ControllerBase

        Private ReadOnly _mongoService As MongoDbService

        Public Sub New(mongoService As MongoDbService)
            _mongoService = mongoService
        End Sub

        <HttpGet>
        Public Async Function GetMyPayments() As Task(Of IActionResult)
            Try
                Dim userId = MyBase.User.FindFirstValue(ClaimTypes.NameIdentifier)
                Dim filter = Builders(Of Payment).Filter.Eq(Function(p) p.UserId, userId)
                Dim payments = Await _mongoService.Payments.Find(filter).SortByDescending(Function(p) p.CreatedAt).ToListAsync()
                Return Ok(New ApiResponse(Of Object)(True, "Payments retrieved successfully", payments))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function

        <HttpPost("{id}/pay")>
        Public Async Function Pay(id As String) As Task(Of IActionResult)
            Try
                Dim userId = MyBase.User.FindFirstValue(ClaimTypes.NameIdentifier)
                Dim fb = Builders(Of Payment).Filter
                Dim filter = fb.And(fb.Eq(Function(p) p.Id, id), fb.Eq(Function(p) p.UserId, userId))
                Dim payment = Await _mongoService.Payments.Find(filter).FirstOrDefaultAsync()
                If payment Is Nothing Then
                    Return NotFound(New ApiResponse(Of Object)(False, "Payment record not found"))
                End If

                If payment.Status = "Paid" Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Payment is already marked as paid."))
                End If

                Dim update = Builders(Of Payment).Update.Set(Function(p) p.Status, "Paid")
                Await _mongoService.Payments.UpdateOneAsync(filter, update)

                If Not String.IsNullOrEmpty(payment.BookingId) Then
                    Dim bFilter = Builders(Of Booking).Filter.Eq(Function(b) b.Id, payment.BookingId)
                    Dim bUpdate = Builders(Of Booking).Update _
                        .Set(Function(b) b.PaymentStatus, "Paid") _
                        .Set(Function(b) b.Status, "Confirmed")
                    Await _mongoService.Bookings.UpdateOneAsync(bFilter, bUpdate)
                End If

                Return Ok(New ApiResponse(Of Object)(True, "Payment of ₹" & payment.Amount.ToString("F2") & " completed successfully!"))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function
    End Class
End Namespace

Imports Microsoft.AspNetCore.Mvc
Imports MongoDB.Driver
Imports StudioAPI.DTOs
Imports StudioAPI.Models
Imports StudioAPI.Services

Namespace Controllers
    <Route("api/[controller]")>
    <ApiController>
    Public Class ServicesController
        Inherits ControllerBase

        Private ReadOnly _mongoService As MongoDbService

        Public Sub New(mongoService As MongoDbService)
            _mongoService = mongoService
        End Sub

        <HttpGet>
        Public Async Function GetServices() As Task(Of IActionResult)
            Try
                Dim services = Await _mongoService.Services.Find(Function(s) s.IsActive).ToListAsync()
                Return Ok(New ApiResponse(Of Object)(True, "Services retrieved successfully", services))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function
    End Class
End Namespace

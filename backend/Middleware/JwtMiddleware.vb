Imports Microsoft.AspNetCore.Http
Imports System.Threading.Tasks

Namespace Middleware
    Public Class JwtMiddleware
        Private ReadOnly _next As RequestDelegate

        Public Sub New(nextDelegate As RequestDelegate)
            _next = nextDelegate
        End Sub

        Public Async Function Invoke(context As HttpContext) As Task
            ' Custom JWT logic can go here. For now, relying on [Authorize] attribute.
            Await _next(context)
        End Function
    End Class
End Namespace

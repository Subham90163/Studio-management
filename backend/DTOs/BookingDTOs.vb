Namespace DTOs
    Public Class CreateBookingRequest
        Public Property StudioId As String
        Public Property ServiceId As String
        Public Property ServiceIds As List(Of String)
        Public Property [Date] As DateTime
        Public Property StartTime As String
        Public Property EndTime As String
    End Class
End Namespace

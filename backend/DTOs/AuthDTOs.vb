Namespace DTOs
    Public Class ApiResponse(Of T)
        Public Property Success As Boolean
        Public Property Message As String
        Public Property Data As T

        Public Sub New(success As Boolean, message As String, Optional data As T = Nothing)
            Me.Success = success
            Me.Message = message
            Me.Data = data
        End Sub
    End Class

    Public Class RegisterRequest
        Public Property Name As String
        Public Property Email As String
        Public Property Phone As String
        Public Property Password As String
    End Class

    Public Class LoginRequest
        Public Property Email As String
        Public Property Password As String
    End Class

    Public Class UpdateProfileRequest
        Public Property Name As String
        Public Property Phone As String
        Public Property Password As String
    End Class

    Public Class ResetPasswordRequest
        Public Property Email As String
        Public Property Phone As String
        Public Property NewPassword As String
    End Class

    Public Class CreateUserRequest
        Public Property Name As String
        Public Property Email As String
        Public Property Phone As String
        Public Property Password As String
        Public Property Role As String
    End Class
End Namespace

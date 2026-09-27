Namespace DTOs
    Public Class ServiceDTO
        Public Property Id As String
        Public Property Name As String
        Public Property Description As String
        Public Property Image As String
        Public Property Price As Decimal
        Public Property PriceType As String = "OneTime"
        Public Property Duration As Integer
        Public Property IsActive As Boolean
    End Class
End Namespace

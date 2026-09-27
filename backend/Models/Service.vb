Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Namespace Models
    Public Class Service
        <BsonId>
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property Id As String
        
        Public Property Name As String
        Public Property Description As String
        Public Property Image As String
        Public Property Price As Decimal
        Public Property PriceType As String = "OneTime" ' "OneTime" or "PerHour"
        Public Property Duration As Integer ' in minutes
        Public Property IsActive As Boolean = True
    End Class
End Namespace

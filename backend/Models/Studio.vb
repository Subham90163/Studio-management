Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Namespace Models
    Public Class Studio
        <BsonId>
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property Id As String
        
        Public Property Name As String
        Public Property Description As String
        Public Property Image As String
        Public Property Capacity As Integer
        Public Property Price As Decimal ' per hour
        Public Property IsActive As Boolean = True
    End Class
End Namespace

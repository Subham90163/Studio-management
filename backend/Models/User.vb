Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Namespace Models
    Public Class User
        <BsonId>
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property Id As String
        
        Public Property Name As String
        Public Property Email As String
        Public Property Phone As String
        Public Property PasswordHash As String
        Public Property Role As String = "USER" ' USER or ADMIN
        Public Property IsActive As Boolean = True
        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class
End Namespace

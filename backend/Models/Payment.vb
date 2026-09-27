Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Namespace Models
    Public Class Payment
        <BsonId>
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property Id As String
        
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property BookingId As String
        
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property UserId As String
        
        Public Property Amount As Decimal
        Public Property TransactionId As String
        Public Property Status As String = "Pending" ' Pending, Paid, Failed
        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class
End Namespace

Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Namespace Models
    Public Class Booking
        <BsonId>
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property Id As String
        
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property UserId As String
        
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property StudioId As String
        
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property ServiceId As String
        
        Public Property ServiceIds As List(Of String) = New List(Of String)()
        
        <BsonElement("date")>
        Public Property [Date] As DateTime

        Public Property StartTime As String
        Public Property EndTime As String
        Public Property TotalAmount As Decimal
        Public Property PaymentStatus As String = "Pending" ' Pending, Paid, Failed
        Public Property Status As String = "Pending" ' Pending, Confirmed, Completed, Cancelled
        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class
End Namespace

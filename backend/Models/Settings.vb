Imports MongoDB.Bson
Imports MongoDB.Bson.Serialization.Attributes

Namespace Models
    Public Class Settings
        <BsonId>
        <BsonRepresentation(BsonType.ObjectId)>
        Public Property Id As String
        Public Property StudioName As String = "STUDIO"
        Public Property Email As String = "hello@studio.com"
        Public Property Phone As String = ""
        Public Property Address As String = ""
    End Class
End Namespace

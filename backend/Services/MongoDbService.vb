Imports StudioAPI.Models

Namespace Services
    Public Class MongoDbService
        Private ReadOnly _database As MongoDB.Driver.IMongoDatabase

        Public Sub New(configuration As Microsoft.Extensions.Configuration.IConfiguration)
            Dim client = New MongoDB.Driver.MongoClient(configuration("MongoDB:ConnectionString"))
            _database = client.GetDatabase(configuration("MongoDB:DatabaseName"))
        End Sub

        Public ReadOnly Property Users As MongoDB.Driver.IMongoCollection(Of User)
            Get
                Return _database.GetCollection(Of User)("Users")
            End Get
        End Property

        Public ReadOnly Property Studios As MongoDB.Driver.IMongoCollection(Of Studio)
            Get
                Return _database.GetCollection(Of Studio)("Studios")
            End Get
        End Property

        Public ReadOnly Property Services As MongoDB.Driver.IMongoCollection(Of Service)
            Get
                Return _database.GetCollection(Of Service)("Services")
            End Get
        End Property

        Public ReadOnly Property Bookings As MongoDB.Driver.IMongoCollection(Of Booking)
            Get
                Return _database.GetCollection(Of Booking)("Bookings")
            End Get
        End Property

        Public ReadOnly Property Payments As MongoDB.Driver.IMongoCollection(Of Payment)
            Get
                Return _database.GetCollection(Of Payment)("Payments")
            End Get
        End Property

        Public ReadOnly Property Settings As MongoDB.Driver.IMongoCollection(Of Settings)
            Get
                Return _database.GetCollection(Of Settings)("Settings")
            End Get
        End Property
    End Class
End Namespace

Imports Microsoft.AspNetCore.Authentication.JwtBearer
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.IdentityModel.Tokens
Imports System.Text
Imports MongoDB.Driver
Imports StudioAPI.Services

Module Program
    Sub Main(args As String())
        Dim builder = WebApplication.CreateBuilder(args)

        ' Add services to the container.
        builder.Services.AddControllers()

        ' Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer()
        builder.Services.AddSwaggerGen()

        ' Configure MongoDB
        builder.Services.AddSingleton(Of MongoDbService)()

        ' Configure JWT Service
        builder.Services.AddScoped(Of JwtService)()

        ' Configure CORS
        builder.Services.AddCors(Sub(options)
            options.AddPolicy("AllowFrontend", Sub(policyBuilder)
                policyBuilder.WithOrigins("http://localhost:5173") _
                             .AllowAnyHeader() _
                             .AllowAnyMethod()
            End Sub)
        End Sub)

        ' Configure JWT Authentication
        Dim jwtSecret = builder.Configuration("Jwt:Secret")
        Dim key = Encoding.UTF8.GetBytes(jwtSecret)

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) _
            .AddJwtBearer(Sub(options)
                options.TokenValidationParameters = New TokenValidationParameters With {
                    .ValidateIssuer = True,
                    .ValidateAudience = True,
                    .ValidateLifetime = True,
                    .ValidateIssuerSigningKey = True,
                    .ValidIssuer = builder.Configuration("Jwt:Issuer"),
                    .ValidAudience = builder.Configuration("Jwt:Audience"),
                    .IssuerSigningKey = New SymmetricSecurityKey(key)
                }
            End Sub)

        Dim app = builder.Build()

        ' Configure the HTTP request pipeline.
        If app.Environment.IsDevelopment() Then
            app.UseSwagger()
            app.UseSwaggerUI()
        End If

        app.UseCors("AllowFrontend")

        app.UseAuthentication()
        app.UseAuthorization()

        app.MapControllers()

        ' Ensure default admin exists
        SeedAdminUser(app)

        app.Run("http://localhost:5000")
    End Sub

    Private Sub SeedAdminUser(app As WebApplication)
        Try
            Using scope = app.Services.CreateScope()
                Dim mongo = scope.ServiceProvider.GetRequiredService(Of MongoDbService)()
                Dim adminFilter = MongoDB.Driver.Builders(Of Models.User).Filter.Eq(Function(u) u.Email, "admin@studio.com")
                Dim existingAdmin = mongo.Users.Find(adminFilter).FirstOrDefault()
                If existingAdmin Is Nothing Then
                    mongo.Users.InsertOne(New Models.User With {
                        .Name = "Admin User",
                        .Email = "admin@studio.com",
                        .PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        .Role = "ADMIN",
                        .IsActive = True,
                        .CreatedAt = DateTime.UtcNow
                    })
                End If
            End Using
        Catch ex As Exception
            Console.WriteLine($"Auto-seed check notice: {ex.Message}")
        End Try
    End Sub
End Module

Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc
Imports MongoDB.Driver
Imports StudioAPI.DTOs
Imports StudioAPI.Models
Imports StudioAPI.Services
Imports System.Security.Claims

Namespace Controllers
    <Route("api/[controller]")>
    <ApiController>
    Public Class AuthController
        Inherits ControllerBase

        Private ReadOnly _mongoService As MongoDbService
        Private ReadOnly _jwtService As JwtService

        Public Sub New(mongoService As MongoDbService, jwtService As JwtService)
            _mongoService = mongoService
            _jwtService = jwtService
        End Sub

        <HttpPost("register")>
        Public Async Function Register(<FromBody> request As RegisterRequest) As Task(Of IActionResult)
            Try
                If String.IsNullOrWhiteSpace(request.Name) OrElse
                   String.IsNullOrWhiteSpace(request.Email) OrElse
                   String.IsNullOrWhiteSpace(request.Password) Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Name, email, and password are required."))
                End If

                If String.IsNullOrWhiteSpace(request.Phone) Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Phone number is mandatory."))
                End If

                Dim filter = Builders(Of User).Filter.Eq(Function(u) u.Email, request.Email.Trim().ToLower())
                Dim existingUser = Await _mongoService.Users.Find(filter).FirstOrDefaultAsync()
                If existingUser IsNot Nothing Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "User with this email already exists."))
                End If

                Dim newUser As New User With {
                    .Name = request.Name.Trim(),
                    .Email = request.Email.Trim().ToLower(),
                    .Phone = request.Phone.Trim(),
                    .PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    .Role = "USER",
                    .IsActive = True,
                    .CreatedAt = DateTime.UtcNow
                }

                Await _mongoService.Users.InsertOneAsync(newUser)

                Dim token = _jwtService.GenerateToken(newUser)

                Return Ok(New ApiResponse(Of Object)(True, "Registration successful", New With {
                    Key .Token = token,
                    Key .User = New With { newUser.Id, newUser.Name, newUser.Email, newUser.Phone, newUser.Role }
                }))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function

        <HttpPost("login")>
        Public Async Function Login(<FromBody> request As LoginRequest) As Task(Of IActionResult)
            Try
                Dim filter = Builders(Of User).Filter.Eq(Function(u) u.Email, request.Email)
                Dim dbUser = Await _mongoService.Users.Find(filter).FirstOrDefaultAsync()
                
                If dbUser Is Nothing OrElse Not BCrypt.Net.BCrypt.Verify(request.Password, dbUser.PasswordHash) Then
                    Return Unauthorized(New ApiResponse(Of Object)(False, "Invalid email or password."))
                End If

                If Not dbUser.IsActive Then
                    Return Unauthorized(New ApiResponse(Of Object)(False, "Account is disabled."))
                End If

                Dim token = _jwtService.GenerateToken(dbUser)

                Return Ok(New ApiResponse(Of Object)(True, "Login successful", New With {
                    Key .Token = token,
                    Key .User = New With { dbUser.Id, dbUser.Name, dbUser.Email, dbUser.Phone, dbUser.Role }
                }))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function

        <Authorize>
        <HttpGet("profile")>
        Public Async Function GetProfile() As Task(Of IActionResult)
            Try
                Dim currentUserId = MyBase.User.FindFirstValue(ClaimTypes.NameIdentifier)
                Dim filter = Builders(Of User).Filter.Eq(Function(u) u.Id, currentUserId)
                Dim dbUser = Await _mongoService.Users.Find(filter).FirstOrDefaultAsync()
                If dbUser Is Nothing Then
                    Return NotFound(New ApiResponse(Of Object)(False, "User not found"))
                End If

                Return Ok(New ApiResponse(Of Object)(True, "Profile retrieved", New With {
                    dbUser.Id,
                    dbUser.Name,
                    dbUser.Email,
                    dbUser.Phone,
                    dbUser.Role
                }))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function

        <Authorize>
        <HttpPut("profile")>
        Public Async Function UpdateProfile(<FromBody> request As UpdateProfileRequest) As Task(Of IActionResult)
            Try
                Dim currentUserId = MyBase.User.FindFirstValue(ClaimTypes.NameIdentifier)
                Dim filter = Builders(Of User).Filter.Eq(Function(u) u.Id, currentUserId)
                Dim dbUser = Await _mongoService.Users.Find(filter).FirstOrDefaultAsync()
                If dbUser Is Nothing Then
                    Return NotFound(New ApiResponse(Of Object)(False, "User not found"))
                End If

                If String.IsNullOrWhiteSpace(request.Name) Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Name is required."))
                End If

                If String.IsNullOrWhiteSpace(request.Phone) Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Phone number is mandatory."))
                End If

                Dim updates As New List(Of UpdateDefinition(Of User))()
                updates.Add(Builders(Of User).Update.Set(Function(u) u.Name, request.Name.Trim()))
                updates.Add(Builders(Of User).Update.Set(Function(u) u.Phone, request.Phone.Trim()))

                If Not String.IsNullOrWhiteSpace(request.Password) Then
                    If request.Password.Length < 6 Then
                        Return BadRequest(New ApiResponse(Of Object)(False, "Password must be at least 6 characters."))
                    End If
                    updates.Add(Builders(Of User).Update.Set(Function(u) u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(request.Password)))
                End If

                Dim combinedUpdate = Builders(Of User).Update.Combine(updates)
                Await _mongoService.Users.UpdateOneAsync(filter, combinedUpdate)
                
                Dim updatedUser = Await _mongoService.Users.Find(filter).FirstOrDefaultAsync()

                Return Ok(New ApiResponse(Of Object)(True, "Profile updated successfully", New With {
                    updatedUser.Id,
                    updatedUser.Name,
                    updatedUser.Email,
                    updatedUser.Phone,
                    updatedUser.Role
                }))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function

        <HttpPost("reset-password")>
        Public Async Function ResetPassword(<FromBody> request As ResetPasswordRequest) As Task(Of IActionResult)
            Try
                If String.IsNullOrWhiteSpace(request.Email) OrElse
                   String.IsNullOrWhiteSpace(request.Phone) OrElse
                   String.IsNullOrWhiteSpace(request.NewPassword) Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Email, phone number, and new password are required."))
                End If

                Dim normalizedEmail = request.Email.Trim().ToLower()
                Dim trimmedPhone = request.Phone.Trim()

                Dim filter = Builders(Of User).Filter.Eq(Function(u) u.Email, normalizedEmail)
                Dim dbUser = Await _mongoService.Users.Find(filter).FirstOrDefaultAsync()
                If dbUser Is Nothing Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "User with this email and phone does not exist."))
                End If

                Dim cleanDbPhone = System.Text.RegularExpressions.Regex.Replace(If(dbUser.Phone, ""), "[^\d]", "")
                Dim cleanReqPhone = System.Text.RegularExpressions.Regex.Replace(trimmedPhone, "[^\d]", "")
                If String.IsNullOrEmpty(cleanDbPhone) OrElse cleanDbPhone <> cleanReqPhone Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Phone number does not match registered account details."))
                End If

                If request.NewPassword.Length < 6 Then
                    Return BadRequest(New ApiResponse(Of Object)(False, "Password must be at least 6 characters."))
                End If

                Dim update = Builders(Of User).Update.Set(Function(u) u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(request.NewPassword))
                Await _mongoService.Users.UpdateOneAsync(Builders(Of User).Filter.Eq(Function(u) u.Id, dbUser.Id), update)

                Return Ok(New ApiResponse(Of Object)(True, "Password reset successfully. You can now log in with your new password."))
            Catch ex As Exception
                Return StatusCode(500, New ApiResponse(Of Object)(False, ex.Message))
            End Try
        End Function
    End Class
End Namespace

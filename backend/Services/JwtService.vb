Imports System.IdentityModel.Tokens.Jwt
Imports System.Security.Claims
Imports Microsoft.Extensions.Configuration
Imports Microsoft.IdentityModel.Tokens
Imports StudioAPI.Models

Namespace Services
    Public Class JwtService
        Private ReadOnly _config As IConfiguration

        Public Sub New(config As IConfiguration)
            _config = config
        End Sub

        Public Function GenerateToken(user As User) As String
            Dim securityKey = New SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config("Jwt:Secret")))
            Dim credentials = New SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)

            Dim claims = New List(Of Claim) From {
                New Claim(JwtRegisteredClaimNames.Sub, user.Id),
                New Claim(JwtRegisteredClaimNames.Email, user.Email),
                New Claim(ClaimTypes.Role, user.Role),
                New Claim("name", user.Name)
            }

            Dim expiryDays = Integer.Parse(_config("Jwt:ExpiryDays"))

            Dim token = New JwtSecurityToken(
                issuer:=_config("Jwt:Issuer"),
                audience:=_config("Jwt:Audience"),
                claims:=claims,
                expires:=DateTime.Now.AddDays(expiryDays),
                signingCredentials:=credentials)

            Return New JwtSecurityTokenHandler().WriteToken(token)
        End Function
    End Class
End Namespace

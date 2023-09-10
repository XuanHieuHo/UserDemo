using Azure;
using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using UserDemo.Data;
using UserDemo.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace UserDemo.Controllers
{
    [Route("api")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly TokenBlacklist _tokenBlacklist;
        private readonly MyDbContext _context;
        private readonly AppSetting _appSettings;
        private List<string> _revokedAccessTokens = new List<string>();


        public UserController(MyDbContext context, IOptionsMonitor<AppSetting> optionsMonitor)
        {
            _context = context;
            _appSettings = optionsMonitor.CurrentValue;
        }


        
        [HttpPost("Login")]
        public IActionResult Validate(LoginModel model)
        {
            var user = _context.NguoiDungs.SingleOrDefault(p => p.UserName == model.UserName && model.Password == p.Password);
            if (user == null) //không đúng
            {
                return Ok(new ApiResponse
                {
                    Success = false,
                    Message = "Invalid username/password"
                });
            }

            //cấp token


            var refresh_token = GenerateToken(user, "6");
            var access_token = GenerateToken(user, "1");

            var session = new Session
            {
                UserNameId = user.Id,
                RefreshToken = refresh_token.token,
                ExpiresAt = refresh_token.payload.ExpiredAt.ToUniversalTime(),
            };

            _context.Add(session);
            _context.SaveChanges();

            LoginRes res = new LoginRes
            {
                SessionId = session.Id,
                AccessToken = access_token.token,
                HoTen = user.HoTen,
                Email = user.Email,
                UserName = user.UserName,
                RefreshToken = session.RefreshToken,
                ExpiresAt = session.ExpiresAt,
            };


            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Authenticate success",
                Data = res
            });
        }

        
        private Payload NewPayload(int usernameid, TimeSpan duration)
        {
            var tokenID = Guid.NewGuid();
            var issuedAt = DateTimeOffset.UtcNow;
            var expiredAt = DateTimeOffset.UtcNow.Add(duration);

            var payload = new Payload
            {
                Id = tokenID,
                UserNameId = usernameid.ToString(),
                IssuedAt = issuedAt,
                ExpiredAt = expiredAt
            };


            return payload;
        }

        private TokenRes GenerateToken(NguoiDung nguoiDung, string time)
        {

            var payload = new Payload();
            TimeSpan duration = TimeSpan.FromHours(double.Parse(time));

            payload = NewPayload(nguoiDung.Id, duration);
            string payloadJson = JsonConvert.SerializeObject(payload);
            Claim payloadClaim = new Claim("Payload", payloadJson);

            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var secretKeyBytes = Encoding.UTF8.GetBytes(_appSettings.SecretKey);

            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    payloadClaim
                }),
            
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKeyBytes), SecurityAlgorithms.HmacSha512Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescription);

            var tokenres = new TokenRes
            {
                payload = payload,
                token = jwtTokenHandler.WriteToken(token)
            };
            return tokenres;          
        }

        private Payload VerifyToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var keyBytes = Encoding.UTF8.GetBytes(_appSettings.SecretKey);

                var tokenValidationParameters = GetValidationParameters();

                SecurityToken securityToken;
                IPrincipal principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
     

                if (securityToken is JwtSecurityToken jwtSecurityToken && principal.Identity is ClaimsIdentity claimsIdentity)
                {
                    if (claimsIdentity.Claims.Any())
                    {
                        var payloadClaim = claimsIdentity.FindFirst("Payload");
                        if (payloadClaim != null)
                        {
                            string payloadJson = payloadClaim.Value;
                            var payload = JsonConvert.DeserializeObject<Payload>(payloadJson);
                            if (payload != null)
                            {
                                return payload;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi hoặc ghi nhật ký tại đây
                Console.WriteLine($"Lỗi xác minh token: {ex.Message}");
            }

            return null;
        }

        private TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters()
            {
                ValidateLifetime = false, // Because there is no expiration in the generated token
                ValidateAudience = false, // Because there is no audiance in the generated token
                ValidateIssuer = false,   // Because there is no issuer in the generated token
                ValidIssuer = "Sample",
                ValidAudience = "Sample",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.SecretKey)) // The same key as the one that generate the token
            };
        }

        [HttpPost("RenewAccessToken")]
        public IActionResult renewAccessToken(RenewAccessTokenRequest renewAccessTokenRequest)
        {
            var refreshPayload = new Payload();
            refreshPayload = VerifyToken(renewAccessTokenRequest.RefreshToken);
            if (refreshPayload == null)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Error message 1",
                });
            }

            var session = _context.Sessions
                    .Where(p => p.RefreshToken == renewAccessTokenRequest.RefreshToken)
                    .FirstOrDefault();
            if (session == null)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Error message 2",
                });
            }
            if (session.IsLocked)
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "blocked session",
                });
            }
            if (session.UserNameId.ToString() != refreshPayload.UserNameId)
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "incorrect session user",
                });
            }
            if (session.RefreshToken != renewAccessTokenRequest.RefreshToken)
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "mismatched session token",
                });
            }
            if (DateTime.Now > session.ExpiresAt)
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "mismatched session token",
                });
            }
            var user = new NguoiDung();
            user = _context.NguoiDungs.SingleOrDefault(p => p.Id.ToString() == refreshPayload.UserNameId.ToString());
            if (user == null)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "mismatched user id",
                });
            }
            var accessToken = GenerateToken(user, "1");
            if (accessToken == null)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "token failed",
                });
            }

            LoginRes res = new LoginRes
            {
                SessionId = session.Id,
                AccessToken = accessToken.token,
                HoTen = user.HoTen,
                Email = user.Email,
                UserName = user.UserName,
                RefreshToken = session.RefreshToken,
                ExpiresAt = session.ExpiresAt,
            };

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Authenticate success",
                Data = res
            });
        }


        [HttpPost("Register")]
        public IActionResult CreateNewUser(RegisterModel model)
        {
            try
            {
                var nguoidung = new NguoiDung
                {
                    Email = model.Email,
                    UserName = model.UserName,
                    HoTen = model.HoTen,
                    Password = model.Password,
                };
                _context.Add(nguoidung);
                _context.SaveChanges();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = nguoidung,
                    Message = "Add new user successfully"
                });;
            }  catch (Exception ex)
            {
                return Ok(new ApiResponse 
                { 
                    Success = false, 
                    Data = ex 
                });
            }
        }




        [HttpPost("logout")]
        public IActionResult Logout(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest("AccessToken is required.");
            }

            if (_revokedAccessTokens.Contains(accessToken))
            {
                return BadRequest("AccessToken is already revoked.");
            }

            // Thêm AccessToken vào danh sách bị vô hiệu hóa
            _revokedAccessTokens.Add(accessToken);

            return Ok("Logout successful.");
        }

    }
}

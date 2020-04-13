using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WhyNotEarth.Meredith.App.Auth;
using WhyNotEarth.Meredith.App.Configuration;
using WhyNotEarth.Meredith.App.Models.Api.v0.Authentication;
using WhyNotEarth.Meredith.App.Results.Api.v0.Public.Authentication;
using WhyNotEarth.Meredith.Data.Entity.Models;

namespace WhyNotEarth.Meredith.App.Controllers.Api.v0.Public
{
    [ApiVersion("0")]
    [Route("/api/v0/authentication")]
    public class AuthenticationController : ControllerBase
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly JwtOptions _jwtOptions;

        public AuthenticationController(UserManager<User> userManager, SignInManager<User> signInManager,
            IOptions<JwtOptions> jwtOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtOptions = jwtOptions.Value;
        }

        [HttpPost]
        [Route("login")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login(LoginModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, true, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new {error = "You have entered an invalid username or password"});
            }

            var appUser = await _userManager.Users.SingleOrDefaultAsync(r => r.Email == model.Email);
            return Ok(GenerateJwtToken(model.Email, appUser));
        }

        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return Ok();
        }

        [HttpGet]
        [Route("provider/login")]
        public IActionResult ProviderLogin(string provider, string returnUrl = null)
        {
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider,
                $"/api/v0/authentication/provider/callback?returnUrl={returnUrl}");

            return new ChallengeResult(provider, properties);
        }

        [HttpPost]
        [Route("provider/logout")]
        public async Task<IActionResult> ProviderLogout(string provider)
        {
            var user = await _userManager.GetUserAsync(User);
            var userLoginInfos = await _userManager.GetLoginsAsync(user);

            var userLoginInfo = userLoginInfos.FirstOrDefault(item => item.LoginProvider == provider);

            if (userLoginInfo is null)
            {
                return Ok();
            }

            await _userManager.RemoveLoginAsync(user, userLoginInfo.LoginProvider, userLoginInfo.ProviderKey);

            return Ok();
        }

        [HttpGet]
        [Route("provider/callback")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> ProviderCallback(string remoteError = null, string returnUrl = null)
        {
            if (remoteError != null)
            {
                return Unauthorized(new {error = $"Error from external provider: {remoteError}"});
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                // Login failed, typically because they cancelled.
                return Redirect(returnUrl);
            }

            if (!info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                return Unauthorized(new {error = "Provider did not return an e-mail address"});
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
                false, true);

            if (result.Succeeded)
            {
                await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
                return Redirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return Unauthorized(new {error = "User is locked out"});
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            User user;

            if (User.Identity.IsAuthenticated)
            {
                user = await _userManager.GetUserAsync(User);
            }
            else
            {
                user = await _userManager.FindByEmailAsync(email);
            }

            if (user is null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return Error("Error creating user", createResult.Errors);
                }
            }

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                return Error("Error adding login to user", addLoginResult.Errors);
            }

            await _signInManager.SignInAsync(user, true);
            return Redirect(returnUrl);
        }

        [HttpPost]
        [Route("register")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            var newUser = new User
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Name = model.Email,
                Address = model.Address,
                GoogleLocation = model.GoogleLocation
            };

            IdentityResult identityResult;
            if (model.Password is null)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user is null)
                {
                    identityResult = await _userManager.CreateAsync(newUser);
                }
                else
                {
                    // We only let users that registered without password and
                    // also don't have any other provider linked to login this way
                    if (user.PasswordHash is null)
                    {
                        var logins = await _userManager.GetLoginsAsync(user);
                        if (!logins.Any())
                        {
                            return await SignIn(user);
                        }
                    }

                    identityResult = IdentityResult.Failed(new IdentityErrorDescriber().DuplicateUserName(model.Email));
                }
            }
            else
            {
                identityResult = await _userManager.CreateAsync(newUser, model.Password);
            }

            if (!identityResult.Succeeded)
            {
                return BadRequest(identityResult.Errors);
            }

            return await SignIn(newUser);
        }

        [HttpGet]
        [Authorize]
        [Route("ping")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(List<PingResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Ping()
        {
            var user = await _userManager.GetUserAsync(User);
            var logins = await _userManager.GetLoginsAsync(user);

            return Ok(new PingResult(user.Id, user.UserName, User.Identity.IsAuthenticated,
                logins.Select(item => item.LoginProvider).ToList()));
        }

        private async Task<IActionResult> SignIn(User user)
        {
            await _signInManager.SignInAsync(user, true);

            return Ok(GenerateJwtToken(user.Email, user));
        }

        private string GenerateJwtToken(string email, User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(_jwtOptions.ExpireDays);

            var token = new JwtSecurityToken(
                _jwtOptions.Issuer,
                _jwtOptions.Issuer,
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UnauthorizedObjectResult Error(string message, IEnumerable<IdentityError> identityErrors)
        {
            var errors = string.Join(",", identityErrors.Select(e => e.Description).ToList());

            return Unauthorized(new {error = $"{message}: {errors}"});
        }
    }
}
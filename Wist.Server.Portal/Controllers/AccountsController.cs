using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Wist.Client.DataModel.Enums;
using Wist.Server.Portal.Dtos;
using Wist.Server.Portal.Helpers;
using Wist.Server.Portal.Services;
using Wist.Core.ExtensionMethods;

namespace Wist.Server.Portal.Controllers
{
	[Authorize(Roles = "puser")]
	[ApiController]
	[Route("[controller]")]
	public class AccountsController : ControllerBase
	{
		private IAccountsService _accountsService;
		private readonly AppSettings _appSettings;

		public AccountsController(IAccountsService accountsService, IOptions<AppSettings> appSettings)
		{
			_accountsService = accountsService;
			_appSettings = appSettings.Value;
		}

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody]AccountDto accountDto)
        {
            var accountDescriptor = _accountsService.Authenticate(accountDto.AccountId, accountDto.Password);

            if (accountDescriptor == null)
            {
                return Unauthorized(new { Message = "Failed to authenticate account" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, accountDto.AccountId.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, accountDto.AccountId.ToString()),
                    new Claim(ClaimTypes.Role, "puser")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { accountDescriptor.AccountId, accountDescriptor.AccountType, accountDescriptor.AccountInfo, Token = tokenString, PublicSpendKey = accountDescriptor.PublicSpendKey.ToHexString(), PublicViewKey = accountDescriptor.PublicViewKey.ToHexString() });
        }

        private string CreateToken(AccountDto accountDto)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, accountDto.AccountId.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, accountDto.AccountId.ToString()),
                    new Claim(ClaimTypes.Role, "puser")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            return tokenString;
        }

        [AllowAnonymous]
		[HttpPost("register")]
		public IActionResult Register([FromBody]AccountDto accountDto)
		{
			try
			{
				_accountsService.Create((AccountType)accountDto.AccountType, accountDto.AccountInfo, accountDto.Password);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[AllowAnonymous]
		[HttpGet("GetAll")]
		public IActionResult GetAll()
		{
			var accounts = _accountsService.GetAll().Select(a => new AccountDto
			{
				AccountId = a.AccountId,
				AccountType = (byte)a.AccountType,
				AccountInfo = a.AccountInfo
			});

			return Ok(accounts);
		}

        [HttpGet("{accountId}")]
		public IActionResult GetById(ulong accountId)
		{
			var account = _accountsService.GetById(accountId);
			return Ok(new AccountDto
			{
				AccountId = account.AccountId,
				AccountType = (byte)account.AccountType,
				AccountInfo = account.AccountInfo
			});
		}

        [HttpPost("logout")]
        public IActionResult Logout()
        {
			try
			{
				ulong accountId = ulong.Parse(User.Identity.Name);

				_accountsService.Clean(accountId);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

            return Ok();
        }

        [HttpPost("DuplicateUserAccount")]
		public IActionResult DuplicateUserAccount([FromBody] UserAccountReplicationDto userAccountReplication)
		{
			ulong accountId = _accountsService.DuplicateAccount(userAccountReplication.SourceAccountId, userAccountReplication.AccountInfo);

			if(accountId > 0)
			{
				return Ok();
			}

			return BadRequest();
		}

        [AllowAnonymous]
        [HttpPost("RemoveAccount")]
        public IActionResult RemoveAccount([FromBody] AccountDto account)
        {
            if (account != null)
            {
                _accountsService.Delete(account.AccountId);
                return Ok();
            }

            return BadRequest();
        }
	}
}

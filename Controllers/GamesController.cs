using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AcmeGames.Data;
using AcmeGames.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcmeGames.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Produces("application/json")]
	[Route("api/games")]
	public class GamesController : Controller
	{
        private readonly Database _db;

        public GamesController()
        {
            _db = Database.GetInstance();
        }

        /// <summary>
        /// Return Game information for the current or specific user owned
        /// </summary>
        /// <param name="userAccountId"></param>
        /// <returns></returns>
		[HttpGet]
		public async Task<IEnumerable<Game>>
		GetAllGames(string userAccountId = null)
		{
            var claimId = HttpContext.User.Claims.Where(c=>c.Type.Equals(ClaimTypes.NameIdentifier)).FirstOrDefault();
            var claimRole = HttpContext.User.Claims.Where(c=>c.Type.Equals(ClaimTypes.Role)).FirstOrDefault();
            if (!claimRole.Value.Equals("Admin") && !string.IsNullOrEmpty(userAccountId) && !claimId.Value.Equals(userAccountId))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;//Non-admin user has no rights to check other's owned games
                return await Task.FromResult<IEnumerable<Game>>(null);
            }
            return await _db.GetUserOwnedGames(string.IsNullOrEmpty(userAccountId) ? claimId.Value : userAccountId);
        }

        /// <summary>
        /// Get user owned game info, including the ownership info
        /// </summary>
        /// <param name="userAccountId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("ownership")]
        public async Task<IEnumerable<dynamic>>
        GetUserOwnershipsInfo(string userAccountId = null)
        {
            var claimId = HttpContext.User.Claims.Where(c => c.Type.Equals(ClaimTypes.NameIdentifier)).FirstOrDefault();
            var claimRole = HttpContext.User.Claims.Where(c => c.Type.Equals(ClaimTypes.Role)).FirstOrDefault();
            if (!claimRole.Value.Equals("Admin") && !string.IsNullOrEmpty(userAccountId) && !claimId.Value.Equals(userAccountId))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;//Non-admin user has no rights to check other's owned games
                return await Task.FromResult<IEnumerable<dynamic>>(null);
            }
            return await _db.GetUserOwnershipsInfo(string.IsNullOrEmpty(userAccountId) ? claimId.Value : userAccountId);
        }

        /// <summary>
        /// Get all games info target to specific user, including not owned ones
        /// </summary>
        /// <param name="userAccountId"></param>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        [HttpGet]
        [Route("all")]
        public async Task<IEnumerable<dynamic>>
        GetAllGameInfoByUser(string userAccountId)
        {
            if (string.IsNullOrEmpty(userAccountId))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return await Task.FromResult<IEnumerable<dynamic>>(null);
            }
            return await _db.GetAllGameInfoByUser(userAccountId);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        [HttpPost]
        [Route("grant")]
        public async Task<dynamic>
        GrantUserGame([FromBody] Ownership ownership)
        {
            if (ownership == null || string.IsNullOrEmpty(ownership.UserAccountId) || ownership.GameId<=0 )
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return await Task.FromResult<dynamic>(null);
            }
            var targetGameInfo = await _db.GrantUserGameAsync(ownership.UserAccountId, ownership.GameId);
            return new { StatusCode = HttpStatusCode.OK, GrantInfo = targetGameInfo };
        }

        [HttpPost]
        [Route("redeem")]
        public async Task<dynamic> RedeemGameKey([FromBody] GameKey key)
        {
            var findGameKey = await _db.GetGameKeys(k => k.Key.Equals(key.Key, StringComparison.CurrentCultureIgnoreCase));
            var gameKey = findGameKey.FirstOrDefault();
            if (gameKey == null)
            {
                //Key not found
                return new { StatusCode = HttpStatusCode.NotFound };
            }else if (gameKey.IsRedeemed)
            {
                //Redeemed
                return new { StatusCode = HttpStatusCode.NotModified };
            }
            else
            {
                var claim = HttpContext.User.Claims.Where(c => c.Type.Equals(ClaimTypes.NameIdentifier)).FirstOrDefault();
                var games = await _db.RedeemGameKey(key.Key, claim.Value);
                return new { StatusCode = HttpStatusCode.OK, Game = games.FirstOrDefault() };
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        [HttpPost]
        [Route("revoke")]
        public async Task<dynamic> RevokeUserGame([FromBody] Ownership ownship)
        {
            var findOwnership = await _db.GetOwnship(os => os.OwnershipId == ownship.OwnershipId);
            var find = findOwnership.FirstOrDefault();
            if (find == null)
            {
                //Ownership not found
                return new { StatusCode = HttpStatusCode.NotFound };
            }
            else if (find.State == OwnershipState.Revoked)
            {
                //Revoked already
                return new { StatusCode = HttpStatusCode.NotModified };
            }
            else
            {
                find.State = OwnershipState.Revoked;//Revode
                return new { StatusCode = HttpStatusCode.OK };
            }
        }
    }
    
}

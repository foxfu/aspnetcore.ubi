using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AcmeGames.Data;
using AcmeGames.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AcmeGames.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/users")]
    public class UsersController : Controller
    {
        private readonly Database _db;

        public UsersController()
        {
            _db = Database.GetInstance();
        }

        [HttpGet]
        public async Task<IEnumerable<User>>
        GetUserListAsync()
        {
            return await _db.GetUsers();
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost]
        [Route("update")]
        public User
        UpdateProfile([FromBody] User user)
        {
            var claimId = HttpContext.User.Claims.Where(c => c.Type.Equals(ClaimTypes.NameIdentifier)).FirstOrDefault();
            var claimRole = HttpContext.User.Claims.Where(c => c.Type.Equals(ClaimTypes.Role)).FirstOrDefault();
            if (!claimId.Value.Equals(user.UserAccountId) && !claimRole.Value.Equals("Admin"))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            return _db.UpdateProfile(user);
        }
    }
}
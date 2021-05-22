using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API.Controllers
{
    
    public class AccountController : BaseAPIController
    {
        private readonly DataContext context;

        public ITokenService Tokenservice { get; }

        public AccountController(DataContext context,ITokenService tokenservice)
        {
            this.context = context;
            Tokenservice = tokenservice;
        }

        [HttpPost("register")]

        public async Task<ActionResult<UserDto>> Register(RegisterDto dto)
        {
            using var hmac = new HMACSHA512();

            if (await UserExists(dto.Username)) return BadRequest("User exists");

            var user = new AppUser
            {
                UserName = dto.Username,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)),
                PasswordSalt = hmac.Key
            };

            context.Users.Add(user);

            await context.SaveChangesAsync();

            return new UserDto { 
                Username =user.UserName,
                Token= Tokenservice.CreateToken(user)

            };
        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {

            var user = await context.Users.SingleOrDefaultAsync(x => x.UserName.ToLower() == loginDto.Username.ToLower());

            if (user == null) return Unauthorized("UseName is Invalid");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computeHash.Length; i++)
            {
                if (computeHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }

            return new UserDto
            {
                Username = user.UserName,
                Token = Tokenservice.CreateToken(user)

            };

        }
        private async Task<bool> UserExists(string username)
        {

            return await context.Users.AnyAsync(x => x.UserName.ToLower() == username.ToLower());
        }
    }
}

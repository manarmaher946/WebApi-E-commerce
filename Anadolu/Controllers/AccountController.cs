﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Anadolu.Models;
using Anadolu.Repository.Base;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using Anadolu.DTO;

namespace Anadolu.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration config;
        private readonly IUnitOfWork unit;
        private readonly IHostingEnvironment host;

        public AccountController(UserManager<ApplicationUser> userManager, IConfiguration config,
            IUnitOfWork _unit,IHostingEnvironment _host)
        {
            this.userManager = userManager;
            this.config = config;
            unit = _unit;
            host = _host;
        }
        [HttpPost("register")]//api/account/register
        public async Task<IActionResult> Register(RegisterDTO userDTO)
        {
            ResultDTO resultDTO = new ResultDTO();
            if (ModelState.IsValid)
            {
                //create  ==>add user db
                ApplicationUser userModel = new ApplicationUser();
                userModel.FirstName = userDTO.FirstName;
                userModel.LastName = userDTO.LastName;
                userModel.Email = userDTO.Email;
                userModel.UserName = userDTO.UserName;
                userModel.PhoneNumber= userDTO.PhoneNumber;
                userModel.BirthDate = userDTO.BirthDate;
                userModel.Gender = userDTO.Gender;
                //string fileName = string.Empty;
                //if (userDTO.File == null || userDTO.File.Length == 0)
                //{
                //    return BadRequest(new { IsPassed = false, Data = "No File Selected" });
                //}
                //string myUpload = Path.Combine(host.WebRootPath, "images");
                //fileName = userDTO.File.FileName;
                //string fullPath = Path.Combine(myUpload, fileName);
                //using (var stream = new FileStream(fullPath, FileMode.Create))
                //{
                //    await userDTO.File.CopyToAsync(stream);
                //}

                //userModel.ImagePath = "http://localhost:5194/images/" + fileName;
                
                IdentityResult result = await userManager.CreateAsync(userModel, userDTO.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(userModel,"User");
                    User user = new User();

                    //Another Action
                    ///////////////////////////////////////////////
                    user.ApplicationUserId = userModel.Id;
                    //user.ImagePath = userModel.ImagePath;
                    user.Phone = userModel.PhoneNumber;
                    user.LastName = userModel.LastName;
                    user.FirstName = userModel.FirstName;
                    unit.UserRepository.Add(user);
                    
                    Cart cart = new Cart();
                    cart.UserId = userModel.Id;
                    unit.CartRepository.Add(cart);

                    //user.CardType = userDTO.CardType;

                    resultDTO.Data = userModel;
                    resultDTO.IsPassed= true;
                    return Ok(resultDTO);
                }
                else
                {
                    return BadRequest(new { IsPassed = false, Data = userModel });
                }
            }
            resultDTO.IsPassed = false;
            resultDTO.Data = ModelState;
            return BadRequest(resultDTO);
        }

        //[HttpPost("userregister")]
        //public IActionResult UserRegister(UserRegisterDTO userDTO)
        //{
        //    ResultDTO result = new ResultDTO();
        //    if (ModelState.IsValid)
        //    {
        //        User usermodel = new User();
        //        usermodel.ApplicationUserId = userDTO.ApplicationUserId;
        //        usermodel.FirstName = userDTO.FirstName;
        //        usermodel.LastName = userDTO.LastName;
        //        usermodel.Image= userDTO.Image;
        //        usermodel.Region = userDTO.Region;
        //        usermodel.Country = userDTO.Country;
        //        //usermodel.Role = "User";
        //        unit.UserRepository.Add(usermodel);

        //        ReturnUserDTO returnUserDTO = new ReturnUserDTO();
        //        returnUserDTO.ApplicationUserId = usermodel.ApplicationUserId;
        //        returnUserDTO.IsDeleted = usermodel.IsDeleted;
        //        //returnUserDTO.Role = usermodel.Role;
        //        returnUserDTO.Image = usermodel.Image;
        //        returnUserDTO.FirstName = usermodel.FirstName;
        //        returnUserDTO.LastName = usermodel.LastName;
        //        returnUserDTO.Region = usermodel.Region;
        //        returnUserDTO.Country = usermodel.Country;

        //        result.IsPassed = true;
        //        result.Data= returnUserDTO;
        //        return Ok(result);
        //    }

        //    result.IsPassed = false;
        //    result.Data = "ModelState isInvalid";
        //    return BadRequest(result);
        //}

        //[HttpPost("artistregister")]
        //public IActionResult ArtistRegister(ArtistRegisterDTO artistDTO)
        //{
        //    ResultDTO result = new ResultDTO();
        //    if (ModelState.IsValid)
        //    {
        //        Artist artistmodel = new Artist();
        //        artistmodel.ApplicationUserId = artistDTO.ApplicationUserId;
        //        artistmodel.FirstName= artistDTO.FirstName;
        //        artistmodel.LastName= artistDTO.LastName;
        //        artistmodel.Image = artistDTO.Image;
        //        artistmodel.Bio = artistDTO.Bio;
        //        //artistmodel.Role = "Artist";
        //        unit.ArtistRepository.Add(artistmodel);
                
        //        ReturnArtistDTO returnArtistDTO = new ReturnArtistDTO();
        //        returnArtistDTO.ApplicationUserId = artistDTO.ApplicationUserId;
        //        returnArtistDTO.IsDeleted = artistmodel.IsDeleted;
        //        //returnArtistDTO.Role = artistmodel.Role;
        //        returnArtistDTO.Image = artistDTO.Image;
        //        returnArtistDTO.FirstName = artistDTO.FirstName;
        //        returnArtistDTO.LastName = artistDTO.LastName;
        //        returnArtistDTO.Bio = artistDTO.Bio;
                
        //        result.IsPassed = true;
        //        result.Data = returnArtistDTO;
        //        return Ok(result);
        //    }

        //    result.IsPassed = false;
        //    result.Data = "ModelState isInvalid";
        //    return BadRequest(result);
        //}
        [HttpGet("getapplicationuser/{applicationUserId}")]
        public IActionResult GetApplicationUser(string applicationUserId)
        {
            ApplicationUser user= unit.ApplicationUserRepository.GetByIdString(applicationUserId, u=>u.IsDeleted==false);
            return Ok(user);
        }

        [HttpPost("login")]//api/account/login
        public async Task<IActionResult> Login(LoginDTO userDto)
        {
            ResultDTO result = new ResultDTO();
            if (ModelState.IsValid)
            {
                //check 
                ApplicationUser userModel = await userManager.FindByNameAsync(userDto.UserName);
                if (userModel != null && await userManager.CheckPasswordAsync(userModel, userDto.Password))
                {
                    //claims
                    List<Claim> myClaims = new List<Claim>();
                    //id,name,role,color,jit
                    myClaims.Add(new Claim(ClaimTypes.NameIdentifier, userModel.Id));
                    myClaims.Add(new Claim(ClaimTypes.Name, userModel.UserName));
                    myClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

                    //var user = await _userManager.FindByIdAsync();
                    List<string> roles = (List<string>)await userManager.GetRolesAsync(userModel);
                    //var roles = await userManager.GetRolesAsync(userModel);
                    if (roles.Contains("Admin"))
                    {
                        myClaims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    }
                    else if (roles.Contains("User"))
                    {
                        myClaims.Add(new Claim(ClaimTypes.Role, "User"));
                    }


                    //string userRole = string.Empty;


                    //ApplicationUser applicationUser = 
                    //    unit.ApplicationUserRepository.GetByUserName(userDto.UserName);

                    //if(applicationUser != null)
                    //{
                    //    Admin IsAdmin = unit.AdminRepository.GetByIdString(applicationUser.Id, a => a.IsDeleted == false);

                    //    User IsUser = unit.UserRepository.GetByIdString(applicationUser.Id, a => a.IsDeleted == false);

                    //    if (IsUser != null)
                    //    {
                    //        userRole = "User";
                    //    }
                    //    else if(IsAdmin != null)
                    //    {
                    //        userRole = "Artist";
                    //    }
                    //    else
                    //    {
                    //        userRole = "Admin";
                    //    }
                    //}
                    
                    //if (roles != null && applicationUser != null )
                    //{
                    //    //string userRole = applicationUser.

                    //    myClaims.Add(new Claim(ClaimTypes.Role, userRole));
                        
                    //}

                    var authSecritKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:SecrytKey"]));//asdZXCZX!#!@342352

                    SigningCredentials credentials =
                        new SigningCredentials(authSecritKey, SecurityAlgorithms.HmacSha256);

                    //craete token
                    JwtSecurityToken mytoken = new JwtSecurityToken(
                        issuer: config["JWT:ValidIss"],
                        audience: config["JWT:ValidAud"],
                        expires: DateTime.Now.AddHours(2),
                        claims: myClaims,
                        signingCredentials: credentials
                        );//signed token "resprest Toke"
                    result.IsPassed= true;
                    result.Data = new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(mytoken),
                        expiration = mytoken.ValidTo
                    };
                    return Ok(result);

                }
                result.IsPassed = false;
                result.Data = "Invalid Login Account";
                //craete toke
                return BadRequest(result);
            }
            result.IsPassed= false;
            result.Data = ModelState;
            return BadRequest(result);
        }



//        [HttpPost("adminregister")]
//        public async Task<IActionResult> RegisterAsAdmin([FromForm] RegisterDTO userDTO)
//        {
//            ResultDTO resultDTO = new ResultDTO();
//            if (ModelState.IsValid)
//            {
//                ApplicationUser userModel = new ApplicationUser();
//                userModel.FirstName = userDTO.FirstName;
//                userModel.LastName = userDTO.LastName;
//                userModel.Email = userDTO.Email;
//                userModel.UserName = userDTO.UserName;
//                userModel.PhoneNumber = userDTO.PhoneNumber;
//                userModel.BirthDate = userDTO.BirthDate;
//                userModel.Gender = userDTO.Gender;
//                //string fileName = string.Empty;
//                //if (userDTO.File == null || userDTO.File.Length == 0)
//                //{
//                //    return BadRequest(new { IsPassed = false, Data = "No File Selected" });
//                //}
//                //string myUpload = Path.Combine(host.WebRootPath, "images");
//                //fileName = userDTO.File.FileName;
//                //string fullPath = Path.Combine(myUpload, fileName);
//                //using (var stream = new FileStream(fullPath, FileMode.Create))
//                //{
//                //    await userDTO.File.CopyToAsync(stream);
//                //}

//                //userModel.ImagePath = "http://localhost:5194/images/" + fileName;
//                IdentityResult result = await userManager.CreateAsync(userModel, userDTO.Password);
//                if (result.Succeeded)
//                {
//                    await userManager.AddToRoleAsync(userModel, "Admin");
//                    Admin admin = new Admin();

//                    //Another Action
//                    ///////////////////////////////////////////////
//                    admin.ApplicationUserId = userModel.Id;
//                    //admin.ImagePath = userModel.ImagePath;
//                    admin.Phone = userModel.PhoneNumber;
//                    admin.LastName = userModel.LastName;
//                    admin.FirstName = userModel.FirstName;
//                    admin.BirthDate = userModel.BirthDate;

//                    unit.AdminRepository.Add(admin);

//                    resultDTO.Data = userModel;
//                    resultDTO.IsPassed = true;
//                    return Ok(resultDTO);
//                }
//                else
//                {
//                    return BadRequest(new { IsPassed = false, Data = userModel });
//                }
//            }
//            resultDTO.IsPassed = true;
//            resultDTO.Data = ModelState;
//            return BadRequest(resultDTO);
//        }



    }
}

//[HttpPost("register")]//api/account/register
//public async Task<IActionResult> Register([FromForm] RegisterDTO userDTO)
//{
//    ResultDTO resultDTO = new ResultDTO();
//    if (ModelState.IsValid)
//    {
//        //create  ==>add user db
//        ApplicationUser userModel = new ApplicationUser();
//        userModel.FirstName = userDTO.FirstName;
//        userModel.LastName = userDTO.LastName;
//        userModel.Email = userDTO.Email;
//        userModel.UserName = userDTO.UserName;
//        userModel.PhoneNumber = userDTO.PhoneNumber;
//        userModel.BirthDate = userDTO.BirthDate;
//        userModel.Gender = userDTO.Gender;
//        string fileName = string.Empty;
//        if (userDTO.File == null || userDTO.File.Length == 0)
//        {
//            return BadRequest(new { IsPassed = false, Data = "No File Selected" });
//        }
//        string myUpload = Path.Combine(host.WebRootPath, "images");
//        fileName = userDTO.File.FileName;
//        string fullPath = Path.Combine(myUpload, fileName);
//        using (var stream = new FileStream(fullPath, FileMode.Create))
//        {
//            await userDTO.File.CopyToAsync(stream);
//        }

//        userModel.ImagePath = "http://localhost:5194/images/" + fileName;
//        IdentityResult result = await userManager.CreateAsync(userModel, userDTO.Password);
//        if (result.Succeeded)
//        {
//            await userManager.AddToRoleAsync(userModel, "User");
//            User user = new User();

//            //Another Action
//            ///////////////////////////////////////////////
//            user.ApplicationUserId = userModel.Id;
//            user.ImagePath = userModel.ImagePath;
//            user.Phone = userModel.PhoneNumber;
//            user.LastName = userModel.LastName;
//            user.FirstName = userModel.FirstName;
//            unit.UserRepository.Add(user);

//            Cart cart = new Cart();
//            cart.UserId = userModel.Id;
//            unit.CartRepository.Add(cart);

//            //user.CardType = userDTO.CardType;

//            resultDTO.Data = userModel;
//            resultDTO.IsPassed = true;
//            return Ok(resultDTO);
//        }
//        else
//        {
//            return BadRequest(new { IsPassed = false, Data = userModel });
//        }
//    }
//    resultDTO.IsPassed = true;
//    resultDTO.Data = ModelState;
//    return BadRequest(resultDTO);
//}
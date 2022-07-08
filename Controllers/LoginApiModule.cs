using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using СourseworkBackend.Models;
using СourseworkBackend.CustomAttributes;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace СourseworkBackend.Controllers
{
    [Route("api/")]
    [Produces("application/json")]
    [ApiController]
    public class LoginModule : ControllerBase
    {
        [Route("hi")]
        [HttpPost]
        public string Hi()
        {
            return "Hi!";
        }


        [Route("new_user")]
        [HttpPost]
        public IActionResult createNewUser(CreateAccountRequest newAccountData)
        {
            byte[] serverside_pass_fingerprint;

            try
            {
                serverside_pass_fingerprint = GetServersideFingerprint(newAccountData.passFingerprint);
            }
            catch
            {
                Response.StatusCode = 400;
                return new BadRequestResult();
            }


            User newUser = new User { Login = newAccountData.login, PassFingerprint = serverside_pass_fingerprint, UserName = newAccountData.userName };

            User? exsistingUser =  GlobalScope.database.Users.Where(user => user.Login == newAccountData.login).FirstOrDefault();

            if (exsistingUser != null)
            {
                Response.StatusCode = 409;
                return new ConflictResult();
            }

            GlobalScope.database.Users.Add(newUser);
            Session newSession = CreateSession(newUser);

            Console.WriteLine($"New user added: {newUser.UserName} | {newUser.Id}") ;

            return new ObjectResult(new CreateAccountRequestSuccessResponse { userName = newUser.UserName, token = Convert.ToBase64String(newSession.Token) });
        }

        [Route("login")]
        [HttpPost]
        public IActionResult TryToLogin(LoginRequest loginData)
        {
            byte[] serverside_pass_fingerprint;

            try
            {
                serverside_pass_fingerprint = GetServersideFingerprint(loginData.passFingerprint);
            }
            catch
            {
                Response.StatusCode = 400;
                return new BadRequestResult();
            }

            User? userInDatabase = GlobalScope.database.Users.Where(user => user.Login == loginData.login && user.PassFingerprint == serverside_pass_fingerprint).FirstOrDefault();
            if (userInDatabase != null)
            {
                Session newSession = CreateSession(userInDatabase);
                Console.WriteLine($"New user loggedin: {userInDatabase.UserName} | {userInDatabase.Id}");
                return new ObjectResult(new LoginSuccessResponce { token = Convert.ToBase64String(newSession.Token) });

            }
            else
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }
        }

        [Route("device_login")]
        [HttpPost]
        public IActionResult TryToLoginByDeviceID(LoginByDeviceRequest deviceData)
        {
            byte[] serverside_device_fingerprint;
            byte[] serverside_device_token;
            try 
            { 
                serverside_device_fingerprint = GetServersideFingerprint(deviceData.device_id);
                serverside_device_token = Convert.FromBase64String(deviceData.device_token);
            }
            catch
            {
                Response.StatusCode = 400;
                return new BadRequestResult();
            }

            //Console.WriteLine(BitConverter.ToString(serverside_device_fingerprint) +" | "+ BitConverter.ToString(serverside_device_token));

            UserTrustedDevice? userDeviceInDatabase = GlobalScope.database.UsersTrustedDevices.Where(device => device.DeviceFingerprint == serverside_device_fingerprint && device.DeviceToken == serverside_device_token).FirstOrDefault();
            if (userDeviceInDatabase != null)
            {
                User? user = GlobalScope.database.Users.Where(user => user.Id == userDeviceInDatabase.Userid).FirstOrDefault();

                if (user == null)
                {
                    GlobalScope.database.UsersTrustedDevices.Remove(userDeviceInDatabase);
                    return new UnauthorizedResult();
                }

                //Console.WriteLine(userDeviceInDatabase.User.Login + " | "+ userDeviceInDatabase.User.Id);
                Session newSession = CreateSession(user);
                return new ObjectResult( new LoginSuccessResponce { token = Convert.ToBase64String(newSession.Token) });
            }
            else
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }
        }


        [Route("trust_device")]
        [HttpPost]
        [ValidSessionRequired]
        public IActionResult TrustThisDeviceID(TrustDeviceRequest loginData)
        {
            Session? session = Request.RouteValues["Session"] as Session;

            if (session == null)
            {
                Response.StatusCode = 403;
                return new UnauthorizedResult();
            }

            byte[] serverside_device_fingerprint;
            try
            {
                serverside_device_fingerprint = GetServersideFingerprint(loginData.deviceFingerprint);
            }
            catch
            {
                Response.StatusCode = 400;
                return new BadRequestResult();
            }

            var device_token = Guid.NewGuid().ToByteArray();
            var serverside_device_token = GlobalScope.GetHashSha3(device_token);
            GlobalScope.database.UsersTrustedDevices.Add(new UserTrustedDevice { User = session.User, DeviceFingerprint = serverside_device_fingerprint, DeviceToken = serverside_device_token});
            GlobalScope.database.SaveChangesAsync().Wait();
            
            return new ObjectResult( new TrustDeviceSuccessResponce { deviceToken = Convert.ToBase64String(serverside_device_token) } );

        }

        private Session CreateSession(User user)
        {
            Session newSession = new Session { User = user, Ip = Request.Host.Host, Token = Guid.NewGuid().ToByteArray() };
            GlobalScope.database.Sessions.Add(newSession);
            GlobalScope.database.SaveChangesAsync().Wait();
            return newSession;
        }

        private byte[] GetServersideFingerprint(string clientsideBase64PassFingerprint)
        {
            return GlobalScope.GetHashSha3(Convert.FromBase64String(clientsideBase64PassFingerprint));
        }
    }


    public class CreateAccountRequest
    {
        [Required]
        public string login { get; set; } = null!;

        [Required]
        public string passFingerprint { get; set; } = null!;

        [Required]
        public string userName { get; set; } = null!;
    }

    public class CreateAccountRequestResponse : ActionResult
    {
    }

    public class CreateAccountRequestSuccessResponse : CreateAccountRequestResponse
    {
        public string userName { get; set; } = null!;

        public string token { get; set; } = null!;

    }


    public class LoginRequest 
    {
        [Required]
        public string login { get; set; } = null!;

        [Required]
        public string passFingerprint { get; set; } = null!;
    }

    public class LoginByDeviceRequest
    {
        [Required]
        public string device_id { get; set; } = null!;
        [Required]
        public string device_token { get; set; } = null!;
    }

    public class LoginResponce : ActionResult
    {
    }

    public class LoginSuccessResponce : LoginResponce
    {
        public string token { get; set; } = null!;
    }

    public class TrustDeviceRequest
    {
        [Required]
        public string deviceFingerprint { get; set; } = null!;
    }

    public class TrustDeviceResponce : ActionResult
    {
    }

    public class TrustDeviceSuccessResponce : TrustDeviceResponce
    {
        public string deviceToken { get; set; } = null!;
    }


}

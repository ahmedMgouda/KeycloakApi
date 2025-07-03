using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeycloakApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public IActionResult Get()
        {
            return Ok("This is a protected endpoint");
        }


        [Authorize(Roles = "perm-read-surgeon-activities")]
        public IActionResult GetSurgeonActivities()
        {
            return Ok("Surgeon data accessed.");
        }

        [Authorize(Roles = "perm-read-replenishment")]
        public IActionResult GetReplenishment()
        {
            return Ok("Replenishment data accessed.");
        }
    }
}

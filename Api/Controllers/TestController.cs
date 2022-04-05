using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {        
        [HttpGet(Name = "test1")]
        public async Task<int> Test1()
        {
            return await Task.FromResult(1);
        }
    }
}

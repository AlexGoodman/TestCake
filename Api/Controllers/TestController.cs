using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {        
        [HttpGet(Name = "test1")]
        public async Task<IEnumerable<string>> Test1()
        {
            return await Task.FromResult(new HashSet<string>{ "test" });
        }
    }
}

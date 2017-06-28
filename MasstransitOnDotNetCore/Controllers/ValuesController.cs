using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

namespace MasstransitOnDotNetCore.Controllers
{
    using System.Threading.Tasks;

    using MassTransit;

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly IBus bus;

        public ValuesController(IBus bus)
        {
            this.bus = bus;
        }

        // GET api/values
        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            await this.bus.Publish(new SimpleRequest());
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

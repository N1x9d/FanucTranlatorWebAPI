using System.IO;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using WebApplication2.Data.Interfaces;
using WebApplication2.Data.Models;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("api/")]
    public class TranslatorController:ControllerBase
    {
        public TranslatorController(IParserFactory parserFactory)
        {
            ParserFactory = parserFactory;
        }

        private IParserFactory ParserFactory { get; }
        
        [HttpPost("Parse")]
        public async Task<IActionResult> DocxToXml(IFormFile js)
        {
            long length = js.Length;
            if (length < 0)
                return BadRequest();

            var result = new StringBuilder();
            using (var reader = new StreamReader(js.OpenReadStream()))
            {
                while (reader.Peek() >= 0)
                    result.AppendLine(await reader.ReadLineAsync()); 
            }
            JObject json = JObject.Parse(result.ToString());
            ParceReqestModel pm = json.ToObject<ParceReqestModel>();
            ParserFactory.Parce(pm);
            return null;
        }

    }
}
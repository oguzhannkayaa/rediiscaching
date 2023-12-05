using CachingWebApi.Data;
using CachingWebApi.Models;
using CachingWebApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CachingWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriversController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ICacheService _cacheService;
        private readonly AppDbContext _context;
        
        public DriversController(ILogger<WeatherForecastController> logger, ICacheService cacheService, AppDbContext context)
        {
            _logger = logger;
            _cacheService = cacheService;
            _context = context;
        }

        [HttpGet("drivers")]
        public async Task<IActionResult> Get()
        {
            var cacheData = _cacheService.GetData<IEnumerable<Driver>>("drivers");
            if(cacheData != null && cacheData.Count() > 0)
            {
                return Ok(cacheData);
            }
            cacheData = await _context.Drivers.ToListAsync();

            var expryTime = DateTimeOffset.Now.AddMinutes(5);

            _cacheService.SetData<IEnumerable<Driver>>("drivers", cacheData, expryTime);

            return Ok(cacheData);
        }

        [HttpPost("AddDriver")]
        public async Task<IActionResult> Post(Driver value)
        {
            var addedObject = await _context.Drivers.AddAsync(value);
            var expryTime = DateTimeOffset.Now.AddMinutes(5);

            _cacheService.SetData<Driver>($"driver{value.Id}", addedObject.Entity, expryTime);

            await _context.SaveChangesAsync();

            return Ok(addedObject.Entity);

        }

        [HttpDelete("DeleteDriver")]
        public async Task<IActionResult> Delete(int id)
        {
            var exist = await _context.Drivers.FirstOrDefaultAsync(c => c.Id == id);

            if(exist != null)
            {
                _context.Remove(exist);
                _cacheService.RemoveData($"driver{id}");
                await _context.SaveChangesAsync();
                return NoContent();
            }

            return NotFound("Not found");
        }
    }
}

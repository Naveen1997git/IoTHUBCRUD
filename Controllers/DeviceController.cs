using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using ClientMessage = Microsoft.Azure.Devices.Client.Message;
using ClientTransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace IoTDeviceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly RegistryManager _registryManager;
        private readonly string _deviceConnectionString;

        public DeviceController(IConfiguration configuration)
        {
            string iotHubConnectionString = configuration["AzureIoTHub:ConnectionString"];
            _deviceConnectionString = configuration["AzureIoTHub:DeviceConnectionString"];
            _registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
        }

        public class DeviceRequest
        {
            public string DeviceId { get; set; }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateDevice([FromBody] DeviceRequest request)
        {
            try
            {
                var device = new Device(request.DeviceId);
                await _registryManager.AddDeviceAsync(device);
                return Ok(new { message = $"Device '{request.DeviceId}' created." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{deviceId}")]
        public async Task<IActionResult> GetDevice(string deviceId)
        {
            try
            {
                var device = await _registryManager.GetDeviceAsync(deviceId);
                return Ok(new { device.Id, device.Status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateDevice([FromBody] string deviceId)
        {
            try
            {
                var device = await _registryManager.GetDeviceAsync(deviceId);
                device.Status = DeviceStatus.Disabled;
                await _registryManager.UpdateDeviceAsync(device);
                return Ok(new { message = $"Device '{deviceId}' updated to Disabled." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{deviceId}")]
        public async Task<IActionResult> DeleteDevice(string deviceId)
        {
            try
            {
                await _registryManager.RemoveDeviceAsync(deviceId);
                return Ok(new { message = $"Device '{deviceId}' deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("updateDesiredProperties")]
        public async Task<IActionResult> UpdateDesiredProperties([FromBody] string deviceId)
        {
            try
            {
                Twin twin = await _registryManager.GetTwinAsync(deviceId);
                twin.Properties.Desired["temperature"] = 22;
                await _registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);
                return Ok(new { message = "Desired properties updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("updateReportedProperties")]
        public async Task<IActionResult> UpdateReportedProperties([FromBody] string deviceId)
        {
            try
            {
                Twin twin = await _registryManager.GetTwinAsync(deviceId);
                twin.Properties.Reported["batteryLevel"] = 85;
                await _registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);
                return Ok(new { message = "Reported properties updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("sendTelemetry")]
        public async Task<IActionResult> SendTelemetry()
        {
            try
            {
                var deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, ClientTransportType.Mqtt);
                string messageString = "{ \"temperature\": 25, \"humidity\": 60 }";
                var message = new ClientMessage(Encoding.ASCII.GetBytes(messageString));
                await deviceClient.SendEventAsync(message);
                return Ok(new { message = "Telemetry message sent." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

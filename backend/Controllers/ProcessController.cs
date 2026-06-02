using asec.DataConversion;
using asec.Digitalization;
using asec.Emulation;
using asec.LongRunning;
using asec.Upload;
using asec.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace asec.Controllers
{
    [ApiController]
    [Route("/api/v1/processes")]
    public class ProcessController : ControllerBase
    {
        private readonly IProcessManager<Digitalization.Process, DigitalizationResult, DigitalizationProcessDetail> _digitalizationProcessManager;
        private readonly IProcessManager<DataConversion.Process, ConversionResult, ConversionProcessDetail> _conversionProcessManager;
        private readonly IProcessManager<Emulation.BaseProcess, EmulationResult, EmulationProcessDetail> _emulationProcessManager;
        private readonly IProcessManager<Upload.Process, UploadResult, EmptyProcessDetail> _uploadProcessManager;
        public ProcessController(
            IProcessManager<Digitalization.Process, DigitalizationResult, DigitalizationProcessDetail> digitalizationProcessManager,
            IProcessManager<DataConversion.Process, ConversionResult, ConversionProcessDetail> conversionProcessManager,
            IProcessManager<Emulation.BaseProcess, EmulationResult, EmulationProcessDetail> emulationProcessManager,
            IProcessManager<Upload.Process, UploadResult, EmptyProcessDetail> uploadProcessManager)
        {
            _digitalizationProcessManager = digitalizationProcessManager;
            _conversionProcessManager = conversionProcessManager;
            _emulationProcessManager = emulationProcessManager;
            _uploadProcessManager = uploadProcessManager;
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetProcesses()
        {
            var result = new Processes();

            foreach(var process in _digitalizationProcessManager.GetProcesses())
            {
                result.digitalizationProcesses.Add(DigitalizationProcess.FromProcess(process));
            }
            foreach (var process in _conversionProcessManager.GetProcesses())
            {
                result.conversionProcesses.Add(ConversionProcess.FromProcess(process));
            }
            foreach (var process in _emulationProcessManager.GetProcesses())
            {
                result.emulationProcesses.Add(EmulationProcess.FromProcess(process));
            }
            foreach (var process in _uploadProcessManager.GetProcesses())
            {
                result.uploadProcesses.Add(UploadProcess.FromProcess(process));
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("{processId}/stop")]
        public async Task<IActionResult> StopProcess(string processId)
        {
            var id = Guid.Parse(processId);

            if(_digitalizationProcessManager.GetProcess(id) is not null)
            {
                await _digitalizationProcessManager.CancelProcessAsync(id);
                return Ok();
            }
            if (_conversionProcessManager.GetProcess(id) is not null)
            {
                await _conversionProcessManager.CancelProcessAsync(id);
                return Ok();
            }
            if (_emulationProcessManager.GetProcess(id) is not null)
            {
                await _emulationProcessManager.CancelProcessAsync(id);
                return Ok();
            }
            if (_uploadProcessManager.GetProcess(id) is not null)
            {
                await _uploadProcessManager.CancelProcessAsync(id);
                return Ok();
            }

            return NotFound(processId);
        }
    }
}

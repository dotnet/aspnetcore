using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ActualApiResponseMetadataFactoryTest
{
    public class TryGetActualResponseMetadataController : ControllerBase
    {
        public async Task<ActionResult<IEnumerable<TryGetActualResponseMetadataModel>>> ActionWithActionResultOfTReturningOkResult()
        {
            await Task.Yield();
            var models = new List<TryGetActualResponseMetadataModel>();

            return Ok(models);
        }

        public async Task<ActionResult<IEnumerable<TryGetActualResponseMetadataModel>>> ActionWithActionResultOfTReturningModel()
        {
            await Task.Yield();
            var models = new List<TryGetActualResponseMetadataModel>();

            return models;
        }

        public async Task<ActionResult<TryGetActualResponseMetadataModel>> ActionReturningNotFoundAndModel(int id)
        {
            await Task.Yield();

            if (id == 0)
            {
                /*MM1*/return NoContent();
            }

            /*MM2*/return new TryGetActualResponseMetadataModel();
        }
    }

    public class TryGetActualResponseMetadataModel { }
}

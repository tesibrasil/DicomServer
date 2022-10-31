using System.Web.Http;

namespace DicomAPI.Interfaces
{
    public interface IGenericController 
    {
        [HttpGet]
        bool IsAlive();

        [HttpGet]
        string Info();

        [HttpPost]
        bool LogToDashboard([FromUri]string log);       
    }
}

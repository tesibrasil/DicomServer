using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DicomAPI.Interfaces
{
    internal class GenericController : ApiController, IGenericController
    {
        internal IGenericController _controller;
        internal GenericController(IGenericController i)
        {
            _controller = i;
        }

        public string Info()
        {
            return _controller.Info();
        }

        public bool IsAlive()
        {
            return _controller.IsAlive();
        }

        public bool LogToDashboard([FromUri] string log)
        {
            return _controller.LogToDashboard(log);
        }
    }
}

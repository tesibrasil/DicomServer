using System.Collections.Generic;

namespace DicomServer.Worklist.Model
{
    public interface IModalityAETSource
    {
        List<ModalityAET> GetAllModalityAETs();
    }
}

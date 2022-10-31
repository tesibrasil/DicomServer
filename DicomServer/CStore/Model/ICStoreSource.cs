using Dicom.Network;

namespace DicomServer.CStore.Model
{
    public interface ICStoreSource
    {
        DicomStatus Store(DicomCStoreRequest request);

        DicomStatus StoreDocument(DicomCStoreRequest documentRequest);

        DicomStatus StoreImage(DicomCStoreRequest imageRequest);
    }
}

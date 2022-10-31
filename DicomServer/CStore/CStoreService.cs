using Dicom;
using Dicom.Log;
using Dicom.Network;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DicomServer.CStore
{

    public class CStoreService : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
    {
        private static readonly DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
        {
               DicomTransferSyntax.ExplicitVRLittleEndian,
               DicomTransferSyntax.ExplicitVRBigEndian,
               DicomTransferSyntax.ImplicitVRLittleEndian
        };

        private static readonly DicomTransferSyntax[] AcceptedImageTransferSyntaxes = new DicomTransferSyntax[]
        {
               // Lossless
               DicomTransferSyntax.JPEGLSLossless,
               DicomTransferSyntax.JPEG2000Lossless,
               DicomTransferSyntax.JPEGProcess14SV1,
               DicomTransferSyntax.JPEGProcess14,
               DicomTransferSyntax.RLELossless,
               // Lossy
               DicomTransferSyntax.JPEGLSNearLossless,
               DicomTransferSyntax.JPEG2000Lossy,
               DicomTransferSyntax.JPEGProcess1,
               DicomTransferSyntax.JPEGProcess2_4,               
               // Uncompressed
               DicomTransferSyntax.ExplicitVRLittleEndian,
               DicomTransferSyntax.ExplicitVRBigEndian,
               DicomTransferSyntax.ImplicitVRLittleEndian
        };

        public CStoreService(INetworkStream stream, Encoding fallbackEncoding, Logger log)
            : base(stream, fallbackEncoding, log)
        {
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            if (CStoreServer.AETitle != association.CalledAE)
            {
                LogHelper.Error($"Association with {association.CallingAE} rejected since called aet {association.CalledAE} is unknown", Program.DebugMode);
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            }

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification) pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None) pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
            }

            LogHelper.Info($"Accepted association request from {association.CallingAE}", Program.DebugMode);
            return SendAssociationAcceptAsync(association);
        }
        public void Clean()
        {
            // cleanup, like cancel outstanding move- or get-jobs
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            Clean();
            return SendAssociationReleaseResponseAsync();
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            //log the abort reason
            LogHelper.Error($"Received abort from {source}, reason is {reason}", Program.DebugMode);
        }

        public void OnConnectionClosed(Exception exception)
        {
            Clean();
        }

        public DicomCStoreResponse OnCStoreRequest(DicomCStoreRequest request)
        {            
            if (Program.DebugMode || Program.DebugSaveDcm)
            {
                var fileName = $"CS_{DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.ffff")}_";
                try
                {
                    var directory = $"{Program.DebugDcmFolder}\\CS";
                   
                    string exam = null;
                    request.Dataset.TryGetSingleValue<string>(DicomTag.AccessionNumber, out exam);

                    string ptid = null;
                    request.Dataset.TryGetSingleValue<string>(DicomTag.PatientID, out ptid);
                    
                    if ((exam is null) && (ptid is null))
                    {                        
                        fileName += $"{Guid.NewGuid()}.dcm";                                                
                    }
                    else
                    {
                        fileName += $"ORD.{exam ?? "0"}--PID{ptid ?? "0"}.dcm";
                    }

                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                    var filePath = $"{directory}\\{fileName}";
                    request.File.Save(filePath);
                    LogHelper.Debug($"C-Store Request DCM saved as {filePath}", Program.DebugMode);
                }
                catch (Exception e)
                {
                    LogHelper.Debug($"Could not Save Request {fileName} to Log due to {e.Message}", Program.DebugMode);
                }
            }

            DicomStatus storeStatus = CStoreServer.CStoreProvider.Store(request);
            if (storeStatus != DicomStatus.Success)
            {
                LogHelper.Error($"Request {request.File.File.Name} Failed to Store due to {storeStatus}", Program.DebugMode);
                SaveErroredMessageAsDCM(request);
            }
            return new DicomCStoreResponse(request, storeStatus);
        }

        public void OnCStoreRequestException(string tempFileName, Exception e)
        {
            // let library handle logging and error response
        }

        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            LogHelper.Info($"Received verification request from AE {Association.CallingAE} with IP: {Association.RemoteHost}", Program.DebugMode);
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        private void SaveErroredMessageAsDCM(DicomCStoreRequest request)
        {
            try
            {
                var directory = $"{Program.AssemblyLocation}\\DicomLog\\Files";
                var fileName = string.Empty;

                string exam = null;
                request.Dataset.TryGetSingleValue<string>(DicomTag.AccessionNumber, out exam);
                if (exam is null)
                {
                    string ptid = null;
                    request.Dataset.TryGetSingleValue<string>(DicomTag.PatientID, out ptid);

                    if (ptid is null) fileName = $"{Guid.NewGuid()}.dcm";
                    else fileName = "PID." + ptid + ".dcm";
                }
                else
                {
                    fileName = "ORD." + exam + ".dcm";
                }

                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                var filePath = $"{directory}\\{fileName}";
                request.File.Save(filePath);
                LogHelper.Error($"Request DCM saved as {filePath}", Program.DebugMode);
            }
            catch (Exception e)
            {
                LogHelper.Error($"Could not Save Errored Request {request.File.File.Name} to Log due to {e.Message}", Program.DebugMode);
            }
        }
    }

}

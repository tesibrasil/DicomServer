using Dicom;
using Dicom.Log;
using Dicom.Network;
using DicomServer.Worklist.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicomServer.Worklist
{

    public class WorklistService : DicomService, IDicomServiceProvider, IDicomCEchoProvider, IDicomCFindProvider, IDicomNServiceProvider
    {
        private static readonly DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
           {
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian
           };

        private IMppsSource _mppsSource;
        private IMppsSource MppsSource
        {
            get
            {
                if (_mppsSource == null) _mppsSource = new MppsHandler(Logger);
                return _mppsSource;
            }
        }
        
        public WorklistService(INetworkStream stream, Encoding fallbackEncoding, Logger log) : base(stream, fallbackEncoding, log)
        {
        }

        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            LogHelper.Info($"Received verification request from AE {Association.CallingAE} with IP: {Association.RemoteHost}", Program.DebugMode);
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }


        public IEnumerable<DicomCFindResponse> OnCFindRequest(DicomCFindRequest request)
        {
            var directory = $"{Program.DebugDcmFolder}\\WL";
            var fileName = $"WL_{Association.CallingAE}{DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.ffff")}.txt";
            var filePath = $"{directory}\\{fileName}";

            if (Program.DebugMode || Program.DebugSaveDcm)
            {                
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);                
                File.WriteAllText(filePath, $"{request.ToString(true)}");
            }

            if (request.HasDataset)
            {                               
                DicomDataset procedureStep = null;
                if (request.Dataset.Contains(DicomTag.ScheduledProcedureStepSequence))
                {
                    procedureStep = request.Dataset.GetSequence(DicomTag.ScheduledProcedureStepSequence).First();

                    var scheduledStationAET = procedureStep.GetSingleValueOrDefault(DicomTag.ScheduledStationAETitle, string.Empty);

                    if (string.IsNullOrEmpty(scheduledStationAET) && WorklistServer.ServerModule.WLUsesAssociationCallingAE)
                        procedureStep.AddOrUpdate<string>(DicomTag.ScheduledStationAETitle, Association.CallingAE);
                }

                foreach (DicomDataset result in WorklistHandler.FilterWorklistItems(request.Dataset, WorklistServer.CurrentWorklistItems, WorklistServer.CurrentModalityAETs))
                {
                    yield return new DicomCFindResponse(request, DicomStatus.Pending) { Dataset = result };
                    if (Program.DebugMode || Program.DebugSaveDcm)
                    {
                        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                        File.AppendAllText(filePath, $"{Environment.NewLine}"
                            + $"{new DicomCFindResponse(request, DicomStatus.Pending) { Dataset = result }.ToString(true)}");
                        LogHelper.Debug($"Worklist Request DCM saved as {filePath}", Program.DebugMode);
                    }
                }
            }
            yield return new DicomCFindResponse(request, DicomStatus.Success);
        }


        public void OnConnectionClosed(Exception exception)
        {
            Clean();
        }


        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
            LogHelper.Error($"Received abort from {source}, reason is {reason}", Program.DebugMode);
        }


        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            Clean();
            return SendAssociationReleaseResponseAsync();
        }


        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            LogHelper.Info($"Received association request from AE: {association.CallingAE} with IP: {association.RemoteHost}", Program.DebugMode);

            if (WorklistServer.AETitle != association.CalledAE)
            {
                LogHelper.Error($"Association from [{association.CallingAE}] to [{WorklistServer.AETitle}] was rejected since called aet [{association.CalledAE}] is unknown", Program.DebugMode);
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            }

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification
                    || pc.AbstractSyntax == DicomUID.ModalityWorklistInformationModelFIND
                    || pc.AbstractSyntax == DicomUID.ModalityPerformedProcedureStepSOPClass
                    || pc.AbstractSyntax == DicomUID.ModalityPerformedProcedureStepNotificationSOPClass
                    || pc.AbstractSyntax == DicomUID.ModalityPerformedProcedureStepNotificationSOPClass)
                {
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                }
                else
                {
                    LogHelper.Warning($"Requested abstract syntax {pc.AbstractSyntax} from {association.CallingAE} not supported", Program.DebugMode);
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            LogHelper.Info($"Accepted association request from {association.CallingAE}", Program.DebugMode);
            return SendAssociationAcceptAsync(association);
        }


        public void Clean()
        {
            // cleanup, like cancel outstanding move- or get-jobs
        }


        public DicomNCreateResponse OnNCreateRequest(DicomNCreateRequest request)
        {
            if (request.SOPClassUID != DicomUID.ModalityPerformedProcedureStepSOPClass)
            {
                return new DicomNCreateResponse(request, DicomStatus.SOPClassNotSupported);
            }
            // on N-Create the UID is stored in AffectedSopInstanceUID, in N-Set the UID is stored in RequestedSopInstanceUID
            var affectedSopInstanceUID = request.Command.GetSingleValue<string>(DicomTag.AffectedSOPInstanceUID);
            LogHelper.Info($"reeiving N-Create with SOPUID {affectedSopInstanceUID}", Program.DebugMode);
            // get the procedureStepIds from the request
            var procedureStepId = request.Dataset
                .GetSequence(DicomTag.ScheduledStepAttributesSequence)
                .First()
                .GetSingleValue<string>(DicomTag.ScheduledProcedureStepID);
            var ok = MppsSource.SetInProgress(affectedSopInstanceUID, procedureStepId);

            return new DicomNCreateResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
        }


        public DicomNSetResponse OnNSetRequest(DicomNSetRequest request)
        {
            if (request.SOPClassUID != DicomUID.ModalityPerformedProcedureStepSOPClass)
            {
                return new DicomNSetResponse(request, DicomStatus.SOPClassNotSupported);
            }
            // on N-Create the UID is stored in AffectedSopInstanceUID, in N-Set the UID is stored in RequestedSopInstanceUID
            var requestedSopInstanceUID = request.Command.GetSingleValue<string>(DicomTag.RequestedSOPInstanceUID);
            LogHelper.Info($"receiving N-Set with SOPUID {requestedSopInstanceUID}", Program.DebugMode);

            var status = request.Dataset.GetSingleValue<string>(DicomTag.PerformedProcedureStepStatus);
            if (status == "COMPLETED")
            {
                // most vendors send some informations with the mpps-completed message. 
                // this information should be stored into the datbase
                var doseDescription = request.Dataset.GetSingleValueOrDefault(DicomTag.CommentsOnRadiationDose, string.Empty);
                var listOfInstanceUIDs = new List<string>();
                foreach (var seriesDataset in request.Dataset.GetSequence(DicomTag.PerformedSeriesSequence))
                {
                    // you can read here some information about the series that the modalidy created
                    //seriesDataset.Get(DicomTag.SeriesDescription, string.Empty);
                    //seriesDataset.Get(DicomTag.PerformingPhysicianName, string.Empty);
                    //seriesDataset.Get(DicomTag.ProtocolName, string.Empty);
                    foreach (var instanceDataset in seriesDataset.GetSequence(DicomTag.ReferencedImageSequence))
                    {
                        // here you can read the SOPClassUID and SOPInstanceUID
                        var instanceUID = instanceDataset.GetSingleValueOrDefault(DicomTag.ReferencedSOPInstanceUID, string.Empty);
                        if (!string.IsNullOrEmpty(instanceUID)) listOfInstanceUIDs.Add(instanceUID);
                    }
                }
                var ok = MppsSource.SetCompleted(requestedSopInstanceUID, doseDescription, listOfInstanceUIDs);

                return new DicomNSetResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
            }
            else if (status == "DISCONTINUED")
            {
                // some vendors send a reason code or description with the mpps-discontinued message
                // var reason = request.Dataset.Get(DicomTag.PerformedProcedureStepDiscontinuationReasonCodeSequence);
                var ok = MppsSource.SetDiscontinued(requestedSopInstanceUID, string.Empty);

                return new DicomNSetResponse(request, ok ? DicomStatus.Success : DicomStatus.ProcessingFailure);
            }
            else
            {
                return new DicomNSetResponse(request, DicomStatus.InvalidAttributeValue);
            }
        }


        #region not supported methods but that are required because of the interface

        public DicomNDeleteResponse OnNDeleteRequest(DicomNDeleteRequest request)
        {
            LogHelper.Info("receiving N-Delete, not supported", Program.DebugMode);
            return new DicomNDeleteResponse(request, DicomStatus.UnrecognizedOperation);
        }

        public DicomNEventReportResponse OnNEventReportRequest(DicomNEventReportRequest request)
        {
            LogHelper.Info("receiving N-Event, not supported", Program.DebugMode);
            return new DicomNEventReportResponse(request, DicomStatus.UnrecognizedOperation);
        }

        public DicomNGetResponse OnNGetRequest(DicomNGetRequest request)
        {
            LogHelper.Info("receiving N-Get, not supported", Program.DebugMode);
            return new DicomNGetResponse(request, DicomStatus.UnrecognizedOperation);
        }

        public DicomNActionResponse OnNActionRequest(DicomNActionRequest request)
        {
            LogHelper.Info("receiving N-Action, not supported", Program.DebugMode);
            return new DicomNActionResponse(request, DicomStatus.UnrecognizedOperation);
        }

        #endregion

    }
}


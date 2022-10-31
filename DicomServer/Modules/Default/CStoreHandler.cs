using Dicom;
using Dicom.Imaging;
using Dicom.Network;
using DicomServer.CStore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms.DataVisualization.Charting;

namespace DicomServer.Modules.Default
{
    public enum FileType { DCM, PDF, TXT, RTF, DOC, IMG, PNG, JPG, JPEG, GIF, BMP, TIFF, MP4, UNKNOWN }

    public class CStoreHandler : ICStoreSource
    {
        object CStoreHandlerLock = new object();
        private Module _Module;

        private string CodeMeaning; //used to sort measurements because some measurements have more than one value

        private string TextValue;
        private string CodeValue;
        private string SRMessage;
        private int MeasureCount;

        List<Equipment> EqptList;
        Equipment Eqpt;
        private bool EnableText;
        private bool EnableCode;
        private string ImageComments; //Gabriel BUG 6225 - Lista DO

        public CStoreHandler(Module module)
        {
            _Module = module;
        }

        public DicomStatus Store(DicomCStoreRequest request)
        {
            string accn = string.Empty; // Accession Number
            string ptid = string.Empty; // Patient ID            
            var type = FileType.UNKNOWN;

            DicomStatus measurementsResult = DicomStatus.ProcessingFailure;
            //DicomStatus waveformResult = DicomStatus.ProcessingFailure;

            // Required Fields //
#if !DEBUG
            try
            {
                accn = request.Dataset.GetSingleValue<string>(DicomTag.AccessionNumber);
                ptid = request.Dataset.GetSingleValue<string>(DicomTag.PatientID);

                if (string.IsNullOrEmpty(accn) || string.IsNullOrEmpty(ptid)) //Accession Number and Patient ID are Required
                {
                    return DicomStatus.MissingAttributeValue;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Store request is missing Accn or Ptid", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.MissingAttribute;
            }
            //
#endif
            //Waveform
            /*DicomSequence waveformSequence;
            request.Dataset.TryGetSequence(DicomTag.WaveformSequence, out waveformSequence);
            if (waveformSequence != null && waveformSequence.Count() > 0)
            {
                try
                {
                    waveformResult = StoreWaveform(request);
                    return waveformResult;
                }
                catch (Exception e)
                {
                    LogHelper.Error($"({_Module.CSAETitle}) Store request failed on waveform parsing", Program.DebugMode);
                    LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                    return DicomStatus.ProcessingFailure;
                }
            }*/

            //Measurements
            if (_Module.CSStoreMeasuremnts)
            {
                try
                {
                    measurementsResult = StoreMeasurements(request);
                    if (!_Module.CSStorePdfs && !_Module.CSStoreImages)
                        return measurementsResult;
                }
                catch (Exception e)
                {
                    LogHelper.Error($"({_Module.CSAETitle}) Store request failed on measurements parsing", Program.DebugMode);
                    LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                    return DicomStatus.ProcessingFailure;
                }
            }
            //

            //Teste Multiframe
            /*
            #if DEBUG
            if(RequestIsMultiframe(request))
            {
                return StoreMultiframe(request);
            }
            #endif
            */

            // Trying to find an image inside the dcm
            try
            {
                DicomImage image = new DicomImage(request.Dataset); // It will error if dcm does not contain the pixel data tag
                type = FileType.BMP;
            }
            catch
            {
                //if it has no image, try to find a document
                try
                {
                    byte[] edoc; // Encapsulated Document           
                    string mmtp = string.Empty; // MIME Type Of Encapsulated Document

                    request.Dataset.TryGetValues<byte>(DicomTag.EncapsulatedDocument, out edoc);

                    if (!(edoc is null)) //A document must be followed by it's mime type
                    {
                        mmtp = request.Dataset.GetSingleValue<string>(DicomTag.MIMETypeOfEncapsulatedDocument);
                        if (string.IsNullOrEmpty(mmtp)) return DicomStatus.MissingAttributeValue;

                        try
                        {
                            type = GetFileType(mmtp);
                        }
                        catch (Exception e)
                        {
                            LogHelper.Error($"({_Module.CSAETitle}) Store request failed to find encapsulated file type", Program.DebugMode);
                            LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                            return DicomStatus.ProcessingFailure;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogHelper.Error($"({_Module.CSAETitle}) Store request did not contain any parseable image, document or measurement", Program.DebugMode);
                    LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                    return DicomStatus.MissingAttribute;
                }

            }

            if (new[] {
                FileType.IMG,  FileType.PNG, FileType.JPG, FileType.DCM,
                FileType.JPEG, FileType.GIF, FileType.BMP, FileType.TIFF
            }.Contains(type)) //if type in (x, y, z) then it's an image
            {
                return StoreImage(request);
            }
            else if (new[] { FileType.PDF, FileType.TXT, FileType.RTF, FileType.DOC }.Contains(type))
            {
                return StoreDocument(request);
            }
            else
            {
                if (_Module.CSStoreMeasuremnts) return measurementsResult;
                return DicomStatus.NoSuchActionType;
            }
        }

        public DicomStatus StoreImage(DicomCStoreRequest imageRequest)
        {
            if (!_Module.CSStoreImages) return DicomStatus.NoSuchActionType;

            string exam, ptid, dtnw, filePath;
            FileType type;

            string fileDir = Program.AssemblyLocation;

            try
            {
                exam = imageRequest.Dataset.GetSingleValue<string>(DicomTag.AccessionNumber);
                ptid = imageRequest.Dataset.GetSingleValue<string>(DicomTag.PatientID);

                type = FileType.BMP;
                dtnw = DateTime.Now.ToString("yyyyMMdd");

                //Gabriel BUG 6225 - Lista DO
                if (_Module.CSEnableImageComments)
                {
                    ImageComments = string.Empty;
                    imageRequest.Dataset.TryGetSingleValue<string>(DicomTag.ImageComments, out ImageComments);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Image Store request is missing Accn/Ptid", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.MissingAttribute;
            }

            try
            {
                filePath = BuildStoreDirectory($"{_Module.CSImgFolder}\\{ptid}\\{exam}\\{Guid.NewGuid()}.{type.ToString().ToLower()}");
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Image Store request failed to build the store directory", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            try
            {
                DicomImage dicomImage = new DicomImage(imageRequest.Dataset);

                //var PixelData = DicomPixelData.Create(imageRequest.Dataset);
                //var PhotometricInterpretation = PixelData.PhotometricInterpretation;

                //var _pixelData = PixelDataFactory.Create(PixelData, 0);                
                //int[] output;
                //_pixelData.Render();

                IImage image = dicomImage.RenderImage();
                Bitmap bitmap = image.AsClonedBitmap();
                bitmap.Save(filePath);


                image.Dispose();
                bitmap.Dispose();
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Image Store request server failed to decapsulate image", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            try
            {
                CompressImage(filePath);
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Image Store request server failed to compress image", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            try
            {
                //SendStoreHttpMessage(exam, filePath, imageRequest, "", ImageComments); //Gabriel BUG 6225 - Lista DO
                SendStoreHttpMessage(exam, filePath, imageRequest); //Gabriel BUG 6225 - Lista DO
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Image Store request server failed to send http message after decapsulation", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            return DicomStatus.Success;
        }

        public DicomStatus StoreDocument(DicomCStoreRequest documentRequest)
        {
            if (!_Module.CSStorePdfs) return DicomStatus.NoSuchActionType;

            string exam = string.Empty, ptid, dtnw, filePath, manufacturer;
            byte[] file;
            FileType type;

            string fileDir = Program.AssemblyLocation;

            try
            {
#if !DEBUG
                exam = documentRequest.Dataset.GetSingleValue<string>(DicomTag.AccessionNumber);
#endif
                ptid = documentRequest.Dataset.GetSingleValue<string>(DicomTag.PatientID);
                file = documentRequest.Dataset.GetValues<byte>(DicomTag.EncapsulatedDocument);
                type = GetFileType(documentRequest.Dataset.GetSingleValue<string>(DicomTag.MIMETypeOfEncapsulatedDocument));
                
                documentRequest.Dataset.TryGetValue(DicomTag.Manufacturer, 0, out manufacturer);

                dtnw = DateTime.Now.ToString("yyyyMMdd");

                //Gabriel BUG 6225 - Lista DO
                if (_Module.CSEnableImageComments)
                {
                    ImageComments = string.Empty;
                    documentRequest.Dataset.TryGetSingleValue<string>(DicomTag.ImageComments, out ImageComments);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Document Store request is missing Accn/Ptid/Type/File", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.MissingAttribute;
            }

            try
            {
                filePath = BuildStoreDirectory($"{_Module.CSDocFolder}\\{ptid}\\{exam}\\{Guid.NewGuid()}.{type.ToString().ToLower()}");
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Document Store request failed to build the store directory", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            try
            {
                File.WriteAllBytes(filePath, file);
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Document Store request server failed to write bytes into file", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            try
            {
                //SendStoreHttpMessage(exam, filePath, documentRequest, manufacturer, ImageComments); //Gabriel BUG 6225 - Lista DO
                SendStoreHttpMessage(exam, filePath, documentRequest, manufacturer); //Gabriel BUG 6225 - Lista DO
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Document Store request server failed to send http message after decapsulation", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            return DicomStatus.Success;
        }

        public DicomStatus StoreWaveform(DicomCStoreRequest waveformRequest)
        {
            try
            {
                string ptid, accn, name;
                waveformRequest.Dataset.TryGetSingleValue<string>(DicomTag.PatientID, out ptid);
                waveformRequest.Dataset.TryGetSingleValue<string>(DicomTag.AccessionNumber, out accn);
                waveformRequest.Dataset.TryGetSingleValue<string>(DicomTag.PatientName, out name);

                string manufacturer, model;
                waveformRequest.Dataset.TryGetSingleValue<string>(DicomTag.Manufacturer, out manufacturer);
                waveformRequest.Dataset.TryGetSingleValue<string>(DicomTag.ManufacturerModelName, out model);

                int scale;
                waveformRequest.Dataset.TryGetSingleValue<int>(DicomTag.WaveformDataDisplayScale, out scale);
                if (scale == 0) scale = 25; //This is de default scale (25 mm/s)


                List<Waveform> waveList = new List<Waveform>();
                foreach (DicomDataset waveData in waveformRequest.Dataset.GetSequence(DicomTag.WaveformSequence))
                {
                    Waveform wave = new Waveform();
                    wave.BitsAllocated = waveData.GetValues<ushort>(DicomTag.WaveformBitsAllocated)[0];
                    wave.Originality = waveData.GetValues<string>(DicomTag.WaveformOriginality)[0];
                    wave.SampleInterpretation = waveData.GetValues<string>(DicomTag.WaveformSampleInterpretation)[0];
                    wave.SamplingFrequency = waveData.GetValues<int>(DicomTag.SamplingFrequency)[0];
                    wave.WFChannelCount = waveData.GetValues<ushort>(DicomTag.NumberOfWaveformChannels)[0];
                    wave.WFSamplesCount = waveData.GetValues<uint>(DicomTag.NumberOfWaveformSamples)[0];

                    byte[] byteArray = waveData.GetValues<byte>(DicomTag.WaveformData); //Always decompress as byte[]
                    wave.WaveFormData = byteArray;

                    //Convert to short[] if its 16 bit allocation and SS interpretation
                    short[] shortArray = new short[(byteArray.Length / 2)]; //<-- divided by 2 because byte = 8 bits and short = 16 bits
                    Buffer.BlockCopy(byteArray, 0, shortArray, 0, byteArray.Length);
                    //

                    foreach (DicomDataset chData in waveData.GetSequence(DicomTag.ChannelDefinitionSequence))
                    {
                        WFChannel ch = new WFChannel();
                        ch.Name = chData.GetSequence(DicomTag.ChannelSourceSequence).Items[0].GetValues<string>(DicomTag.CodeMeaning)[0];
                        ch.BitsStored = chData.GetValues<ushort>(DicomTag.WaveformBitsStored)[0];
                        try { ch.FilterHigh = chData.GetValues<float>(DicomTag.FilterHighFrequency)[0]; } catch { ch.FilterHigh = 0; }
                        try { ch.FilterLow = chData.GetValues<float>(DicomTag.FilterLowFrequency)[0]; } catch { ch.FilterLow = 0; }
                        try { ch.FilterNoch = chData.GetValues<float>(DicomTag.NotchFilterFrequency)[0]; } catch { ch.FilterNoch = 0; }
                        ch.Sensitivity = chData.GetValues<float>(DicomTag.ChannelSensitivity)[0];

                        /* Formula to get the distance between ticks (X) of each sample (Y)
                        * e = 25 mm/s => default scale. 
                        * m = 4.1 px/mm => default monitor pixel density
                        * [(e / SamplingFrequency) * m] = distance between pixels (x dislocation for each Y)
                        */
                        double posXDislocation = ((scale / (double)wave.SamplingFrequency) * 4.1);
                        double posX = 0;
                        //

                        //Setup new chart
                        Chart chart = ChartManager.CreateNewWaveformChart(name, accn, manufacturer, model, scale, ch.Name, shortArray.Max());
                        Series serie = ChartManager.AddNewWaveformSeries(chart);
                        //

                        //waveform data values are expected to have interleaved encoding, 
                        //incrementing by channel and then by sample(i.e., C1S1, C2S1, C3S1, … CnS1, C1S2, C2S2, C3S2, … CnSm)
                        //This loop will step acoarding to the channel count to obtain all samples for the current channel
                        for (int i = wave.ChannelList.Count; i < shortArray.Length; i += wave.WFChannelCount)
                        {
                            WFSample sp = new WFSample();
                            sp.X = posX += posXDislocation;
                            sp.Y = Convert.ToDouble(shortArray[i]);
                            ch.SampleList.Add(sp);

                            //Apply the filters to add values on the chart
                            if (sp.Y >= ch.FilterHigh || sp.Y <= ch.FilterLow)
                                serie.Points.AddXY(sp.X, sp.Y);
                        }

                        //Add channel to the listds
                        wave.ChannelList.Add(ch);

                        ChartManager.SaveWaveformChart(chart);
                    }

                    waveList.Add(wave);
                }

                try
                {
                    // SendStoreHttpMessage(SRMessage);
                }
                catch (Exception e)
                {
                    LogHelper.Error($"({_Module.CSAETitle}) Waveform Store request server failed to send http message after decapsulation", Program.DebugMode);
                    LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                    return DicomStatus.ProcessingFailure;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Waveform Store request server failed", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.StackTrace}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            return DicomStatus.Success;
        }

        public DicomStatus StoreMultiframe(DicomCStoreRequest multiframeRequest)
        {
            //if (!_Module.CSStoreImages) return DicomStatus.NoSuchActionType;

            string exam = "0", ptid, dtnw, filePath;
            FileType type;

            string fileDir = Program.AssemblyLocation;

            try
            {
#if !DEBUG
                exam = multiframeRequest.Dataset.GetSingleValue<string>(DicomTag.AccessionNumber);
#endif
                ptid = multiframeRequest.Dataset.GetSingleValue<string>(DicomTag.PatientID);

                type = FileType.MP4;
                dtnw = DateTime.Now.ToString("yyyyMMdd");
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Multiframe Store request is missing Accn/Ptid", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.MissingAttribute;
            }

            try
            {
                filePath = BuildStoreDirectory($"{_Module.CSImgFolder}\\{ptid}\\{exam}\\{Guid.NewGuid()}.{type.ToString().ToLower()}");
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Multiframe Store request failed to build the store directory", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            try
            {
                int frameCount = multiframeRequest.Dataset.GetSingleValue<int>(DicomTag.NumberOfFrames);
                string guid = Guid.NewGuid().ToString();

                List<string> extractedFrameList = new List<string>();
                FFMpegSharp.ImageInfo[] imageInfoArray = new FFMpegSharp.ImageInfo[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    string imagePath = $"{_Module.CSImgFolder}\\{ptid}\\{exam}\\{guid}_{i}.{FileType.PNG.ToString().ToLower()}";
                    DicomImage dicomImage = new DicomImage(multiframeRequest.Dataset, i);

                    IImage image = dicomImage.RenderImage(i);
                    Bitmap bitmap = image.AsClonedBitmap();
                    bitmap.Save(imagePath);

                    image.Dispose();
                    bitmap.Dispose();

                    extractedFrameList.Add(imagePath);
                    imageInfoArray[i] = FFMpegSharp.ImageInfo.FromPath(imagePath);
                }

                using (FFMpegSharp.FFMPEG.FFMpeg encoder = new FFMpegSharp.FFMPEG.FFMpeg())
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    encoder.JoinImageSequence(fileInfo, 25, //Muti Frame DICOM Images play at a fixed frame rate of 25 frames per second. 
                            imageInfoArray);
                };

                /*foreach (string frame in extractedFrameList)
                    File.Delete(frame);*/
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Multiframe Store request server failed to decapsulate image", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }


            try
            {
                SendStoreHttpMessage(exam, filePath, multiframeRequest); //Gabriel BUG 6225 - Lista DO
            }
            catch (Exception e)
            {
                LogHelper.Error($"({_Module.CSAETitle}) Multiframe Store request server failed to send http message after decapsulation", Program.DebugMode);
                LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                return DicomStatus.ProcessingFailure;
            }

            return DicomStatus.Success;
        }

        #region MEASUREMENTS
        public DicomStatus StoreMeasurements(DicomCStoreRequest measurementsRequest)
        {
            lock (CStoreHandlerLock)
            {
                try
                {
                    string exam = string.Empty;
                    string manufacturer;
                    string model;
                    ImageComments = string.Empty; //Gabriel BUG 6225 - Lista DO
#if !DEBUG
                    exam = measurementsRequest.Dataset.GetSingleValue<string>(DicomTag.AccessionNumber);
#endif
                    measurementsRequest.Dataset.TryGetSingleValue<string>(DicomTag.Manufacturer, out manufacturer);
                    measurementsRequest.Dataset.TryGetSingleValue<string>(DicomTag.ManufacturerModelName, out model);
                    //Gabriel BUG 6225 - Lista DO
                    if (_Module.CSEnableImageComments)
                        measurementsRequest.Dataset.TryGetSingleValue<string>(DicomTag.ImageComments, out ImageComments);

                    TextValue = string.Empty;
                    CodeValue = string.Empty;
                    SRMessage = string.Empty;
                    MeasureCount = 0;

                    LoadEquipments();
                    SetEquipment(model);

                    ExtractReport(measurementsRequest.Dataset);

                    switch (_Module.CSMirthMessageType)
                    {
                        case (int)MirthMessageType.JSON:
                            SRMessage = SRMessage.Remove(SRMessage.LastIndexOf(","));
                            SRMessage =
                                $"{{\"OrderNumber\": \"{exam}\", " +
                                $"\"Manufacturer\": \"{manufacturer + " " + model}\", " +
                                $"\"MeasureCount\": \"{MeasureCount}\", " +
                                $"\"ImageComments\": {{ {ImageComments} }}" + //Gabriel BUG 6225 - Lista DO
                                $"\"Measurements\": {{ {SRMessage} }}" +
                                $"}}";
                            break;
                        case (int)MirthMessageType.XML:
                            SRMessage =
                                $"<Measurements><OrderNumber>{exam}</OrderNumber>" +
                                $"<Manufacturer>{manufacturer + " " + model}</Manufacturer>" +
                                $"<ImageComments>{ImageComments}</ImageComments>" + //Gabriel BUG 6225 - Lista DO
                                $"{SRMessage}</Measurements>";
                            break;
                        default:
                            SRMessage = SRMessage.Remove(SRMessage.LastIndexOf(","));
                            SRMessage =
                                $"{{\"OrderNumber\": \"{exam}\", " +
                                $"\"Manufacturer\": \"{manufacturer + " " + model}\", " +
                                $"\"MeasureCount\": \"{MeasureCount}\", " +
                                $"\"ImageComments\": {{ {ImageComments} }}" + //Gabriel BUG 6225 - Lista DO
                                $"\"Measurements\": {{ {SRMessage} }}" +
                                $"}}";
                            break;
                    }
#if DEBUG
                    Console.WriteLine(SRMessage);
#endif
                    try
                    {
                        SendStoreHttpMessage(SRMessage);
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error($"({_Module.CSAETitle}) Measurement Store request server failed to send http message after decapsulation", Program.DebugMode);
                        LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                        return DicomStatus.ProcessingFailure;
                    }
                }
                catch (Exception e)
                {
                    LogHelper.Error($"({_Module.CSAETitle}) Measurement Store request server failed", Program.DebugMode);
                    LogHelper.Error($"({_Module.CSAETitle}) {e.ToString()}", Program.DebugMode);
                    LogHelper.Error($"({_Module.CSAETitle}) {e.StackTrace}", Program.DebugMode);
                    return DicomStatus.ProcessingFailure;
                }

                return DicomStatus.Success;
            }
        }

        public void LoadEquipments()
        {
            EqptList = new List<Equipment>();
            var sections = CfgHelper.ReadAllSections();

            foreach (string section in sections.Split(';'))
            {
                EqptList.Add(new Equipment
                {
                    Name = section,
                    CodeMeaningForTextVal = CfgHelper.Read(section, "TextVal", true).Split(';').ToList(),
                    CodeMeaningForCodeVal = CfgHelper.Read(section, "CodeVal", true).Split(';').ToList()
                });
            }
        }

        public void SetEquipment(string name)
        {
            Eqpt = EqptList.FirstOrDefault(x => x.Name == name);
            EnableText = false;
            EnableCode = false;
        }

        public void ExtractReport(DicomDataset d)
        {
            //Gabriel BUG 6225 - Lista DO
            //Added this verification because DicomServer was throwing an error and could not proceed
            if (Eqpt == null)
            {
                LogHelper.Warning($"Could not extract Report... Equipment not set", Program.DebugMode);
                return;
            }
                
            var set = new Queue<DicomItem>(d ?? new DicomDataset());

            while (set.Count > 0)
            {
                DicomItem item = null;
                if (set.Count > 0) item = set.Peek();

                if (item != null)
                {
                    if (item is DicomSequence)
                    {
                        DicomSequence sequence = (item as DicomSequence);
                        if (sequence != null)
                        {
                            int count = sequence.Items.Count;
                            for (int i = 0; i < count; i++)
                            {
                                DicomDataset subset = sequence.Items[i];
                                ExtractReport(subset);
                            }
                        }
                    }
                    else
                    {
                        string tagname = item.Tag.DictionaryEntry.Name;
                        if (item is DicomElement)
                        {
                            var element = (item as DicomElement);

                            string value = "<large value not displayed>";
                            if (element.Length <= 2048) value = String.Join("\\", element.Get<string[]>());

                            if (tagname.Equals("Code Meaning"))
                            {
                                if (Eqpt.CodeMeaningForCodeVal.Contains(value))
                                {
                                    EnableCode = true;
                                    CodeMeaning = value.ToUpper().Replace(" ", "_");
                                }
                                if (Eqpt.CodeMeaningForTextVal.Contains(value))
                                {
                                    EnableText = true;
                                    CodeMeaning = value.ToUpper().Replace(" ", "_");
                                }
                            }
                            if (tagname.Equals("Text Value") && EnableText)
                            {
                                TextValue = value.ToUpper().Replace(" ", "_");
                                EnableText = false;
                            }
                            if ((tagname.Equals("Text Value") && EnableCode)
                             || (tagname.Equals("Code Value") && EnableCode))
                            {
                                CodeValue = value.ToUpper().Replace(" ", "_");
                                EnableCode = false;
                            }
                            if (tagname.Equals("Numeric Value"))
                            {
                                switch (_Module.CSMirthMessageType)
                                {
                                    case (int)MirthMessageType.XML:
                                        SRMessage += $"<Measure>" +
                                            $"<TextValue>{TextValue}_{CodeMeaning}</TextValue>" +
                                            $"<NumericValue>{value}</NumericValue>" +
                                            $"<CodeValue>{CodeValue.ToLower()}</CodeValue>" +
                                            $"</Measure>";
                                        break;
                                    case (int)MirthMessageType.JSON:
                                    default:
                                        SRMessage += $"\"Measure{MeasureCount}\": {{" +
                                            $"\"TextValue\": \"{TextValue}_{CodeMeaning}\", " +
                                            $"\"NumericValue\": \"{value}\", " +
                                            $"\"CodeValue\": \"{CodeValue.ToLower()}\" }},";
                                        break;
                                }
                                MeasureCount++;
                            }
                        }
                    }
                    set.Dequeue();
                    continue;
                }
            }
        }
        #endregion

        #region HTTP
        private void SendStoreHttpMessage(string message)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_Module.CSMirthEndpoint);
            request.Timeout = 3000;
            request.Method = "POST";

            byte[] messageBytes = Encoding.GetEncoding(1252).GetBytes(message);
            request.ContentLength = messageBytes.Length;

            Stream reqStream = request.GetRequestStream();
            reqStream.Write(messageBytes, 0, messageBytes.Length);
            reqStream.Flush();
            reqStream.Close();
        }

        //Gabriel BUG 6225 - Lista DO
        //private void SendStoreHttpMessage(string exam, string path, DicomCStoreRequest req, string manufacturer = "", string imageComments = "")
        private void SendStoreHttpMessage(string exam, string path, DicomCStoreRequest req, string manufacturer = "")
        {
            string messageText = string.Empty;

            switch (_Module.CSMirthMessageType)
            {
                case (int)MirthMessageType.JSON:
                    //messageText = GetHttpJSONMessage(exam, path, req, manufacturer, imageComments); //Gabriel BUG 6225 - Lista DO
                    messageText = GetHttpJSONMessage(exam, path, req, manufacturer); //Gabriel BUG 6225 - Lista DO
                    break;
                case (int)MirthMessageType.XML:
                    //messageText = GetHttpXMLMessage(exam, path, req, manufacturer, imageComments); //Gabriel BUG 6225 - Lista DO
                    messageText = GetHttpXMLMessage(exam, path, req, manufacturer); //Gabriel BUG 6225 - Lista DO
                    break;
                default:
                    //messageText = GetHttpJSONMessage(exam, path, req, manufacturer, imageComments); //Gabriel BUG 6225 - Lista DO
                    messageText = GetHttpJSONMessage(exam, path, req, manufacturer); //Gabriel BUG 6225 - Lista DO
                    break;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_Module.CSMirthEndpoint);
            request.Timeout = 3000;
            request.Method = "POST";

            byte[] messageBytes = Encoding.GetEncoding(1252).GetBytes(messageText);
            request.ContentLength = messageBytes.Length;

            Stream reqStream = request.GetRequestStream();
            reqStream.Write(messageBytes, 0, messageBytes.Length);
            reqStream.Flush();
            reqStream.Close();
        }
        #endregion

        #region UTIL
        private FileType GetFileType(string mimeType)
        {
            var type = FileType.UNKNOWN;
            switch (mimeType.ToLower())
            {
                case "application/pdf":
                    type = FileType.PDF;
                    break;

                default:
                    type = FileType.UNKNOWN;
                    break;
            }
            return type;
        }

        private string BuildStoreDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return filePath;
        }

        private byte[] ConvertBitArrayToByteArray(ushort[] pixelData)
        {
            List<byte> byteData = new List<byte>();
            for (int i = 0; i < pixelData.Length; i++)
            {
                byteData.AddRange(BitConverter.GetBytes(pixelData[i]));
            }
            return byteData.ToArray();
        }

        private void CompressImage(string filePath)
        {
            if (_Module.CSMaxImgSizeKB == 0) return;

            var size = Math.Round((double)(((double)(new FileInfo(filePath).Length) / 1024) / 1024), 2); //size in MB
            if (size <= (double)(_Module.CSMaxImgSizeKB / 1024)) return;

            Image bigImage = Image.FromFile(filePath);
            var width = Convert.ToInt32(bigImage.Width / 1.05);
            var heigth = Convert.ToInt32(bigImage.Height / 1.05);

            Bitmap bmp = new Bitmap(width, heigth, bigImage.PixelFormat);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;

                Rectangle bigImageRect = new Rectangle(0, 0, bigImage.Width, bigImage.Height);
                Rectangle newImageRect = new Rectangle(0, 0, width, heigth);

                g.DrawImage(bigImage, newImageRect, bigImageRect, GraphicsUnit.Pixel);
                bmp.SetResolution(bigImage.HorizontalResolution, bigImage.VerticalResolution);
            }

            using (var stream = new MemoryStream())
            {
                bmp.Save(stream, ImageFormat.Jpeg);
                bigImage.Dispose();
                File.WriteAllBytes(filePath, stream.ToArray());
                stream.Dispose();
            }
            bmp.Dispose();
            CompressImage(filePath);
        }

        //Gabriel BUG 6225 - Lista DO
        //private string GetHttpJSONMessage(string exam, string path, DicomCStoreRequest request, string manufacturer = "", string imageComments = "")
        private string GetHttpJSONMessage(string exam, string path, DicomCStoreRequest request, string manufacturer = "")
        {
            string additionalTags = string.Empty;
            List<string> configuredTags = _Module.CSVerificationTags.Split('|').ToList();

            foreach (string tag in configuredTags)
            {
                switch (tag)
                {
                    case "PatientBirthDate":
                        {
                            string patientBirthDate;
                            request.Dataset.TryGetValue<string>(DicomTag.PatientBirthDate, 0, out patientBirthDate);

                            additionalTags += $", \"PatientBirthDate\": \"{patientBirthDate}\"";
                            break;
                        }
                    case "PatientName":
                        {
                            string patientName;
                            request.Dataset.TryGetValue<string>(DicomTag.PatientName, 0, out patientName);
                            
                            additionalTags += $", \"PatientName\": \"{patientName}\"";
                            break;
                        }
                    case "PatientID":
                        {
                            string patientId;
                            request.Dataset.TryGetValue<string>(DicomTag.PatientID, 0, out patientId);

                            additionalTags += $", \"PatientId\": \"{patientId}\"";
                            break;
                        }


                    case "PatientSex":
                        {
                            string patientSex;
                            request.Dataset.TryGetValue<string>(DicomTag.PatientSex, 0, out patientSex);

                            additionalTags += $", \"PatientSex\": \"{patientSex}\"";
                            break;
                        }
                    default: break;
                }
            }

            //return $"{{\"OrderNumber\": \"{exam}\", \"Path\": \"{path.Replace("\\", "\\\\")}\", \"Description\": \"DicomServer\", \"Manufacturer\": \"{manufacturer}\", \"ImageComments\": \"{imageComments}\", {additionalTags}}}";
            return $"{{\"OrderNumber\": \"{exam}\", \"Path\": \"{path.Replace("\\", "\\\\")}\", \"Description\": \"DicomServer\", \"Manufacturer\": \"{manufacturer}\"{additionalTags}}}";
        }

        //Gabriel BUG 6225 - Lista DO
        //private string GetHttpXMLMessage(string exam, string path, DicomCStoreRequest request, string manufacturer = "", string imageComments = "")
        private string GetHttpXMLMessage(string exam, string path, DicomCStoreRequest request, string manufacturer = "")
        {
            string additionalTags = string.Empty;
            List<string> configuredTags = _Module.CSVerificationTags.Split('|').ToList();

            foreach (string tag in configuredTags)
            {
                switch (tag)
                {
                    case "PatientBirthDate":
                        {
                            string patientBirthDate;
                            request.Dataset.TryGetValue<string>(DicomTag.PatientBirthDate, 0, out patientBirthDate);

                            additionalTags += $"<PatientBirthDate>{patientBirthDate}</PatientBirthDate>";
                            break;
                        }
                    case "PatientName":
                        {
                            string patientName;
                            request.Dataset.TryGetValue<string>(DicomTag.PatientName, 0, out patientName);

                            additionalTags += $"<PatientName>{patientName}</PatientName>";
                            break;
                        }
                    case "PatientId":
                        {
                            string patientId;
                            request.Dataset.TryGetValue<string>(DicomTag.PatientID, 0, out patientId);

                            additionalTags += $"<PatientId>{patientId}</PatientId>";
                            break;
                        }
                    case "PatientSex":
                        {
                            string patientSex;
                            request.Dataset.TryGetValue<string>(DicomTag.PatientSex, 0, out patientSex);

                            additionalTags += $"<PatientSex>{patientSex}</PatientSex>";
                            break;
                        }
                    default: break;
                }
            }

            //return $"<Message><OrderNumber>{exam}</OrderNumber><Path>{path.Replace("\\", "\\\\")}</Path><Manufacturer>{manufacturer}</Manufacturer><ImageComments>{imageComments}</ImageComments>{additionalTags}</Message>";
            return $"<Message><OrderNumber>{exam}</OrderNumber><Path>{path.Replace("\\", "\\\\")}</Path><Manufacturer>{manufacturer}</Manufacturer>{additionalTags}</Message>";
        }
        #endregion

        private bool RequestIsImage(DicomCStoreRequest request)
        {
            bool response = false;
            try
            {
                DicomImage imageF0 = new DicomImage(request.Dataset, 0);
                response = true; //Se encontrar uma imagem no frame 0 o request é de singleframe 

                DicomImage imageF1 = new DicomImage(request.Dataset, 1);
                response = false;//Se encontrar uma imagem no frame 1 o request é de multiframe
            }
            catch
            {
            }
            return response;
        }
        private bool RequestIsDocument(DicomCStoreRequest request)
        {
            return request.Dataset.TryGetValues<byte>(DicomTag.EncapsulatedDocument, out byte[] document);
        }

        private bool RequestIsWaveform(DicomCStoreRequest request)
        {
            return request.Dataset.TryGetSequence(DicomTag.WaveformSequence, out DicomSequence waveform);
        }

        private bool RequestIsMultiframe(DicomCStoreRequest request)
        {
            bool response = false;
            try
            {
                DicomImage imageF1 = new DicomImage(request.Dataset, 1);
                response = true;//Se encontrar uma imagem no frame 1 o request é de multiframe
            }
            catch
            {
            }
            return response;
        }
    }
}
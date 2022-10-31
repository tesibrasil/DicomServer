using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicomServer.CStore.Model
{
    public class Waveform
    {
        public Waveform()
        {
            ChannelList = new List<WFChannel>();
        }
        public ushort WFChannelCount { get; set; }
        public List<WFChannel> ChannelList { get; set; }
        public uint WFSamplesCount { get; set; }
        public int SamplingFrequency { get; set; }
        public string Originality { get; set; }
        public string SampleInterpretation { get; set; }
        public ushort BitsAllocated { get; set; }
        public byte[] WaveFormData { get; set; }

        /*public override string ToString()
        {
            string header = 
                $"{Environment.NewLine} WFChannelCount:         {WFChannelCount} " +
                $"{Environment.NewLine} WFSamplesCount:         {WFSamplesCount} " +
                $"{Environment.NewLine} SamplingFrequency:      {SamplingFrequency} " +
                $"{Environment.NewLine} Originality:            {Originality} " +
                $"{Environment.NewLine} SampleInterpretation:   {SampleInterpretation} " +
                $"{Environment.NewLine} BitsAllocated:          {BitsAllocated} " +
                $"{Environment.NewLine} ChannelList:            ";

            LogHelper.Debug(header);

            string list = string.Empty;
            if (ChannelList != null && ChannelList.Count > 0)
                foreach (WFChannel s in ChannelList) list += s.ToString();


            header = $"{Environment.NewLine} Complete Wave Byte[]:   ";
            LogHelper.Debug(header);

            if (WaveFormData.Count() > 0)
                foreach (byte item in WaveFormData) LogHelper.Debug(item.ToString() + ",");

            return $"Waveform printed to DEBUG";
        }*/
    }

    public class WFChannel
    {
        public WFChannel()
        {
            SampleList = new List<WFSample>();
        }
        public string Name { get; set; }
        public float Sensitivity { get; set; }
        public float FilterHigh { get; set; }
        public float FilterLow { get; set; }
        public float FilterNoch { get; set; }
        public ushort BitsStored { get; set; }
        public List<WFSample> SampleList { get; set; }

        /*public override string ToString()
        {
            string header = 
               $"{Environment.NewLine}      Channel Name: {Name} " +
               $"{Environment.NewLine}          Sensitivity: {Sensitivity} " +
               $"{Environment.NewLine}          FilterHigh:  {FilterHigh} " +
               $"{Environment.NewLine}          FilterLow:   {FilterLow} " +
               $"{Environment.NewLine}          FilterNoch:  {FilterNoch} " +
               $"{Environment.NewLine}          BitsStored:  {BitsStored} " +
               $"{Environment.NewLine}          SampleList:  ";

            LogHelper.Debug(header);

            string list = string.Empty;
            if (SampleList != null && SampleList.Count > 0)
                foreach (WFSample s in SampleList) list += s.ToString();

            return $"Channel printed to DEBUG";
        }*/
    }

    public class WFSample
    { 
        public double X { get; set; }
        public double Y { get; set; }

        /*public override string ToString()
        {
            string header = 
                $"{Environment.NewLine}             Sample ByteValue: {BytePair} ";

            LogHelper.Debug(header);

            return $"Sample printed to DEBUG";
        }*/
    }
}


/*
 * I don’t see any issue in the DICOM attribute encoding, 
 * you just need to verify the waveform data matches the value in the DICOM attributes. 
 * 
 * Note that the waveform data values are expected to have interleaved encoding, 
 * incrementing by channel and then by sample (i.e., C1S1, C2S1, C3S1, … CnS1, C1S2, C2S2,C3S2, … CnSm), 
 * with no padding or explicit delimitation between successive samples. 
 * Cx denotes the channel defined in the Channel Definition Sequence ITEM in item number x.
 * 
 * https://stackoverflow.com/questions/33436615/dicomize-ecg-raw-signal-data
 */

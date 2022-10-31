using System;

namespace DicomServer.Worklist.Model
{
    [Serializable]
    public class ModalityAET
    {
        public string AET { get; set; }

        public string Modality { get; set; }

        public static string GetProcedureID(string Modality)
        {
            int procedureId = 0;
            switch (Modality)
            {
                case "CR": //Computed Radiography
                    procedureId = 1000001;
                    break;
                case "CT": //Computed Tomography
                    procedureId = 1000002;
                    break;
                case "MR": //Magnetic Resonance
                    procedureId = 1000003;
                    break;
                case "NM": //Nuclear Medicine
                    procedureId = 1000004;
                    break;
                case "US": //Ultrasound
                    procedureId = 1000005;
                    break;
                case "OT": //Other
                    procedureId = 1000006;
                    break;
                case "BI": //Biomagnetic imaging
                    procedureId = 1000007;
                    break;
                case "DG": //Diaphanography
                    procedureId = 1000008;
                    break;
                case "ES": //Endoscopy
                    procedureId = 1000009;
                    break;
                case "LS": //Laser surface scan
                    procedureId = 1000010;
                    break;
                case "PT": //Positron emission tomography (PET)
                    procedureId = 1000011;
                    break;
                case "RG": //Radiographic imaging (conventional film/screen)
                    procedureId = 1000012;
                    break;
                case "TG": //Thermography
                    procedureId = 1000013;
                    break;
                case "XA": //X-Ray Angiography
                    procedureId = 1000014;
                    break;
                case "RF": //Radio Fluoroscopy
                    procedureId = 1000015;
                    break;
                case "RTIMAGE": //Radiotherapy Image 
                    procedureId = 1000016;
                    break;
                case "RTDOSE": //Radiotherapy Dose
                    procedureId = 1000017;
                    break;
                case "RTSTRUCT": //Radiotherapy Structure Set 
                    procedureId = 1000018;
                    break;
                case "RTPLAN": //Radiotherapy Plan
                    procedureId = 1000019;
                    break;
                case "RTRECORD": //RT Treatment Record
                    procedureId = 1000020;
                    break;
                case "HC": //Hard Copy
                    procedureId = 1000021;
                    break;
                case "DX": //Digital Radiography
                    procedureId = 1000022;
                    break;
                case "MG": //Mammography
                    procedureId = 1000023;
                    break;
                case "IO": //Intra-oral Radiography
                    procedureId = 1000024;
                    break;
                case "PX": //Panoramic X-Ray
                    procedureId = 1000025;
                    break;
                case "GM": //General Microscopy
                    procedureId = 1000026;
                    break;
                case "SM": //Slide Microscopy
                    procedureId = 1000027;
                    break;
                case "XC": //External-camera Photography
                    procedureId = 1000028;
                    break;
                case "PR": //Presentation State
                    procedureId = 1000029;
                    break;
                case "AU": //Audio 
                    procedureId = 1000030;
                    break;
                case "ECG": //Electrocardiography
                    procedureId = 1000031;
                    break;
                case "EPS": //Cardiac Electrophysiology
                    procedureId = 1000032;
                    break;
                case "HD": //Hemodynamic Waveform
                    procedureId = 1000033;
                    break;
                case "SR": //SR Document
                    procedureId = 1000034;
                    break;
                case "IVUS": //Intravascular Ultrasound
                    procedureId = 1000035;
                    break;
                case "OP": //Ophthalmic Photography
                    procedureId = 1000036;
                    break;
                case "SMR": //Stereometric Relationship
                    procedureId = 1000037;
                    break;
                case "AR": //Autorefraction 
                    procedureId = 1000038;
                    break;
                case "KER": //Keratometry
                    procedureId = 1000039;
                    break;
                case "VA": //Visual Acuity
                    procedureId = 1000040;
                    break;
                case "SRF": //Subjective Refraction
                    procedureId = 1000041;
                    break;
                case "OCT": //Optical Coherence Tomography (non-Ophthalmic)
                    procedureId = 1000042;
                    break;
                case "LEN": //Lensometry
                    procedureId = 1000043;
                    break;
                case "OPV": //Ophthalmic Visual Field
                    procedureId = 1000044;
                    break;
                case "OPM": //Ophthalmic Mapping
                    procedureId = 1000045;
                    break;
                case "OAM": //Ophthalmic Axial Measurements 
                    procedureId = 1000046;
                    break;
                case "RESP": //Respiratory Waveform
                    procedureId = 1000047;
                    break;
                case "KO": //Key Object Selection
                    procedureId = 1000048;
                    break;
                case "SEG": //Segmentation
                    procedureId = 1000049;
                    break;
                case "REG": //Registration
                    procedureId = 1000050;
                    break;
                case "OPT": //Ophthalmic Tomography
                    procedureId = 1000051;
                    break;
                case "BDUS": //Bone Densitometry (ultrasound)
                    procedureId = 1000052;
                    break;
                case "BMD": //Bone Densitometry (X-Ray)
                    procedureId = 1000053;
                    break;
                case "DOC": //Document
                    procedureId = 1000054;
                    break;
                case "FID": //Fiducials PLAN Plan
                    procedureId = 1000055;
                    break;
                case "IOL": //Intraocular Lens Data
                    procedureId = 1000056;
                    break;
                case "IVOCT": //Intravascular Optical Coherence Tomography
                    procedureId = 1000057;
                    break;
                default:
                    procedureId = 1000058;
                    break;
            }

            return procedureId.ToString();
        }
    }
}
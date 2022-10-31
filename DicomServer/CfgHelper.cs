using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DicomServer
{
    public enum CfgKeys
    {
        SaveDcm, DcmFolder,

        DBConn,
        UseWLSCP, WLAETitle, WLPort, ItemsLoaderTimeSpan, WLUsesAssociationCallingAE, WLViewName,

        UseCSSCP, CSAETitle, CSPort, CSDocFolder, CSImgFolder, CSMirthEndpoint,
        CSMirthMessageType, CSMaxImgSizeKB, CSStorePdfs, CSStoreImages, CSStoreMeasuremnts, CSVerificationTags, CSEnableImageComments //Gabriel BUG 6225 - Lista DO
    }

    public class CfgHelper
    {
        public static string sCfgFile = Program.AssemblyLocation + "\\DicomServer.cfg";

        [DllImport("Kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("Kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        [DllImport("Kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);

        public static long Write(string secao, string chave, string valor)
        {
            return WritePrivateProfileString(secao, chave, valor, sCfgFile);
        }

        public static string Read(string secao, string chave)
        {
            var strRetVal = new StringBuilder(255);
            GetPrivateProfileString(secao, chave, "", strRetVal, 255, sCfgFile);

            return strRetVal.ToString();
        }
        public static string Read(string secao, string chave, bool eqp = false)
        {
            string file = sCfgFile;
            if (eqp) file = Program.AssemblyLocation + "\\Equipments.cfg";

            var strRetVal = new StringBuilder(255);
            GetPrivateProfileString(secao, chave, "", strRetVal, 255, file);

            return strRetVal.ToString();
        }
        public static string Read(string secao, string chave, string padrao)
        {
            var strRetVal = new StringBuilder(255);
            GetPrivateProfileString(secao, chave, padrao, strRetVal, 255, sCfgFile);

            if (strRetVal.ToString() == padrao || strRetVal.Length == 0) Write(secao, chave, padrao);

            return strRetVal.ToString();

        }
        public static string ReadAllSections()
        {
            string cfgFile = Program.AssemblyLocation + "\\Equipments.cfg";
            byte[] buffer = new byte[2048];

            GetPrivateProfileString(null, null, "", buffer, 255, cfgFile);
            String[] tmp = Encoding.ASCII.GetString(buffer).Trim('\0').Split('\0');

            string result = string.Empty;

            foreach (String entry in tmp)
            {
                result += (result.Length == 0 ? "" : ";") + entry.Split('=')[0];
            }

            return result;
        }

        public static string GetKeyFor(CfgKeys key)
        {
            switch (key)
            {
                case CfgKeys.SaveDcm:
                    return "Save DCM of Successfuly Stored/Sent Messages";
                case CfgKeys.DcmFolder:
                    return "Saved DCM Messages Root Folder";

                case CfgKeys.DBConn:
                    return "DBConn";

                case CfgKeys.UseWLSCP:
                    return "Enable Worklist SCP Server for this Module";
                case CfgKeys.WLAETitle:
                    return "Worklist SCP AET";
                case CfgKeys.WLPort:
                    return "Worklist SCP Port";
                case CfgKeys.ItemsLoaderTimeSpan:
                    return "Worklist Refresh Rate (sec)";
                case CfgKeys.WLUsesAssociationCallingAE:
                    return "Worklist Uses Association CallingAE as Scheduled StationAE";
                case CfgKeys.WLViewName:
                    return "Worklist Provider View Name";

                case CfgKeys.UseCSSCP:
                    return "Enable C-Store SCP Server for this Module";
                case CfgKeys.CSAETitle:
                    return "C-Store SCP AET";
                case CfgKeys.CSPort:
                    return "C-Store SCP Port";
                case CfgKeys.CSDocFolder:
                    return "C-Store SCP Document Root Folder";
                case CfgKeys.CSImgFolder:
                    return "C-Store SCP Image Root Folder";
                case CfgKeys.CSMirthEndpoint:
                    return "C-Store SCP Mirth Store Request Endpoint";
                case CfgKeys.CSMirthMessageType:
                    return "C-Store SCP Mirth Message Type";
                case CfgKeys.CSMaxImgSizeKB:
                    return "C-Store SCP Max Image Size in KB";
                case CfgKeys.CSStorePdfs:
                    return "C-Store SCP Enable Pdf Storage";
                case CfgKeys.CSStoreImages:
                    return "C-Store SCP Enable Image Storage";
                case CfgKeys.CSStoreMeasuremnts:
                    return "C-Store SCP Enable Measurement Storage";
                //Gabriel BUG 6225 - Lista DO
                //Verifications tags are defined inside cfg file
                //Tags should be exactly like DicomTag definition
                //If verification is activated, DicomServer send the information to StoreRequest channel
                //Inside this channel, it's possible to check if the patient information are correct
                case CfgKeys.CSVerificationTags:
                    return "C-Store SCP Verification Tags";
                case CfgKeys.CSEnableImageComments:
                    return "C-Store SCP Enable Image Comments";
                default:
                    return string.Empty;
            }
        }
    }
}

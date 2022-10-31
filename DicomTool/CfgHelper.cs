using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DicomTool
{
    public class CfgHelper
    {
        public static string sCfgFile = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\DicomTool.cfg";

        [DllImport("Kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("Kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        [DllImport("Kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileSection(string section, byte[] retVal, int size, string filePath);

        public static long Write(string secao, string chave, string valor)
        {
            return WritePrivateProfileString(secao, chave, valor, sCfgFile);
        }

        public static string ReadAllKeysFromSection(string secao)
        {                        
            byte[] buffer = new byte[2048];

            GetPrivateProfileSection(secao, buffer, 255, sCfgFile);
            String[] tmp = Encoding.ASCII.GetString(buffer).Trim('\0').Split('\0');

            string result = string.Empty;

            foreach (String entry in tmp)
            {
                result += (result.Length == 0 ? "" : ";") + entry.Split('=')[0];
            }

            return result;
        }

        public static string ReadAllSections()
        {
            byte[] buffer = new byte[2048];

            GetPrivateProfileString(null, null, "", buffer, 255, sCfgFile);
            String[] tmp = Encoding.ASCII.GetString(buffer).Trim('\0').Split('\0');

            string result = string.Empty;

            foreach (String entry in tmp)
            {
                result += (result.Length == 0 ? "" : ";") + entry.Split('=')[0];
            }

            return result;
        }

        public static string Read(string secao, string chave)
        {
            var strRetVal = new StringBuilder(255);
            GetPrivateProfileString(secao, chave, "", strRetVal, 255, sCfgFile);

            return strRetVal.ToString();
        }

        public static string Read(string secao, string chave, string padrao)
        {
            var strRetVal = new StringBuilder(255);
            GetPrivateProfileString(secao, chave, padrao, strRetVal, 255, sCfgFile);

            if (strRetVal.ToString() == padrao || strRetVal.Length == 0) Write(secao, chave, padrao);

            return strRetVal.ToString();

        }
    }
}

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DicomClient
{
    public class CfgHelper
    {
        public static string sCfgFile = Program.AssemblyLocation + "\\DicomClient.cfg";

        [DllImport("Kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("Kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("Kernel32")]
        private static extern bool GetPrivateProfileStruct(string section, string key, IntPtr lpStruct, int size,  string filePath);

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

        public static string Read(string secao, string chave, string padrao)
        {
            var strRetVal = new StringBuilder(255);
            GetPrivateProfileString(secao, chave, padrao, strRetVal, 255, sCfgFile);

            if (strRetVal.ToString() == padrao || strRetVal.Length == 0) Write(secao, chave, padrao);

            return strRetVal.ToString();

        }

        public static tagDSNCONNECTION ReadtagDSNCONNECTION(string secao, string chave)
        {
            string file = Program.AssemblyLocation + "\\EndoxPro.cfg";
            Type tipo = typeof(tagDSNCONNECTION);
            int size = Marshal.SizeOf(tipo);

            IntPtr ptr = Marshal.AllocCoTaskMem(size);
            bool result = GetPrivateProfileStruct(secao, chave, ptr, size, file);

            tagDSNCONNECTION tag = result ?
                (tagDSNCONNECTION)Marshal.PtrToStructure(ptr, tipo) :
                new tagDSNCONNECTION();
            
            Marshal.FreeCoTaskMem(ptr);
            return tag;
        }

        public struct tagDSNCONNECTION
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public char[] strUser;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public char[] strPassword;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public char[] strServer;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public char[] strParam1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public char[] strParam2;
        }
    }
}

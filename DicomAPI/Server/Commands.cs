using System;
using System.ComponentModel;
using System.Reflection;

namespace DicomAPI
{
    public static class Commands
    {        
        public enum GET
        {
            [Description("api/generic/names")] Names,
            [Description("api/generic/isalive")] Name,
            [Description("api/blablabla/falaai?oque=")] Falaai,
            [Description("api/status/getversion")] Version,
            [Description("api/status/conta")] IsAlive
        }

        public enum POST
        {
            [Description("api/generic/names")] Names,
            [Description("api/generic/isalive")] Name,
            [Description("api/blablabla/falaai?oque=")] Falaai,
            [Description("api/status/getversion")] Version,
            [Description("api/status/conta")] IsAlive
        }

        internal static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = 
                (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }
    }
}


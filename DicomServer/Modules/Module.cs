namespace DicomServer.Modules
{
    public enum IntegratedModules {
        Default = 0, Custom1 = 1, Custom2 = 2,
        Custom3 = 3, Custom4 = 4, Custom5 = 5,
        Custom6 = 6, Custom7 = 7, Custom8 = 8,
        Custom9 = 9
    };
    public enum MirthMessageType:int { JSON = 0, XML = 1 };
    public class Module
    {
        public string ConnectionString { get; set; }


        public bool UseWLSCP { get; set; }
        public string WLAETitle { get; set; } //Worklist
        public int WLPort { get; set; } //Worklist
        public int ItemsLoaderTimeSpan { get; set; } //Worklist
        public bool WLUsesAssociationCallingAE { get; set; } //Worklist
        public string WLViewName { get; set; } //Worklist


        public bool UseCSSCP { get; set; }
        public string CSAETitle { get; set; } //C-Store
        public int CSPort { get; set; } //C-Store
        public string CSDocFolder { get; set; } //C-Store
        public string CSImgFolder { get; set; } //C-Store        
        public string CSMirthEndpoint { get; set; } //C-Store
        public int CSMirthMessageType { get; set; } //C-Store
        public int CSMaxImgSizeKB { get; set; } //C-Store
        public bool CSStorePdfs { get; set; } //C-Store
        public bool CSStoreImages { get; set; } //C-Store
        public bool CSStoreMeasuremnts { get; set; } //C-Store

        public string CSVerificationTags { get; set; }//Gabriel BUG 6225 - Lista DO
        
        public bool CSEnableImageComments { get; set; }//Gabriel BUG 6225 - Lista DO

        public IntegratedModules ModuleType { get; set; }


        public static Module DefaultModule = new Module
        {
            ModuleType = IntegratedModules.Default,
            ConnectionString = "DRIVER={SQL SERVER};SERVER=LOCALHOST;UID=XXX;PWD=XXX;DATABASE=XXX",

            UseWLSCP = false,
            WLAETitle = "TESIWLSCP",
            WLPort = 8005,
            ItemsLoaderTimeSpan = 30,
            WLUsesAssociationCallingAE = false,
            WLViewName = "VISTA_DICOMSERVER_WORKLIST",

            UseCSSCP = false,
            CSAETitle = "TESICSSCP",
            CSPort = 11112,
            CSDocFolder = "C:\\ENDOXSERVER\\DICOMSERVER\\TEMP",
            CSImgFolder = "C:\\ENDOXSERVER\\DICOMSERVER\\TEMP",
            CSMirthEndpoint = "http://localhost:1097/StoreRequest/",
            CSMirthMessageType = 0,
            CSMaxImgSizeKB = 0,
            CSStorePdfs = true,
            CSStoreImages = true,
            CSStoreMeasuremnts = true,
            CSEnableImageComments = true,
            CSVerificationTags = "", //Gabriel BUG 6225 - Lista DO
        };
    }
}

using Dicom.Network;
using DicomServer.CStore.Model;
using DicomServer.Modules;

namespace DicomServer.CStore
{
    class CStoreServer
    {
        private static IDicomServer _server;

        protected CStoreServer()
        {
        }

        public static string AETitle { get; set; }


        public static ICStoreSource CStoreProvider;
        public static Module ServerModule;

        public static void Start(Module module)
        {
            ServerModule = module;

            LoadModuleSource(module.ModuleType);

            AETitle = ServerModule.CSAETitle;
            _server = Dicom.Network.DicomServer.Create<CStoreService>(ServerModule.CSPort);
        }

        public static void Stop(int port)
        {
            if (ServerModule.CSPort == port)
                _server.Dispose();
        }

        private static void LoadModuleSource(IntegratedModules module)
        {
            CStoreProvider = new Modules.Default.CStoreHandler(ServerModule);
        }
    }
}

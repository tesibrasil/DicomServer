using Dicom.Network;
using DicomServer.Modules;
using DicomServer.Worklist.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DicomServer.Worklist
{
    class WorklistServer
    {
        private static IDicomServer _server;
        private static Timer _itemsLoaderTimer;


        protected WorklistServer()
        {
        }

        public static string AETitle { get; set; }
        public static Module ServerModule;

        public static IWorklistItemsSource WorklistItemSource;
        public static IModalityAETSource ModalityAETSource;

        public static List<WorklistItem> CurrentWorklistItems { get; private set; }
        public static List<ModalityAET> CurrentModalityAETs { get; private set; }

        public static void Start(Module module)
        {
            ServerModule = module;
            LoadModuleSource(module.ModuleType);

            AETitle = ServerModule.WLAETitle;
            _server = Dicom.Network.DicomServer.Create<WorklistService>(ServerModule.WLPort);

            _itemsLoaderTimer = new Timer((state) =>
            {
                try
                {
                    var newWorklistItems = WorklistItemSource.GetAllCurrentWorklistItems();
                    CurrentWorklistItems = newWorklistItems;

                    var newModalityAETs = ModalityAETSource.GetAllModalityAETs();
                    CurrentModalityAETs = newModalityAETs;
                }
                catch (Exception e)
                {
                    LogHelper.Fatal($"{AETitle} Service Cannot Get Worklist/Modalities Items due to {Environment.NewLine} {e.Message}", Program.DebugMode);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(ServerModule.ItemsLoaderTimeSpan));
        }

        public static void Stop(int port)
        {
            if (ServerModule.WLPort == port)
                _server.Dispose();
        }

        private static void LoadModuleSource(IntegratedModules module)
        {
            WorklistItemSource = new Modules.Default.WorklistItemsProvider(ServerModule);
            ModalityAETSource = new Modules.Default.ModalityAETProvider(ServerModule);            
        }
    }
}

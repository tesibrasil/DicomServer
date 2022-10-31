using DicomServer.Worklist.Model;
using System.Collections.Generic;
using System.Data.Odbc;

namespace DicomServer.Modules.Default
{
    public class ModalityAETProvider : IModalityAETSource
    {
        private Module _Module;

        public ModalityAETProvider(Module module)
        {
            _Module = module;
        }

        public List<ModalityAET> GetAllModalityAETs()
        {
            List<ModalityAET> ma = new List<ModalityAET>();
            
            using (OdbcConnection conn = new OdbcConnection(_Module.ConnectionString))
            {
                conn.Open();
                using (OdbcCommand cmd = new OdbcCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM DICOMSERVER_AET WHERE ELIMINATO = 0";

                    OdbcDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var aet = new ModalityAET
                        {
                            AET = reader.GetString(reader.GetOrdinal("AET")),
                            Modality = reader.GetString(reader.GetOrdinal("MODALITY"))
                        };
                        ma.Add(aet);
                    };
                    cmd.Dispose();
                };
                conn.Close();
                conn.Dispose();
            };

            return ma;
        }
    }
}

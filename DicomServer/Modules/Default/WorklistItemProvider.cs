using DicomServer.Worklist.Model;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Text;

namespace DicomServer.Modules.Default
{
    public class WorklistItemsProvider : IWorklistItemsSource
    {
        private Module _Module;

        public WorklistItemsProvider(Module module)
        {
            _Module = module;
        }

        public List<WorklistItem> GetAllCurrentWorklistItems()
        {
#if DEBUG
            return GetTest();
#endif
            List<WorklistItem> wl = new List<WorklistItem>();

            using (OdbcConnection conn = new OdbcConnection(_Module.ConnectionString))
            {
                conn.Open();
                using (OdbcCommand cmd = new OdbcCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT * FROM " + _Module.WLViewName;

                    OdbcDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        //patient birth date
                        var py = reader.GetInt32(reader.GetOrdinal("PYear"));
                        var pm = reader.GetInt32(reader.GetOrdinal("PMonth"));
                        var pd = reader.GetInt32(reader.GetOrdinal("PDay"));

                        //exam date
                        var ey = reader.GetInt32(reader.GetOrdinal("EYear"));
                        var em = reader.GetInt32(reader.GetOrdinal("EMonth"));
                        var ed = reader.GetInt32(reader.GetOrdinal("EDay"));
                        var eh = reader.GetInt32(reader.GetOrdinal("EHour"));
                        var ex = reader.GetInt32(reader.GetOrdinal("EMinute"));

                        var item = new WorklistItem
                        {
                            AccessionNumber = reader.GetString(reader.GetOrdinal("AccessionNumber")),
                            DateOfBirth = new DateTime(py, pm, pd, 0, 0, 0),
                            PatientID = reader.GetString(reader.GetOrdinal("PatientID")),
                            Surname = reader.GetString(reader.GetOrdinal("Surname")),
                            Forename = reader.GetString(reader.GetOrdinal("Forename")),
                            Sex = reader.GetString(reader.GetOrdinal("Sex")),
                            Title = null,

                            Modality = reader.GetString(reader.GetOrdinal("Modality")),
                            ExamDescription = Encoding.UTF8.GetString(
                                Encoding.Default.GetBytes(
                                    reader.GetString(reader.GetOrdinal("ExamDescription")
                                ))),
                            ExamRoom = null,
                            HospitalName = null,
                            PerformingPhysician = null,
                            ProcedureID = reader.GetString(reader.GetOrdinal("ProcedureID")),
                            ProcedureStepID = reader.GetString(reader.GetOrdinal("ProcedureID")),
                            StudyUID = reader.GetString(reader.GetOrdinal("StudyUID")),
                            ScheduledAET = reader.GetString(reader.GetOrdinal("ScheduledAET")),
                            ReferringPhysician = null,
                            ExamDateAndTime = new DateTime(ey, em, ed, eh, ex, 0)
                        };
                        wl.Add(item);

                    };
                    cmd.Dispose();
                };
                conn.Close();
                conn.Dispose();
            };

            return wl;
        }

        private List<WorklistItem> GetTest()
        {
            List<WorklistItem> wl = new List<WorklistItem>();

            //patient birth date
            var py = DateTime.Now.Year - 30;
            var pm = DateTime.Now.Month;
            var pd = DateTime.Now.Day;

            //exam date
            var ey = DateTime.Now.Year;
            var em = DateTime.Now.Month;
            var ed = DateTime.Now.Day;
            var eh = DateTime.Now.Hour;
            var ex = DateTime.Now.Minute;

            var item = new WorklistItem
            {
                AccessionNumber = "1234567890_1",
                DateOfBirth = new DateTime(py, pm, pd, 0, 0, 0),
                PatientID = "987654ABC",
                Surname = "TESTE",
                Forename = "TESTE",
                Sex = "M",
                Title = null,

                Modality = "ECG",
                ExamDescription = "Exame Teste AAAA",
                ExamRoom = null,
                HospitalName = null,
                PerformingPhysician = null,
                ProcedureID = "10001",
                ProcedureStepID = "10001",
                StudyUID = "",
                ScheduledAET = "",
                ReferringPhysician = null,
                ExamDateAndTime = new DateTime(ey, em, ed, eh, ex, 0)
            };
            wl.Add(item);

            return wl;
        }
    }
}
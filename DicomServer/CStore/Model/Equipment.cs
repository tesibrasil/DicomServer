using System.Collections.Generic;

namespace DicomServer.CStore.Model
{
    public class Equipment
    {
        public string Name { get; set; }
        public List<string> CodeMeaningForTextVal { get; set; }
        public List<string> CodeMeaningForCodeVal { get; set; }
    }
}

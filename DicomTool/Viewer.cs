using Dicom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DicomTool
{
    public partial class Viewer : Form
    {
        private bool EnableText;
        private bool EnableCode;

        private string TextValue;
        private string CodeValue;
        private string SRMessage;
        private int MeasureCount;

        private Equipment Eqpt = new Equipment();
        private List<Equipment> EqptList = new List<Equipment>();
        string File { get; set; }
        int Type { get; set; }
        public Viewer(string file, int type)
        {
            File = file;
            Type = type;
            
                
            LoadEquipments();
            InitializeComponent();
        }

        private void Viewer_Load(object sender, EventArgs e)
        {            
            if(Type == 0)
            {
                DicomFile file = DicomFile.Open(File);
                ShowAll(file.Dataset, "", 1);
            }                

            if (Type == 1)
            {
                DicomFile file = DicomFile.Open(File);

                EnableText = false;
                EnableCode = false;

                string model;
                file.Dataset.TryGetSingleValue<string>(DicomTag.ManufacturerModelName, out model);
                SetEquipment(model);

                ExtractReport(file.Dataset);
                textBox1.Text = SRMessage;
            }

            if(Type == 2)
            {
                textBox1.AppendText(File);
            }
        }

        public void ShowAll(DicomDataset d, string pad, int lvl)
        {
            var set = new Queue<DicomItem>(d ?? new DicomDataset());

            while (set.Count > 0)
            {
                DicomItem item = null;
                if (set.Count > 0) item = set.Peek();

                if (item != null)
                {
                    if (item is DicomSequence)
                    {
                        DicomSequence sequence = (item as DicomSequence);
                        if (sequence != null)
                        {
                            int count = sequence.Items.Count;
                            for (int i = 0; i < count; i++)
                            {
                                DicomDataset subset = sequence.Items[i];
                                ShowAll(subset, pad + "  ", lvl+1);
                            }
                        }
                    }
                    else
                    {
                        string tagname = item.Tag.DictionaryEntry.Name;
                        if (item is DicomElement)
                        {
                            var element = (item as DicomElement);

                            string value = "<large value not displayed>";
                            /*if (element.Length <= 2048)*/ value = String.Join(",", element.Get<byte[]>());

                            textBox1.AppendText($"{pad}{lvl}.{element.ToString()}: {value}{Environment.NewLine}");
                        }
                    }
                    set.Dequeue();
                    continue;
                }
            }
        }

        public void ExtractReport(DicomDataset d)
        {
            var set = new Queue<DicomItem>(d ?? new DicomDataset());

            while (set.Count > 0)
            {
                DicomItem item = null;
                if (set.Count > 0) item = set.Peek();

                if (item != null)
                {
                    if (item is DicomSequence)
                    {
                        DicomSequence sequence = (item as DicomSequence);
                        if (sequence != null)
                        {
                            int count = sequence.Items.Count;
                            for (int i = 0; i < count; i++)
                            {
                                DicomDataset subset = sequence.Items[i];
                                ExtractReport(subset);
                            }
                        }
                    }
                    else
                    {
                        string tagname = item.Tag.DictionaryEntry.Name;
                        if (item is DicomElement)
                        {
                            var element = (item as DicomElement);

                            string value = "<large value not displayed>";
                            if (element.Length <= 2048) value = String.Join("\\", element.Get<string[]>());

                            if (tagname.Equals("Code Meaning"))
                            {
                                if (Eqpt.CodeMeaningForCodeVal.Contains(value))
                                    EnableCode = true;
                                if (Eqpt.CodeMeaningForTextVal.Contains(value))
                                    EnableText = true;
                            }                            
                            if (tagname.Equals("Text Value") && EnableText)
                            {
                                TextValue = value.ToUpper().Replace(" ", "_");
                                EnableText = false;
                            }
                            if ((tagname.Equals("Text Value") && EnableCode)
                             || (tagname.Equals("Code Value") && EnableCode))
                            {
                                CodeValue = value.ToUpper().Replace(" ", "_");
                                EnableCode = false;
                            }
                            if (tagname.Equals("Numeric Value"))
                            {
                                SRMessage += 
                                    $"<Measure>{Environment.NewLine}" +
                                    $"  <TextValue>{TextValue}</TextValue>{Environment.NewLine}" +
                                    $"  <NumericValue>{value}</NumericValue>{Environment.NewLine}" +
                                    $"  <CodeValue>{CodeValue}</CodeValue>{Environment.NewLine}" +
                                    $"</Measure>{Environment.NewLine}{Environment.NewLine}";
                                                                

                                MeasureCount++;
                            }
                        }
                    }
                    set.Dequeue();
                    continue;
                }
            }
        }

        public void SetEquipment(string name)
        {
            Eqpt = EqptList.FirstOrDefault(x => x.Name == name);
        }

        public void LoadEquipments()
        {
            EqptList = new List<Equipment>();
            var sections = CfgHelper.ReadAllSections();
            
            foreach(string section in sections.Split(';'))
            {
                EqptList.Add(new Equipment
                {
                    Name = section,
                    CodeMeaningForTextVal = new List<string> {
                        CfgHelper.Read(section, "TextVal")
                    },
                    CodeMeaningForCodeVal = new List<string> {
                        CfgHelper.Read(section, "CodeVal")
                    }
                });
            }
        }

        public class Equipment
        {
            public string Name { get; set; }
            public List<string> CodeMeaningForTextVal { get; set; }
            public List<string> CodeMeaningForCodeVal { get; set; }            
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetText(textBox1.Text);
        }
    }
}

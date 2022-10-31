using Dicom;
using Dicom.Network;
using System;
using System.Windows.Forms;

namespace DicomTool
{
    public partial class DicomTool : Form
    {
        public DicomTool()
        {
            InitializeComponent();
        }

        private void DicomTool_Load(object sender, EventArgs e)
        {
            txtServerAETWorklist.Text = "TESIWLSCP";
            txtServerHostnameWorklist.Text = "LOCALHOST";
            txtServerPortWorklist.Text = "8005";

            txtServerAETStore.Text = "TESICSSCP";
            txtServerHostnameStore.Text = "LOCALHOST";
            txtServerPortStore.Text = "11112";


            txtDtStart.Text = DateTime.Now.ToShortDateString();
            txtDtEnd.Text = DateTime.Now.ToShortDateString();

            txtLocalAET.Text = "DICOMTOOL";
            txtLocalAETStore.Text = "DICOMTOOL";
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            string txtlog = string.Empty;
            try
            {
                var request = DicomCFindRequest.CreateWorklistQuery(
                    null, null, null, null, txtModality.Text,
                    new DicomDateRange(Convert.ToDateTime(txtDtStart.Text), Convert.ToDateTime(txtDtStart.Text)));

                request.OnResponseReceived = (DicomCFindRequest rq, DicomCFindResponse rp) =>
                {
                    txtlog = rp.ToString(true);
                    Viewer v = new Viewer(txtlog, 2);
                    v.ShowDialog();
                };

                var client = new DicomClient();
                client.AddRequest(request);
                client.Send(txtServerHostnameWorklist.Text, Convert.ToInt32(txtServerPortWorklist.Text),
                false, txtLocalAET.Text, txtServerAETWorklist.Text);
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string txtlog = string.Empty;
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "DCM Files | *.dcm";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var client = new DicomClient();
                    client.NegotiateAsyncOps();

                    DicomCStoreRequest request = new DicomCStoreRequest(ofd.FileName);
                    if (!request.Dataset.TryGetSingleValue<string>(DicomTag.AccessionNumber, out string accn))
                        request.Dataset.AddOrUpdate(DicomTag.AccessionNumber, "ACCNTEST");

                    if (!request.Dataset.TryGetSingleValue<string>(DicomTag.PatientID, out string ptid))
                        request.Dataset.AddOrUpdate(DicomTag.PatientID, "PTIDTEST");

                    /*request.OnResponseReceived = (DicomCStoreRequest rq, DicomCStoreResponse rp) =>
                    {
                        txtlog = rp.ToString(true);
                        Viewer v = new Viewer(txtlog, 2);
                        v.Show();
                    };*/

                    client.AddRequest(request);

                    client.Send(txtServerHostnameStore.Text, Convert.ToInt32(txtServerPortStore.Text),
                        false, txtLocalAETStore.Text, txtServerAETStore.Text);
                }
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message);
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Viewer v = new Viewer(ofd.FileName, 0);
                v.ShowDialog();
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Viewer v = new Viewer(ofd.FileName, 1);
                v.ShowDialog();
            }
        }
    }
}

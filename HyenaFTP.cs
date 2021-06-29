using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using FluentFTP;

namespace FTP
{
    public partial class HyenaFTP : Form
    {
        public HyenaFTP()
        {
            InitializeComponent();
        }


        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var folderPath = txtPath.Text;
            using (var ftp = new FtpClient(txtServer.Text, txtUserName.Text, txtPassword.Text)) // ftp 서버 아이피, 생성될 폴더명(계정명), ftp 비밀번호
            {
                ftp.Connect();
                Action<FtpProgress> pro = new Action<FtpProgress>(s =>
               {
                   AllbackgroundWorker.ReportProgress((int)s.FileIndex + 1, (int)s.FileCount); // 전체 파일 수 카운트
                   backgroundWorker.ReportProgress((int)s.Progress, s.LocalPath); // 파일 전송에 따른 프로그래스바
               }); // 전체 파일수를 카운트하여 하나의 파일이 전송 완료될때마다 1씩 증가하는 ProgressBar


                ftp.UploadDirectory(folderPath, @"\" + txtUserName.Text, FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, null, pro); // 업로드 및 프로그래스바 업데이트

                // 업로드 후 클라이언트의 시간을 읽어 업로드된 서버의 수정시간을 변경하는 코드
                DirectoryInfo di = new DirectoryInfo(folderPath);
                foreach (FileInfo file in di.GetFiles("*", SearchOption.AllDirectories)) // 재귀함수로 각각의 폴더를 전부 들어가는것 보다 검색옵션으로 리스트만 추출하는게 속도가 더 빠름
                {
                    // 주의 : 서버가 MFMT 지원해야함
                    ftp.SetModifiedTime(txtUserName.Text + Convert.ToString(file).Replace(folderPath, ""), file.LastWriteTime); // 클라이언트 측의 경로를 서버 경로로 재 가공 및 LastWriteTime 변경 진행
                    // ftp.Execute("MFCT " + 생성날짜 + 파일경로) // MFCT도 지원할 경우 생성일자도 변경 가능
                }

            }
        }
        private void AllbackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Invoke(new Action(delegate ()
            {
                FilesinPro.Text = $"{e.ProgressPercentage} of {e.UserState} files in progress";
                AllprogressBar.Maximum = ((int)e.UserState);
                AllprogressBar.Value = e.ProgressPercentage;
                AllprogressBar.Update();
            }));
        }
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            IblStatus.Text = $"{e.UserState} : {e.ProgressPercentage} %";
            progressBar.Value = e.ProgressPercentage;
            progressBar.Update();
        }


        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            IblStatus.Text = "업로드가 완료되었습니다.";
        }


        private void selectPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                string[] files = Directory.GetFiles(fbd.SelectedPath);
                txtPath.Text = fbd.SelectedPath;
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            backgroundWorker.RunWorkerAsync();
        }

        private void txtUserName_TextChanged(object sender, EventArgs e)
        {

        }

        private void AllprogressBar_Click(object sender, EventArgs e)
        {

        }

        private void progressBar_Click(object sender, EventArgs e)
        {

        }

        private void txtPath_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

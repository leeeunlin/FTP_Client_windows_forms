﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

using FluentFTP;
using FluentFTP.Rules;

namespace FTP
{
    public partial class HyenaFTP : Form
    {
        public HyenaFTP()
        {
            InitializeComponent();
        }


        public void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var folderPath = txtPath.Text;
            //var token = new CancellationToken();
            using (var ftp = new FtpClient(txtServer.Text, txtUserName.Text, txtPassword.Text)) // ftp 서버 아이피, 생성될 폴더명(계정명), ftp 비밀번호
            {
                ftp.EncryptionMode = FtpEncryptionMode.Explicit;
                ftp.ValidateAnyCertificate = true;
                ftp.Connect();
                Action<FtpProgress> pro = new Action<FtpProgress>(s =>
               {
                   AllbackgroundWorker.ReportProgress(s.FileIndex + 1, s.FileCount); // 전체 파일 수 카운트 / 아래 backgroundwork가 끝날때마다 1씩 증가됨
                   backgroundWorker.ReportProgress((int)s.Progress, s.LocalPath); // 파일 전송에 따른 프로그래스바
                   if (backgroundWorker.CancellationPending)
                   {
                       ftp.Execute("ABOR"); // FileZilla Server에 ABOR 명령어 전달 (현재 전송중인 파일'만' 전송 중단)
                       ftp.Disconnect(); // 폴더 업로드 시 리스트에 있는 파일 추가 전송을 막기위한 접속종료

                       ftp.Connect(); // 전송 cancel시 파일 삭제하기위한 재접속
                       foreach (var item in ftp.GetListing(@"\" + txtUserName.Text, FtpListOption.AllFiles)) // 삭제하기 위한 foreach 서버내에 읽,쓰,삭 권한이 있어야 작동함
                       {
                           ftp.Execute("DELE " + item.FullName); // item에 들어있는 모든 파일명에 DELE명령어를 추가하여 삭제 진행
                       }
                       ftp.DeleteDirectory(@"\" + txtUserName.Text); // 메인 폴더 삭제
                   }
               });
                ftp.UploadDirectory(folderPath, @"\" + txtUserName.Text, FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, null, pro); // 업로드 및 프로그래스바 업데이트

                // 업로드 후 클라이언트의 시간을 읽어 업로드된 서버의 수정시간을 변경하는 코드
                if (!(backgroundWorker.CancellationPending))
                {
                    DirectoryInfo di = new DirectoryInfo(folderPath);
                    foreach (FileInfo file in di.GetFiles("*", SearchOption.AllDirectories)) // 재귀함수로 각각의 폴더를 전부 들어가는것 보다 검색옵션으로 리스트만 추출하는게 속도가 더 빠름
                    {
                        // 주의 : 서버가 MFMT 지원해야함
                        ftp.SetModifiedTime(txtUserName.Text + Convert.ToString(file).Replace(folderPath, ""), file.LastWriteTime); // 클라이언트 측의 경로를 서버 경로로 재 가공 및 LastWriteTime 변경 진행
                    }
                }

                else
                {

                    return;
                }

            }
        }
        private void AllbackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Invoke(new Action(delegate ()
            {
                if (!(backgroundWorker.CancellationPending))
                {
                    FilesinPro.Text = $"{e.ProgressPercentage} of {e.UserState} files in progress";
                    AllprogressBar.Maximum = ((int)e.UserState);
                    AllprogressBar.Value = e.ProgressPercentage;
                    AllprogressBar.Update();
                }
                else
                {
                    AllprogressBar.Value = 0;
                    IblStatus.Text = "업로드 작업을 취소하고 있습니다.";
                    return;
                }
            }));
        }
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!(backgroundWorker.CancellationPending))
            {
                IblStatus.Text = $"{e.UserState} : {e.ProgressPercentage} %";
                progressBar.Value = e.ProgressPercentage;
                progressBar.Update();
            }
            else
            {
                progressBar.Value = 0;
                return;
            }
        }


        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IblStatus.Text = "작업이 완료되었습니다.";
            btncancel.Visible = true;
            btnUpload.Enabled = true;
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
            backgroundWorker.WorkerSupportsCancellation = true;
            AllbackgroundWorker.WorkerSupportsCancellation = true;

            btnUpload.Enabled = false;
            btncancel.Visible = true;


        }


        private void btncancel_Click(object sender, EventArgs e)
        {
            backgroundWorker.CancelAsync();
            AllbackgroundWorker.CancelAsync();
            btncancel.Visible = false;
            btnUpload.Enabled = true;


        }
    }
}

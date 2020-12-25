using AForge.Controls;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;

namespace FaceCheckIn
{
    public partial class HomeForm : Form
    {
        private FilterInfoCollection videoDevices1;//所有摄像设备
        private VideoCaptureDevice videoDevice1;//摄像设备
        private VideoCapabilities[] videoCapabilities1;//摄像头分辨率

        private string[] allowsExts = { ".jpg", ".png", ".bmp", ".jpeg", ".gif" };
        // 图像文件数据
        private List<string> FaceList = new List<string>();
        public List<UserInfo> Userinfolist { get; set; }

        //设置摄像头获取配置
        public VideoSourcePlayer CurrentVideoSourcePlayer { get; set; }
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        public int selectedDeviceIndex = 0;

        

        public HomeForm()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;

            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            { 
                MessageBox.Show("没有可用摄像头!"); 
            }
            else {
                CurrentVideoSourcePlayer = videoSourcePlayer_UserCheckIn; 
                videoSource = new VideoCaptureDevice(videoDevices[selectedDeviceIndex].MonikerString);//连接摄像头。
                videoSource.VideoResolution = videoSource.VideoCapabilities[selectedDeviceIndex];
                CurrentVideoSourcePlayer.VideoSource = videoSource;
                CurrentVideoSourcePlayer.Start();
            }

            if (Userinfolist == null)
            {
                Userinfolist = new List<UserInfo>();
            }

            users_dataGridView.DataSource = Userinfolist;
            List<List<string>> infor = MysqlUtil.listInfor();
            foreach (List<string> l in infor)
            {
                //作为一行输出
                string line = "";
                for (int i = 0; i < l.Count; i++)
                {
                    //姓名:reader[1],时间:reader[2]
                    if(i == 0){
                        line += "姓名:" + l[i];
                    }else{
                        line += ",签到时间:" + l[i];
                    }
                }
                CheckResult_rtb.Text += line + System.Environment.NewLine;
            }
        }

        //创建一个委托，是为访问DataGridView控件服务的。
        public delegate void UserCheckInDelegate();
        //定义一个委托变量
        public UserCheckInDelegate UserCheck;
        public void UserCheckInMethod()
        {
            Bitmap bitmap = videoSourcePlayer_UserCheckIn.GetCurrentVideoFrame();

            using (var m = new MemoryStream())
            {
                bitmap.Save(m, ImageFormat.Jpeg);
                var flag = UserCheckIn(m.ToArray());
                if (!flag)
                {
                    var img = Image.FromStream(m);
                    string fileName = String.Format("tempPict_{0}.jpg", DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                    img.Save(fileName);
                }
            }

            bitmap.Dispose();
        }
        public void ThreadMethodUserCheckIn()
        {
            this.BeginInvoke(UserCheck);
        }
        private void loadImage_btn_Click(object sender, EventArgs e)
        {
            ofdOpenImageFile.Filter = "Image Files (Image)|*.jpg;*.png;*.bmp;*.jpeg;*.gif;";
            ofdOpenImageFile.Multiselect = true;
            ofdOpenImageFile.AutoUpgradeEnabled = true;

            // 打开图像文件
            if (ofdOpenImageFile.ShowDialog() == DialogResult.Cancel)
                return;
            ofdOpenImageFile.FileNames.ToList().ForEach(item =>
            {
                var msg = AddFace(item);
                if (!String.IsNullOrEmpty(msg))
                {
                    MessageBox.Show(msg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            UpdateImageListUI();
        }
        private string AddFace(string filePath)
        {
            if (FaceList.Contains(filePath))
                return "该人脸已注册";

            //检测图像质量
            var imageBytes = File.ReadAllBytes(filePath);
            var image = Convert.ToBase64String(imageBytes);
            var result = BaiduUtils.faceDetect(image);

            if (String.IsNullOrEmpty(result))
            {
                FaceList.Add(filePath);
                return "";
            }
            return result;
        }
        private void UpdateImageListUI()
        {
            imageLists.Images.Clear();
            FacelistView.Items.Clear();

            int length = FaceList.Count;

            for (int i = 0; i < length; i++)
            {
                var item = FaceList[i];
                var imageName = Path.GetFileNameWithoutExtension(item);
                imageLists.Images.Add(Image.FromFile(item));
                FacelistView.Items.Add(imageName);
                var index = FacelistView.Items.Count - 1;
                FacelistView.Items[index].ImageIndex = index;
                FacelistView.Items[index].Name = imageName;
            }

        }
        private void signIn_btn_Click(object sender, EventArgs e)
        {
            string groupId = groupId_tb.Text;
            string userId = userId_tb.Text;
            string userName = UserName_tb.Text;
            if (String.IsNullOrEmpty(groupId))
            {
                groupId_tb.Focus();
                return;
            }
            if (String.IsNullOrEmpty(userId))
            {
                userId_tb.Focus();
                return;
            }
            if (String.IsNullOrEmpty(userName))
            {
                UserName_tb.Focus();
                return;
            }

            // 如果有可选参数
            var options = new Dictionary<string, object>{
        {"user_info", userName},
        {"quality_control", "NORMAL"},
        {"liveness_control", "NORMAL"}
        };
            var count = imageLists.Images.Count;
            for (int i = 0; i < count; i++)
            {
                var imageBytes = File.ReadAllBytes(FaceList[i]);

                var image = Convert.ToBase64String(imageBytes);
                var jresult = BaiduUtils.addUser(image, groupId, userId, userName);

                if (jresult.Equals("SUCCESS"))
                {
                    Userinfolist.Add(new UserInfo() { group_id = groupId, user_id = userId, user_info = userName });
                    MessageBox.Show("注册成功！");
                }
                else {
                    MessageBox.Show("注册失败："+jresult.ToString());
                }
            }
            
        }
        private bool UserCheckIn(byte[] imageBytes)
        {
            bool flag = false;
            var image = Convert.ToBase64String(imageBytes);
            FaceSearchResult result = BaiduUtils.searchOneUserByImage(image);
            // 可选参数
            var option = new Dictionary<string, object>()
                {
                    {"spd", 5}, // 语速
                    {"vol", 7}, // 音量
                    {"per", 4}  // 发音人，4：情感度丫丫童声
                };

            if (result != null && result.flag)
            {
                flag = true;
                var time = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                CheckResult_rtb.AppendText(String.Format("{0}\t 签到时间：{1}\n", result.msg, time));
                //签到信息入库
                MysqlUtil.addInfor(result.msg, time);
                Speak.speech(String.Format("签到成功，欢迎{0}", result.msg), option);
            }
            else
            {
                Speak.speech(String.Format("没有该用户的信息，请先注册"), option);
            }

            return flag;
        }

        private void Cancel_btn_Click(object sender, EventArgs e)
        {
            FaceList.Clear();
            imageLists.Images.Clear();
            FacelistView.Items.Clear();
        }

        private void RemoveBtn_Click(object sender, EventArgs e)
        {
            while (FacelistView.SelectedIndices.Count > 0)
            {
                var index = FacelistView.SelectedIndices[0];
                FaceList.RemoveAt(index);
                imageLists.Images.RemoveAt(index);
                FacelistView.Items.RemoveAt(index);
            }

            UpdateImageListUI();
        }

        private void folderBrowser_btn_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                if (!String.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                    ImageFolderAddress_tb.Text = folderBrowserDialog.SelectedPath.Trim();
            }
        }

        private void uploadFiles_btn_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(ImageFolderAddress_tb.Text))
                return;

            imageLists.Images.Clear();
            FacelistView.Items.Clear();
            //刷新Listview
            BindListView();
        }

        private void BindListView()
        {
            var path = @ImageFolderAddress_tb.Text.Trim();
            DirectoryInfo dir = new DirectoryInfo(path);
            foreach (FileInfo d in dir.GetFiles())
            {
                var filePath = Path.Combine(path, d.Name);

                if (allowsExts.Contains(d.Extension))
                {
                    var msg = AddFace(filePath);
                    if (!String.IsNullOrEmpty(msg)) {
                        MessageBox.Show(msg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            UpdateImageListUI();
        }

        //打开摄像头
        private void activeCamara_btn_Click(object sender, EventArgs e)
        {
            if (videoDevices.Count == 0)
                return;
            if (CurrentVideoSourcePlayer != null && CurrentVideoSourcePlayer.IsRunning)
            {
                CurrentVideoSourcePlayer.SignalToStop();
                CurrentVideoSourcePlayer.WaitForStop();
                videoSource = null;
            }
            switch (UserFace_Page.SelectedIndex)
            {
                case 2:
                    break;
                case 0:
                    CurrentVideoSourcePlayer = videoSourcePlayer_UserCheckIn;
                    break;
            }
            
            videoSource = new VideoCaptureDevice(videoDevices[selectedDeviceIndex].MonikerString);//连接摄像头。
            videoSource.VideoResolution = videoSource.VideoCapabilities[selectedDeviceIndex];
            CurrentVideoSourcePlayer.VideoSource = videoSource;
            CurrentVideoSourcePlayer.Start();
        }

        //用户确认签到
        private void ConfirmCheckIn_btn_Click(object sender, EventArgs e)
        {
            if (videoSource == null)
                return;

            if (CurrentVideoSourcePlayer.Name == "videoSourcePlayer_UserCheckIn" && CurrentVideoSourcePlayer.IsRunning)
            {
                Bitmap bitmap = CurrentVideoSourcePlayer.GetCurrentVideoFrame();

                using (var m = new MemoryStream())
                {
                    bitmap.Save(m, ImageFormat.Jpeg);
                    var flag = UserCheckIn(m.ToArray());
                    if (!flag)
                    {
                        var img = Image.FromStream(m);
                        string fileName = String.Format("tempPict_{0}.jpg", DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                        img.Save(fileName);
                    }
                }
                bitmap.Dispose();
            }
        }
        private void UserFace_Page_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (UserFace_Page.SelectedIndex)
            {
                case 2:
                    users_dataGridView.DataSource = Userinfolist;
                    break;
                case 0:
                    activeCamara_btn_Click(sender, e);
                    break;
            }

        }

        private void CatchPicture_btn_Click(object sender, EventArgs e)
        {
            if (videoSource == null)
                return;
            if (CurrentVideoSourcePlayer == null || !CurrentVideoSourcePlayer.IsRunning)
                return;

            Bitmap bitmap = CurrentVideoSourcePlayer.GetCurrentVideoFrame();

            string fileName = String.Format("tempPict_{0}.jpg", DateTime.Now.ToString("yyyyMMddHHmmssfff"));

            using (var m = new MemoryStream())
            {
                bitmap.Save(m, ImageFormat.Jpeg);

                var img = Image.FromStream(m);

                //TEST
                img.Save(fileName);
                var msg = AddFace(fileName);

                if (!String.IsNullOrEmpty(msg))
                {
                    //SpeechHelper.Tts(msg, null);
                    MessageBox.Show(msg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                    UpdateImageListUI();
            }

            bitmap.Dispose();

        }

        private void delete_btn_Click(object sender, EventArgs e)
        {
            var flag = false;
            List<UserInfo> list = new List<UserInfo>();
            list.AddRange(Userinfolist.Select(x => new UserInfo(x)));
            foreach (DataGridViewRow row in users_dataGridView.SelectedRows)
            {
                var deleteUser = row.DataBoundItem as UserInfo;

                var jresult = BaiduUtils.delUser(deleteUser.group_id, deleteUser.user_id);
                if (jresult.Equals("SUCCESS"))
                {
                    flag = true;
                    list.Remove(deleteUser);
                }
                else {
                    MessageBox.Show("删除失败：" + jresult.ToString());
                }
            }

            if (flag)
            {
                users_dataGridView.DataSource = list;
            }
        }

        private void HomeForm_Load(object sender, EventArgs e)
        {
            videoDevices1 = new FilterInfoCollection(FilterCategory.VideoInputDevice);//得到机器所有接入的摄像设备
            if (videoDevices1.Count != 0)
            {
                foreach (FilterInfo device in videoDevices1)
                {
                    cboVideo.Items.Add(device.Name);//把摄像设备添加到摄像列表中
                }
            }
            else
            {
                cboVideo.Items.Add("没有找到摄像头");
            }
            cboVideo.SelectedIndex = 0;//默认选择第一个

            var groupList = BaiduUtils.queryGroupList();
            var groupIdList = JsonConvert.DeserializeObject<List<string>>(groupList.ToString());

            List<UserInfo> userinfolist1 = new List<UserInfo>();
            foreach (var groupId in groupIdList)
            {
                List<UserInfo> list1 = BaiduUtils.queryUserListByGroupId(groupId);
                userinfolist1.Concat(list1);

            }
            
   
            Thread objThread = new Thread(new ThreadStart(delegate
            {
                while (true)
                {
                    var result = userinfolist1;
                    if (result.Count > 0)
                        Userinfolist = result;
                }
            }));

            objThread.Start();

        }

        private void DisConnect()
        {
            if (CurrentVideoSourcePlayer!= null)
            {
                CurrentVideoSourcePlayer.SignalToStop();
                CurrentVideoSourcePlayer.WaitForStop();
                videoDevice1.VideoResolution = videoCapabilities1[cboResolution.SelectedIndex];//摄像头分辨率
                videoSourcePlayer_UserCheckIn.VideoSource = videoDevice1;//把摄像头赋给控件
                videoSourcePlayer_UserCheckIn.Start();//开启摄像头
                vispShoot.VideoSource = null;
                EnableControlStatus(false);
            }
        }
        private void HomeForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisConnect();
        }

        private void groupId_tb_TextChanged(object sender, EventArgs e)
        {

        }

        private void videoSourcePlayer_UserCheckIn_Click(object sender, EventArgs e)
        {

        }

        private void cboVideo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (videoDevices.Count != 0)
            {
                //获取摄像头
                videoDevice1 = new VideoCaptureDevice(videoDevices[cboVideo.SelectedIndex].MonikerString);
                GetDeviceResolution(videoDevice1);//获得摄像头的分辨率
            }
        }

        private void GetDeviceResolution(VideoCaptureDevice videoCaptureDevice)
        {
            cboResolution.Items.Clear();//清空列表
            videoCapabilities1 = videoCaptureDevice.VideoCapabilities;//设备的摄像头分辨率数组
            foreach (VideoCapabilities capabilty in videoCapabilities1)
            {
                //把这个设备的所有分辨率添加到列表
                cboResolution.Items.Add("{capabilty.FrameSize.Width} x {capabilty.FrameSize.Height}");
            }
            cboResolution.SelectedIndex = 0;//默认选择第一个
        }

        private void cboResolution_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (videoDevice1 != null)//如果摄像头不为空
            {
                Console.Write("摄像头检测成功");
                if ((videoCapabilities1 != null) && (videoCapabilities1.Length != 0))
                {
                    CurrentVideoSourcePlayer.SignalToStop();
                    CurrentVideoSourcePlayer.WaitForStop();
                    videoDevice1.VideoResolution = videoCapabilities1[cboResolution.SelectedIndex];//摄像头分辨率
                    vispShoot.VideoSource = videoDevice1;//把摄像头赋给控件
                    vispShoot.Start();//开启摄像头
                    EnableControlStatus(false);
                }
            }
        }

        private void btnCut_Click(object sender, EventArgs e)
        {
        
            DisConnect();//断开连接
            EnableControlStatus(true);
        }

        //控件的显示切换
        private void EnableControlStatus(bool status)
        {
            cboVideo.Enabled = status;
            cboResolution.Enabled = status;
            btnConnect.Enabled = status;
            btnPic.Enabled = !status;
            btnCut.Enabled = !status;
        }

        private void btnPic_Click(object sender, EventArgs e)
        {
            //for (int i = 1; ;i++)
            {
                Bitmap img = vispShoot.GetCurrentVideoFrame();//拍照
                picbPreview.Image = img;
                //这里可以根据情况，把照片存到某个路径下
                //btnPic.
                var tmp_path = "20201224.png";
                img.Save(tmp_path);
                var msg = AddFace(tmp_path);
                if (!String.IsNullOrEmpty(msg))
                {
                    MessageBox.Show(msg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                UpdateImageListUI();
            }
        }

        

    }
}

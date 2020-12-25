using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Baidu.Aip;
using Baidu.Aip.Face;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using System.Net;
using System.Text.RegularExpressions;

namespace FaceCheckIn
{
    class BaiduUtils
    {
        private const string APP_ID = "22908578";
        private const string API_KEY = "QFGKFa5pLbrcDyXD1zcXhjRp";
        private const string SECRET_KEY = "uZ2uvCEKv5wceHyrjYO9wXVAVsqiVc4Z";

        private static Face aipClient = new Face(API_KEY, SECRET_KEY) { Timeout = 6000 };

        //创建用户组-成功返回SUCCESS，失败返回失败信息
        public static String addGroupOne(String groupId)
        {

            try
            {
                JObject result = aipClient.GroupAdd(groupId);
                if (result["error_msg"].ToString() == String.Empty)
                {
                    return "SUCCESS";
                }
                else
                {
                    return result["error_msg"].ToString();
                }

            }
            catch (AipException exp)
            {
                return exp.Message;
            }
        }

        //删除用户组-成功返回SUCCESS，失败返回失败信息
        public static String delGroupOne(String groupId)
        {

            try
            {
                JObject result = aipClient.GroupDelete(groupId);
                if (result["error_msg"].ToString() == String.Empty)
                {
                    return "SUCCESS";
                }
                else
                {
                    return result["error_msg"].ToString();
                }
            }
            catch (AipException exp)
            {
                return exp.Message;
            }


        }

        //查询用户组
        public static JToken queryGroupList()
        {
            JObject groupObj = aipClient.GroupGetlist();
            var groupIdList = groupObj["result"]["group_id_list"];

            Console.WriteLine("用户组列表：");
            Console.WriteLine(groupIdList);

            return groupIdList;
        }

        //1、创建用户-注册人脸-成功返回SUCCESS，失败返回失败信息
        public static String addUser(string image, string groupId, string userId, String userName)
        {
            var options = new Dictionary<string, object>{
                    {"user_info", userName},
                    {"quality_control", "NORMAL"},
                    {"liveness_control", "NORMAL"}
                };
            JObject userAddObj = aipClient.UserAdd(image, "BASE64", groupId, userId, options);

            try
            {
                if (userAddObj["error_msg"].ToString() == String.Empty)
                {
                    Console.WriteLine("新增成功:");
                    Console.WriteLine(userAddObj.ToString());
                    return "SUCCESS";
                }
                else
                {
                    return userAddObj["error_msg"].ToString();
                }
            }
            catch (AipException exp)
            {
                return exp.Message;
            }

        }

        //更新用户-修改人脸-成功返回SUCCESS，失败返回失败信息
        public static String updUser(string image, string groupId, string userId, String userName)
        {

            var options = new Dictionary<string, object>{
                {"user_info", userName},
                {"quality_control", "NORMAL"},
                {"liveness_control", "NORMAL"}
            };

            JObject userUpdObj = aipClient.UserUpdate(image, "BASE64", groupId, userId, options);

            try
            {
                if (userUpdObj["error_msg"].ToString() == String.Empty)
                {
                    Console.WriteLine("修改成功:");
                    Console.WriteLine(userUpdObj.ToString());
                    return "SUCCESS";
                }
                else
                {
                    return userUpdObj["error_msg"].ToString();
                }
            }
            catch (AipException exp)
            {
                return exp.Message;
            }
        }


        //3、删除用户-成功返回SUCCESS，失败返回失败信息
        public static String delUser(string groupId, string userId)
        {

            try
            {
                JObject result = aipClient.UserDelete(groupId, userId); ;
                if (result["error_msg"].ToString() == String.Empty)
                {
                    return "SUCCESS";
                }
                else
                {
                    return result["error_msg"].ToString();
                }

            }
            catch (AipException exp)
            {
                return exp.Message;
            }


        }

        //2、获取用户列表-返回一个组的全部人员信息List<UserInfo>
        public static List<UserInfo> queryUserListByGroupId(String groupId)
        {

            JObject userListResult = aipClient.GroupGetusers(groupId);
            Console.WriteLine("用户信息列表:");
            Console.WriteLine(userListResult);

            List<UserInfo> userinfolist = new List<UserInfo>();

            if (userListResult["error_msg"].ToString() == "SUCCESS")
            {
                var userIdListObj = userListResult["result"]["user_id_list"];
                var userIdList = JsonConvert.DeserializeObject<List<string>>(userIdListObj.ToString());

                foreach (var userId in userIdList)
                {
                    UserInfo userInfo = queryOneUser(userId, groupId);
                    userinfolist.Add(userInfo);
                }

                return userinfolist;
            }
            else
            {
                return userinfolist;
            }
        }



        //用户信息查询-返回UserInfo类，查询失败返回null
        public static UserInfo queryOneUser(String userId, String groupId)
        {
            JObject userResult = aipClient.UserGet(userId, groupId);
            Console.WriteLine("用户信息:");
            Console.WriteLine(userResult);

            if (userResult["error_msg"].ToString() == "SUCCESS")
            {
                UserInfo userInfo = new UserInfo();
                userInfo.user_id = userId;
                userInfo.user_info = userResult["result"]["user_list"][0]["user_info"].ToString();
                userInfo.group_id = groupId;

                return userInfo;

            }
            else
            {
                return null;
            }


        }

        //4、人脸搜索---返回人员的id+人员的姓名
        public static String searchOneUserByImage(String image)
        {

            var options = new Dictionary<string, object>{
                    {"quality_control", "NORMAL"},
                    {"liveness_control", "LOW"},
                };
            var groupList = queryGroupList();
            String groupIdList = String.Join(",", groupList);


            var searchResult = aipClient.Search(image, "BASE64", groupIdList, options);
            if (searchResult["error_msg"].ToString() == "SUCCESS")
            {
                Console.WriteLine(searchResult);

                var serResultObj = searchResult["result"]["user_list"];
                String userId = serResultObj[0]["user_id"].ToString();
                String userName = serResultObj[0]["user_info"].ToString();

                Console.WriteLine("人脸搜索结果:");
                Console.WriteLine(searchResult);

                return userId.ToString() +"#"+ userName;

            }
            else
            {
                return searchResult["error_msg"].ToString();
            }

        }

        /// <summary>
        /// 人脸检测 
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static String DetectDemo(string image)
        {
            String Result = String.Empty;
            try
            {
                var imageType = "BASE64";
                //var result = _faceClient.Detect(image, imageType);
                // 如果有可选参数
                var options = new Dictionary<string, object>
                {
                    {"face_field",  "beauty,age,expression,face_shape,gender,race,quality"},
                    {"max_face_num", 1},
                };

                var jresult = aipClient.Detect(image, imageType, options);

                if (jresult["error_code"].ToString() != "0" && !String.IsNullOrEmpty(jresult["error_msg"].ToString()))
                    return jresult["error_msg"].ToString();

                FaceDectecResult faceResult = JsonConvert.DeserializeObject<FaceDectecResult>(jresult["result"].ToString());
                var quality = faceResult.face_list[0].quality;

                if (quality != null)
                {
                    var s = CommonUtility.GetProperties<Occlusion>(quality.occlusion);
                    string pattern = @"[^\d.\d]"; // 正则表达式剔除非数字字符（不包含小数点.）
                    var values = Regex.Split(s, pattern).Where(m => !String.IsNullOrEmpty(m)).Select(m => double.Parse(m)).ToList();
                    if (values.Count(x => x > 0.7) > 0)
                    {
                        return "图像中遮挡范围太大，请重新上传！";
                    }

                    if (0.7 < quality.blur)
                    {
                        return "图像太模糊，请重新上传！";
                    }
                    if (40 > quality.illumination)
                    {
                        return "图像中光线太暗，请重新上传！";
                    }
                    if (0.7 > quality.completeness)
                    {
                        return "人脸不完整，请重新上传！";
                    }
                }
            }
            catch (AipException exp)
            {
                MessageBox.Show(exp.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (WebException exp)
            {
                MessageBox.Show(exp.Message, "网络错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return Result;
        }

        //人脸识别-返回是人脸的概率
        public static String faceDetect(String image)
        {
            var imageType = "BASE64";
            //var result = _faceClient.Detect(image, imageType);
            // 如果有可选参数
            var options = new Dictionary<string, object>
                {
                    {"face_field",  "beauty,age,expression,face_shape,gender,race,quality"},
                    {"max_face_num", 1},
                };

            var result = aipClient.Detect(image, imageType, options);

            if (result["error_code"].ToString() != "0" && !String.IsNullOrEmpty(result["error_msg"].ToString()))
                return result["error_msg"].ToString();

            FaceDectecResult faceResult = JsonConvert.DeserializeObject<FaceDectecResult>(result["result"].ToString());
            Console.WriteLine("人脸识别结果:");
            Console.WriteLine(faceResult);

            return faceResult.face_list[0].face_shape.probability;

        }
    }

    public class UserInfo
    {
        public UserInfo() { }
        public UserInfo(UserInfo face)
        {
            group_id = face.group_id;
            user_id = face.user_id;
            user_info = face.user_info;
        }
        public string group_id { get; set; }
        public string user_id { get; set; }
        public string user_info { get; set; }
    }

    public class FaceInfo
    {
        public FaceShape face_shape { get; set; }
        public String age { get; set; }
        public Facetype face_type { get; set; }
        public Gender gender { get; set; }
        public Expression expression { get; set; }
        public Race race { get; set; }
        public String beauty { get; set; }
        public ImageQuality quality { get; set; }

    }

    public class FaceShape
    {
        public String type { get; set; }
        public String probability { get; set; }
    }

    public class Facetype
    {
        public String type { get; set; }
        public String probability { get; set; }
    }

    public class Gender
    {
        public String type { get; set; }
        public String probability { get; set; }
    }

    public class Expression
    {
        public String type { get; set; }
        public String probability { get; set; }
    }

    public class Race
    {
        public String type { get; set; }
        public String probability { get; set; }
    }

    public class FaceDectecResult
    {
        public int face_num { get; set; }
        public List<FaceInfo> face_list { get; set; }
    }

    public class ImageQuality
    {
        /// <summary>
        /// 遮挡范围
        /// </summary>
        public Occlusion occlusion { get; set; }
        /// <summary>
        /// 人脸模糊程度，范围[0~1]，0表示清晰，1表示模糊
        /// </summary>
        public double blur { get; set; }
        /// <summary>
        /// 取值范围在[0~255], 表示脸部区域的光照程度 越大表示光照越好
        /// </summary>
        public double illumination { get; set; }
        /// <summary>
        /// 人脸完整度，0或1, 0为人脸溢出图像边界，1为人脸都在图像边界内
        /// </summary>
        public Int64 completeness { get; set; }
    }

    public class Occlusion
    {
        /// <summary>
        /// 左眼遮挡比例
        /// </summary>
        public double left_eye { get; set; }
        /// <summary>
        /// 右眼遮挡比例
        /// </summary>
        public double right_eye { get; set; }
        /// <summary>
        /// 鼻子遮挡比例
        /// </summary>
        public double nose { get; set; }
        /// <summary>
        /// 嘴巴遮挡比例
        /// </summary>
        public double mouth { get; set; }
        /// <summary>
        /// 左脸颊遮挡比例
        /// </summary>
        public double left_cheek { get; set; }
        /// <summary>
        /// 右脸颊遮挡比例
        /// </summary>
        public double right_cheek { get; set; }
        /// <summary>
        /// 下巴遮挡比例
        /// </summary>
        public double chin { get; set; }

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.ComponentModel;                            //包含事件接口，如：INotifyPropertyChanged。
using System.Security.Permissions;                      //包含为制作类似DoEvent函数的DispatcherHelper类库。
using System.Windows.Threading;                         //包含时钟中断，DispatcherTimer。
using System.Runtime.Serialization;                     //包含序列化函数
using System.Runtime.Serialization.Formatters.Binary;   //包含序列化格式化函数.
using System.Threading;
using System.Windows.Media.Converters;

namespace WpfTestFy4Class
{
    /// <summary>
    /// WpfTestFy4Class程序的摘要说明。
    /// 这并不是一个专门的FY-4卫星资料应用程序。它只是为了测试我所制作的HDF5格式FY-4卫星数据类而设。除去设计器自动生成的代码，
    /// 这个程序大约有2000多行，但他可以读取FY-4卫星广播下发的，包括4、2、1公里、5百米分辨率1到14个光谱通道的HDF5文件数据等。
    /// 读取数据的功能仅仅是这个程序的一小部分，更重要的是程序展示了操纵这些数据的方法。
    /// 这个程序所展示的FY-4卫星数据类，包含了可供你选择的多种操作，其类方法主要包括：
    /// 1、获取任意指定经纬区域的各光谱通道的数据和图像。
    /// 2、获取任意指定经纬点某光谱通道的灰度/亮温/反照率的数值。
    /// 3、获取任意指定经纬点所有光谱通道的灰度/亮温/反照率的数值。
    /// 4、获取数据文件包含的所有数据集及其属性，提取某光谱通道全区域扫描灰度图像。
    /// 因此，这个测试程序实现了：调入指定路径下HDF5格式的FY-4卫星数据并提取任意经纬区域数据及其图像的功能；
    /// 读取显示了相应FY-4卫星广播数据文件的各种数据集参数和属性。
    /// 右下角的经纬区域文本框将选择经纬矩形区域，截取这个区域的数据并显示其图像。
    /// 这个程序还实现了基本的图像、数据操作功能：
    /// 1、放大、缩小、平移选定通道的图像，实现仔细查看功能。
    /// 2、显示图像上任意一点所对应的卫星成像仪观测的灰度、亮温/反射率的数值。
    /// 3、给显示图像加上、取消经纬网格。
    /// 4、保存你所截取区域任意通道或所有通道的数据。为了展示如何操作保存后的数据，另有一个小程序展示了操作方法。
    /// 这个程序包括了几个重要的文件：
    /// 1、Fy4HDF5Dat.dll：这是主文件，他是.NET的托管动态链接库，因此在你的工程中直接添加引用就行了。
    /// 2、HDF5DotNet.dll;hdf5_hldll.dll;hdf5dll.dll;szip.dll;zlib.dll：这5个动态链接库,是美国HDF网站上公开公布的HDF5格式函数库。
    /// 3、WpfTestFy4Class.xaml、WpfTestFy4Class.xaml.cs、PopupImgView.xaml、PopupImgView.xaml.cs这四个文件是源代码文件，展示了如何
    /// 利用Fy4HDF5Dat类库操纵卫星数据的方法。这里的演示程序尽管不是专门为应用而设计,但这2000行程序已经支持如此多的功能,更重要的是你
    /// 可以感觉到使用操纵数据的方法,就如同使用Windows内部函数一样轻松！通过这个程序你可以学会如何使用FY-4的HDF5数据类动态链接库，
    /// 可以利用具有这些功能的工具,借助于他的力量使您对FY4号卫星数据应用轻松进入定量应用阶段。
    /// 设想一下可能的应用方向：1、自动、每个时刻提取某个经纬点14个光谱通道的亮温值，与相对应的气象站测值形成相关对照，进而得出结果甚至
    /// 由此指导类似无站地域的各种要素。2、自动、每时刻提取某个点或区域的14个光谱通道的亮温或反照率，形成阴晴相关对照，得出结果甚至指导
    /// 无人区或国外服务点的阴晴预报。3、持续不断的提取相应小区域14个通道高分辨率的亮温/反照率，积累成年月值，研究你关心小区域的卫星气候性质。
    /// 4、。。。。。。。。等等。FY4号卫星数据是18年5月开始广播下发的，数据分辨率、光谱通道、定量应用能力已经进入国际前列，这个应用工具
    /// 可使你迅速专注于这个最新的先进卫星资料定量应用领域，开创你对气象科技理解的新境界。
    /// 另外，由于Fy4号卫星分辨率高，文件超大，程序运行较费时，同时，这个测试程序同时操纵了多达14个通道的数据，同时显示15张图，所以，程序必须
    /// 运行在64位系统上，内存最好8G以上，屏幕最好支持1920X1080或以上。
    /// </summary>
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>记忆启动程序的目录,要求连带的动态链接库,数据库必须与应用程序同目录.</summary>
        string curDir;
        /// <summary>数据文件路径名</summary>
        string theFile;
        /// <summary>当前原始数据文件名</summary>
        string curSourceFile;
        /// <summary>用于选择读入数据文件的路径</summary>
        public static Microsoft.Win32.OpenFileDialog opfd = new Microsoft.Win32.OpenFileDialog();
        /// <summary>用于选择存储数据文件的路径，没办法只能用这个Forms了，wpf没有对应功能</summary>
        public static System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
        /// <summary>用于侦测新建文件，以便自动处理程序启动处理新生成的数据</summary>
        public static FileSystemWatcher fsw = new FileSystemWatcher();
        /// <summary>用于侦测数据文件夹的文件数量，以便每天清理旧文件以维持稳定的存储容量。</summary>
        public static DispatcherTimer mytimer = new DispatcherTimer();

        /// <summary>定义一个FY-4主数据类变量，所谓主数据，相对于定时下发的FY-4辅助数据(如太阳高度角、卫星观测角等)而言。</summary>
        public Fy4HDF5Dat.Fy4HDF5Dat fy4MajorDat;

        /// <summary>定义了当前15个格点数据图像</summary>
        System.Drawing.Bitmap[] theBitmaps;
        /// <summary>设定相应15个内存流，用于指向图像控件的源</summary>
        MemoryStream[] theBitmapStreams;
        /// <summary>相应的15个显示用图像源</summary>
        ImageSource[] thebmpSources;
        /// <summary>相应的15个显示用图像源转换格式</summary>
        ImageSourceConverter[] theimgSourCons;
        /// <summary>程序当前状态字符串</summary>
        string strCurStatus;
        /// <summary>这是区域截取的经纬边界和经纬步长，升级为全局变量便于使用</summary>
        int iLn = 0, iLs = 0, iLw = 0, iLe = 0, iStep = 0;
        /// <summary>设定一个绑定标签的字符串，用于显示状态</summary>
        ViewTxt lbStr;
        /// <summary>设定绑定</summary>
        System.Windows.Data.Binding lbTxtBinding;
        /// <summary>设定一个绑定显示文本框的字符串，用于中断处理时对显示框的显示状态操作。中断状态下无法获得文本框的控制权。</summary>
        ViewTxt dispStr;
        /// <summary>设定一个绑定显示文本框的绑定</summary>
        System.Windows.Data.Binding dispTxtBinding;
        /// <summary>设个全局变量记录当前文件中的光谱通道数</summary>
        int NOMChannels;
        /// <summary>当前观测的结束日期时间</summary>
        DateTime dtObsEnd;
        /// <summary>当前卫星名、仪器名、ID、观测结束日期时间等等</summary>
        string satNameetc;
        /// <summary>当前观测数据的成像仪步进视角</summary>
        int steppingAngle;

        /// <summary>程序处理生成的数据文件要存储时，存储的具体路径位置</summary>
        public string svDataPath;
        /// <summary>程序图像框中生成的图像需要以图片文件存储时，存储的具体路径位置</summary>
        public string svBitmapPath;
        /// <summary>程序处理生成的数据文件要存储时，在本地存储位置存储的天数，设定这个是保证实时处理不因磁盘满溢而出现意外。</summary>
        public int iFileRetained;
        /// <summary>在程序进行批处理或实时侦测处理FY4A文件时，用户发出终止请求，这是标志，见到这个标志，程序在处理完当前文件后将停止</summary>
        public bool usrBreakBatchProc = false;
        /// <summary>设置一个文件名识别的正则表达式，主要考虑局限处理任务在固定的经纬分辨率上</summary>
        public Regex regFileName;
        /// <summary>设置批处理中是否兼容多种分辨率的标识，true则是生成与文件数据等同分辨率的经纬数据，false则只生成给定分辨率数据</summary>
        public bool multiresolutionID;
        /// <summary>标识当前是否处于自动侦测处理状态。</summary>
        bool bAutoProcessState;

        /// <summary>对界面进行一些初始化</summary>
        public MainWindow()
        {
            InitializeComponent();
            curDir = Environment.CurrentDirectory;      //记录程序运行环境的路径。

            txtNLat.Text = "50000";
            txtWLon.Text = "90000";
            txtSLat.Text = "30000";
            txtELon.Text = "120000";
            txtLLStep.Text = "50";      //以上设置选取显示区域的默认值，以新疆为中心的区域

            getIni();

            btnAffirm.IsEnabled = false;    //确认改变显示选择区的按钮，上述文本框一旦有变则触发有效。
            btnSaveArea.IsEnabled = false;  //在数据调入之前存数据功能暂时屏蔽。

            lbStr = new ViewTxt();
            lbStr.txtLongStr = "文 件 参 数";
            lbTxtBinding = new System.Windows.Data.Binding();
            lbTxtBinding.Source = lbStr;                     //定义好这个窗口图像控件的ToolTip字符串，以便后用。
            lbTxtBinding.Path = new PropertyPath("txtLongStr");
            BindingOperations.SetBinding(this.txtTitle, System.Windows.Controls.Label.ContentProperty, lbTxtBinding);

            dispStr = new ViewTxt();
            dispTxtBinding = new Binding();
            dispTxtBinding.Source = dispStr;
            dispTxtBinding.Path = new PropertyPath("txtLongStr");
            BindingOperations.SetBinding(this.txtDisp, System.Windows.Controls.TextBox.TextProperty, dispTxtBinding);

            //fsw.Created += new FileSystemEventHandler(fsw_Created);
            fsw.Renamed += new RenamedEventHandler(fsw_Created);
            fsw.EnableRaisingEvents = false;
            bAutoProcessState = false;

            mytimer.Tick += new EventHandler(CheckFilesRetain);
            mytimer.Interval = TimeSpan.FromHours(2);       //设定2小时检查一次本地存储文件，删除过期文件以保证新文件的存储空间。
            mytimer.Stop();
        }

        /// <summary>通过指向FY4星扫描辐射仪观测数据文件，展示了全文件数据和选取区域数据的取出和显示</summary>
        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            opfd.InitialDirectory = curDir;
            opfd.DefaultExt = "HDF";
            opfd.Filter = "HDF Files(*.HDF;*.hdf)|*.HDF;*.hdf|All Files(*.*)|*.*";
            opfd.Title = "这个程序装载过程需要一个正规的Fy-4AHDF文件，它可以显示文件参数及其各通道灰度图像";
            //opfd.Multiselect = true;
            if (opfd.ShowDialog() == true)
            {                               //以上调用文件选择对话框，由用户选取打算处理的FY-4卫星数据文件。
                lbStr.txtLongStr = "请稍候！！！.....";
                Cursor tmpMouse = Mouse.OverrideCursor;
                Mouse.OverrideCursor = Cursors.Wait;
                btnSaveArea.IsEnabled = false;
                DispatcherHelper.DoEvents();        //由于卫星数据文件较大，读入处理时间较长，在界面上给予提示并使窗口暂时失效以拒绝人机交互！

                theFile = opfd.FileName;
                if (theFile != "")
                {
                    curDir = System.IO.Path.GetDirectoryName(theFile);		//一旦文件选择有效则记录当前目录。
                    strCurStatus = "";
                    try
                    {   //以下这一句话实例化了FY4卫星数据类，是全部程序的核心。它屏蔽了FY4数据复杂的结构，使你可以集中精力在具体专业问题上。
                        fy4MajorDat = new Fy4HDF5Dat.Fy4HDF5Dat(theFile);
                        if (fy4MajorDat.strStatus.IndexOf(@"Invalid Filename") > 0)
                        {
                            Mouse.OverrideCursor = tmpMouse;
                            lbStr.txtLongStr = "文 件 参 数";

                            strCurStatus = "初始化不成功！\r\n";
                            txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                            return;
                        }       //如果实例化不成功，设置状态信息退出。
                    }
                    catch (Exception ecpt)
                    {
                        strCurStatus = ecpt.Message + "     文件名非法！！！";
                    }
                    finally
                    {
                    }
                    if (strCurStatus != "")
                    {
                        Mouse.OverrideCursor = tmpMouse;
                        lbStr.txtLongStr = "文 件 参 数";

                        txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                        return;
                    }               //上面这几句话都是处理实例化意外不成功的情况。
                }
                else
                {
                    Mouse.OverrideCursor = tmpMouse;
                    lbStr.txtLongStr = "文 件 参 数";

                    strCurStatus = "没有正确的选定文件。\r\n";
                    txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                    return;
                }
                for (int iy = 0; iy < fy4MajorDat.datSetsNames.Length; iy++)
                {
                    txtDisp.Text += fy4MajorDat.datSetsNames[iy] + "\r\n   [" + iy.ToString() + "]----[" + fy4MajorDat.datRow[iy].ToString() + ","
                        + fy4MajorDat.datColumn[iy].ToString() + "]\r\n";
                }       //这个循环将实例化后的FY4数据类的属性作为文件信息显示在承担信息输出的文本框里。

                string strDTtmp = "";
                foreach (Fy4HDF5Dat.CbBoxContent attrTmp in fy4MajorDat.cbAttributeLst)
                {
                    if ((attrTmp.cbname == "Satellite Name") || (attrTmp.cbname == "Sensor Name") || (attrTmp.cbname == "ProductID"))
                    {
                        satNameetc += attrTmp.cbname.Replace("\0", " ") + "：\r\n   " + attrTmp.cbvalue.Replace("\0", " ") + "\r\n";
                    }
                    else if ((attrTmp.cbname == "Observing Ending Date") || (attrTmp.cbname == "Observing Ending Time"))
                    {
                        satNameetc += attrTmp.cbname.Replace("\0", " ") + "：\r\n   " + attrTmp.cbvalue.Replace("\0", " ") + "\r\n";
                        strDTtmp += attrTmp.cbvalue.Replace("\0", " ");
                    }
                    else if ((attrTmp.cbname == "dSteppingAngle"))
                    {
                        satNameetc += attrTmp.cbname.Replace("\0", " ") + "：\r\n" + attrTmp.cbvalue.Replace("\0", " ") + "\r\n";
                        steppingAngle = Convert.ToInt32(attrTmp.cbvalue);
                    }
                }
                dtObsEnd = Convert.ToDateTime(strDTtmp);
                txtDisp.Text = satNameetc + "\r\n" + txtDisp.Text;

                int imgNum = 0;
                for (int ix = 0; ix < fy4MajorDat.datSetsNum; ix++)
                {
                    if (fy4MajorDat.datSetsNames[ix].Contains("NOMChannel")) imgNum++;
                }           //这个循环遍历了FY4数据文件中包含的数据集名称，并确定其中包含有几个光谱通道的数据。
                NOMChannels = imgNum;
                string fImgName = "";
                switch (imgNum)
                {
                    case 1:
                        fImgName = "Channel02"; //如果只包含一个通道，则选取这个通道的数据作为全区域显示。
                        break;
                    case 3:
                        fImgName = "Channel03"; //如果有3个通道，则选取3通道数据作为全区域显示。
                        break;
                    case 7:
                        fImgName = "Channel06"; //如果包含7个通道，则选取6通道数据作为全区域显示。
                        break;
                    case 14:
                        fImgName = "Channel14"; //如果是14个通道，则选取14通道数据作全区域显示。
                        break;
                    default:
                        fImgName = "";
                        imgNum = 0;
                        break;
                }
                if (imgNum == 0)
                {
                    Mouse.OverrideCursor = tmpMouse;
                    lbStr.txtLongStr = "文 件 参 数";

                    strCurStatus = "没有正确的选定文件。\r\n";
                    txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                    return;
                }
                theBitmaps = new Bitmap[imgNum + 1];    //有几个通道则准备几个图像对象和内存池，以备用。
                theBitmapStreams = new MemoryStream[imgNum + 1];
                thebmpSources = new ImageSource[imgNum + 1];
                theimgSourCons = new ImageSourceConverter[imgNum + 1];

                try
                {   //调用FY4数据类的类方法获取其全区域图像，一句话解决问题！
                    fy4MajorDat.GetaChannelWholeImg(ref fImgName, out theBitmaps[0]);
                    if (theBitmaps[0] == null)
                    {
                        Mouse.OverrideCursor = tmpMouse;
                        lbStr.txtLongStr = "文 件 参 数";

                        strCurStatus = "获取全幅图像失败！！！\r\n";
                        txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                        return;
                    }
                    theBitmapStreams[0] = new MemoryStream();
                    theimgSourCons[0] = new ImageSourceConverter();
                    theBitmaps[0].Save(theBitmapStreams[0], System.Drawing.Imaging.ImageFormat.Png);
                    thebmpSources[0] = (ImageSource)theimgSourCons[0].ConvertFrom(theBitmapStreams[0]);
                    imgGrid00.Source = thebmpSources[0];    //在界面输出获得的图像。
                }
                catch (Exception ecpt)
                {
                    Mouse.OverrideCursor = tmpMouse;
                    lbStr.txtLongStr = "文 件 参 数";

                    strCurStatus = ecpt.Message + "  显示图像失败！！！";
                    txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                    return;         //处理调用类方法和图像操作中的意外。
                }

                int numImages = imgNum + 1;     //根据包含的光谱通道数，调用下列子程序显示每一个通道的选择区域数据。
                bool btmp = int.TryParse(txtNLat.Text, out iLn);        //首先文本框的内容要正常转意为整形数字
                btmp = (btmp && int.TryParse(txtSLat.Text, out iLs));
                btmp = (btmp && int.TryParse(txtWLon.Text, out iLw));
                btmp = (btmp && int.TryParse(txtELon.Text, out iLe));
                btmp = (btmp && int.TryParse(txtLLStep.Text, out iStep));
                btmp = (btmp && (iLn > iLs) && (iLe > iLw));    //其次要保证北纬度数大于南纬，东经度数大于西经，从而构成区域。
                if (!btmp)
                {
                    Mouse.OverrideCursor = tmpMouse;
                    lbStr.txtLongStr = "文 件 参 数";

                    strCurStatus = "获取图像区域失败！！！\r\n";
                    txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                    return;
                }       //处理调用中出现的意外。

                btmp = ViewRegionImages(numImages);

                Mouse.OverrideCursor = tmpMouse;
                btnSaveArea.IsEnabled = true;
                lbStr.txtLongStr = "文 件 参 数";       //这是在卫星数据读入处理告一段落后，恢复原界面。以上凡是有返回的地方均执行了这个操作。
            }
            else
            {
                strCurStatus = "没有正确的选定文件。\r\n";
                txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                return;
            }   //处理文件选择中的意外。
        }

        /// <summary>选取区域经纬范围可以在这里改变，确认这种改变后将显示新选取区域的数据</summary>
        private void txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnAffirm.IsEnabled = true; //当在界面上改变选择区域的文本时，触发按钮，询问用户确认改变。
        }

        /// <summary>这是对新选取区域的确认，确认将引发重新取出新选区域的数据并显示</summary>
        private void btnAffirm_Click(object sender, RoutedEventArgs e)
        {       //确认用户改变了选择区域，确认主要任务是检查改变的合理性。
            btnAffirm.IsEnabled = false;
            bool btmp = true;
            Regex regex = new Regex(@"^\d{5,6}$");
            Match match = regex.Match(txtNLat.Text);    //用正则表达式核准文本框的内容符合输入数据的要求。
            btmp = (btmp && match.Success);
            match = regex.Match(txtWLon.Text);
            btmp = (btmp && match.Success);
            match = regex.Match(txtSLat.Text);
            btmp = (btmp && match.Success);
            match = regex.Match(txtELon.Text);
            btmp = (btmp && match.Success);
            Regex regex0 = new Regex(@"^\d{1,3}$");
            match = regex0.Match(txtLLStep.Text);
            btmp = (btmp && match.Success);
            if (!btmp)
            {       //任何不合规的数据都不予认可，同时数据恢复默认值。
                strCurStatus = "你输入的经纬度或经纬步长数字不正确！！！\r\n";
                txtDisp.Text = strCurStatus + txtDisp.Text;
                txtNLat.Text = "50000";
                txtWLon.Text = "70000";
                txtSLat.Text = "30000";
                txtELon.Text = "100000";
                txtLLStep.Text = "50";
                return;
            }

            btmp = int.TryParse(txtNLat.Text, out iLn);        //首先文本框的内容要正常转意为整形数字
            btmp = (btmp && int.TryParse(txtSLat.Text, out iLs));
            btmp = (btmp && int.TryParse(txtWLon.Text, out iLw));
            btmp = (btmp && int.TryParse(txtELon.Text, out iLe));
            btmp = (btmp && int.TryParse(txtLLStep.Text, out iStep));
            btmp = (btmp && (iLn > iLs) && (iLe > iLw));    //其次要保证北纬度数大于南纬，东经度数大于西经，从而构成区域。
            if (!btmp)
            {
                strCurStatus = "获取图像区域失败！！！\r\n";
                txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                return;
            }       //处理调用中出现的意外。

            if (theBitmaps != null)
            {
                int numImages = theBitmaps.Length;  //如果区域改变合理，则重新显示新选择区域的数据图像。
                lbStr.txtLongStr = "请稍候！！！.....";
                Cursor tmpMouse = Mouse.OverrideCursor;
                Mouse.OverrideCursor = Cursors.Wait;
                DispatcherHelper.DoEvents();        //由于卫星数据文件较大，读入处理时间较长，在界面上给予提示并使窗口暂时失效以拒绝人机交互！

                btmp = ViewRegionImages(numImages);

                Mouse.OverrideCursor = tmpMouse;
                lbStr.txtLongStr = "文 件 参 数";       //这是在卫星数据读入处理告一段落后，恢复原界面。以上凡是有返回的地方均执行了这个操作。
            }

            if (!btmp)
            {
                strCurStatus = "获取图像区域失败！！！\r\n";
                txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                txtNLat.Text = "50000";
                txtWLon.Text = "70000";
                txtSLat.Text = "30000";
                txtELon.Text = "100000";
                txtLLStep.Text = "50";
                return;
            }           //如果子程序显示数据意外则恢复默认区域显示。
        }

        /// <summary>双击2号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid02_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (theBitmaps == null) return;
                int imgNum = theBitmaps.Length;
                string fChannName = "";
                switch (imgNum)
                {
                    case 4:
                        fChannName = "Channel02"; //如果有3个通道，则选取1通道数据作为01框区域显示。
                        break;
                    case 8:
                        fChannName = "Channel02"; //如果包含7个通道，则选取1通道数据作为01框区域显示。
                        break;
                    case 15:
                        fChannName = "Channel02"; //如果是14个通道，则选取14通道数据作为01框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[2];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击3号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid03_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 3) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 4:
                        fChannName = "Channel03"; //如果有3个通道，则选取1通道数据作为01框区域显示。
                        break;
                    case 8:
                        fChannName = "Channel03"; //如果包含7个通道，则选取1通道数据作为01框区域显示。
                        break;
                    case 15:
                        fChannName = "Channel03"; //如果是14个通道，则选取14通道数据作为01框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[3];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击4号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid04_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 7) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 8:
                        fChannName = "Channel04"; //如果包含7个通道，则选取1通道数据作为01框区域显示。
                        break;
                    case 15:
                        fChannName = "Channel04"; //如果是14个通道，则选取14通道数据作为01框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[4];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击5号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid05_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 7) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 8:
                        fChannName = "Channel05"; //如果包含7个通道，则选取1通道数据作为01框区域显示。
                        break;
                    case 15:
                        fChannName = "Channel05"; //如果是14个通道，则选取14通道数据作为01框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[5];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击6号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid06_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 7) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 8:
                        fChannName = "Channel06"; //如果包含7个通道，则选取1通道数据作为01框区域显示。
                        break;
                    case 15:
                        fChannName = "Channel06"; //如果是14个通道，则选取14通道数据作为01框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[6];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击7号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid07_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 7) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 8:
                        fChannName = "Channel07"; //如果包含7个通道，则选取7通道数据作为08框区域显示。
                        break;
                    case 15:
                        fChannName = "Channel07"; //如果是14个通道，则选取7通道数据作为08框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[7];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击8号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid08_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 14) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 15:
                        fChannName = "Channel08"; //如果是14个通道，则选取7通道数据作为08框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[8];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击9号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid09_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 14) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 15:
                        fChannName = "Channel09"; //如果是14个通道，则选取7通道数据作为08框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[9];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击10号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid10_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 14) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 15:
                        fChannName = "Channel10"; //如果是14个通道，则选取7通道数据作为08框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[10];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击11号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid11_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 14) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 15:
                        fChannName = "Channel11"; //如果是14个通道，则选取7通道数据作为08框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[11];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击12号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid12_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 14) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 15:
                        fChannName = "Channel12"; //如果是14个通道，则选取7通道数据作为08框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[12];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击13号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid13_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 14) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 15:
                        fChannName = "Channel13"; //如果是14个通道，则选取7通道数据作为08框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[13];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击14号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid14_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                if (imgNum < 14) return;
                string fChannName = "";
                switch (imgNum)
                {
                    case 15:
                        fChannName = "Channel14"; //如果是14个通道，则选取7通道数据作为08框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[14];
                string discribeStr = fChannName + "通道数据：\r\n" + satNameetc;
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>将各通道选定区域的数据以经纬格点数据格式的文件保存起来。</summary>
        private void btnSaveArea_Click(object sender, RoutedEventArgs e)
        {
            double dy1 = iLn / 1000.0;
            double dx1 = iLw / 1000.0;
            double dy2 = iLs / 1000.0;
            double dx2 = iLe / 1000.0;
            double dxStep = iStep / 1000.0;
            double dyStep = dxStep;
            int iLevel = 2;
            CommonGridDat.CommonGridDat theGridDats = new CommonGridDat.CommonGridDat(ref dy1, ref dy2, ref dx1, ref dx2, ref dxStep, ref dyStep, ref iLevel);
            theGridDats.gDataDiscr[0].DYear = dtObsEnd.Year;
            theGridDats.gDataDiscr[1].DYear = dtObsEnd.Year;
            theGridDats.gDataDiscr[0].DMounth = dtObsEnd.Month;
            theGridDats.gDataDiscr[1].DMounth = dtObsEnd.Month;
            theGridDats.gDataDiscr[0].DDay = dtObsEnd.Day;
            theGridDats.gDataDiscr[1].DDay = dtObsEnd.Day;
            theGridDats.gDataDiscr[0].DTime = dtObsEnd.Hour;
            theGridDats.gDataDiscr[1].DTime = dtObsEnd.Hour;
            theGridDats.gDataDiscr[0].DMinute = dtObsEnd.Minute;
            theGridDats.gDataDiscr[1].DMinute = dtObsEnd.Minute;
            theGridDats.gDataDiscr[0].DSecond = dtObsEnd.Second;
            theGridDats.gDataDiscr[1].DSecond = dtObsEnd.Second;

            string[] saveFilenames = new string[NOMChannels];

            string thePath = svDataPath;
            if (!Directory.Exists(thePath))
            {
                Directory.CreateDirectory(thePath);
            }
            fbd.SelectedPath = thePath;
            fbd.Description = "欲保存的文件的路径选在这儿？请选择！";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!thePath.Contains(fbd.SelectedPath)) thePath = fbd.SelectedPath + "\\";
                Cursor tmpMouse = Mouse.OverrideCursor;
                Mouse.OverrideCursor = Cursors.Wait;
                btnSaveArea.IsEnabled = false;

                string strTmp;
                int[,] viewGrayDats;
                float[,] viewValueDats;
                string thePathTmp = "";
                string strStep = "0" + iStep.ToString();        //设这个字符串是要在文件名里加入步长因素，有利于后期文件性质识别。
                strStep = "-" + strStep.Substring(strStep.Length - 2);
                for (int iz = 0; iz < NOMChannels; iz++)
                {
                    strTmp = (NOMChannels == 1) ? "02" : "0" + (iz + 1).ToString();
                    strTmp = strTmp.Substring(strTmp.Length - 2);
                    strTmp = "Channel" + strTmp;
                    thePathTmp = thePath + strTmp + "\\" + iStep.ToString() + "\\";
                    if (!Directory.Exists(thePathTmp))
                    {
                        Directory.CreateDirectory(thePathTmp);
                    }
                    saveFilenames[iz] = thePathTmp + strTmp + dtObsEnd.ToString("yyyyMMddHHmmss") + strStep + ".Grd";
                    theGridDats.gDataDiscr[0].DataDiscr = strTmp + "-GrayDatas";
                    theGridDats.gDataDiscr[1].DataDiscr = strTmp + "-ValueDatas";
                    try
                    {
                        fy4MajorDat.GetaChannelAreaData(ref strTmp, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                            out viewGrayDats, out viewValueDats);
                    }
                    catch (Exception ecpt)
                    {
                        strCurStatus = ecpt.Message + "  调取" + strTmp + "通道数据时意外失败！！！";
                        txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                        return;         //处理调用类方法或数据操作中的意外。
                    }
                    for (int iy = 0; iy < theGridDats.iPixY; iy++)
                    {
                        for (int ix = 0; ix < theGridDats.iPixX; ix++)
                        {
                            theGridDats.iLLGrid[iy, ix, 0] = viewGrayDats[iy, ix];
                            theGridDats.iLLGrid[iy, ix, 1] = (int)(viewValueDats[iy, ix] * 1000 + 0.5);
                        }
                    }
                    theGridDats.LookForStatistic();

                    try
                    {
                        IFormatter theDatFormat = new BinaryFormatter();
                        Stream theDatstream = new FileStream(saveFilenames[iz], FileMode.Create, FileAccess.Write, FileShare.None);
                        theDatFormat.Serialize(theDatstream, theGridDats);
                        theDatstream.Close();       //保存当前数据文件。
                    }
                    catch (Exception ecpt)
                    {
                        txtDisp.Text += "数据文件创建出错。错误码：" + ecpt.Message + "\r\n";
                        return;
                    }
                }
                txtDisp.Text = "区域数据保存成功\r\n" + txtDisp.Text;
                Mouse.OverrideCursor = tmpMouse;
                btnSaveArea.IsEnabled = true;
            }

        }

        /// <summary>这是一个显示选择区域图像的子程序，从loadfile中分离出来主要是代码重复使用的需要</summary>
        private bool ViewRegionImages(int numImages)
        {
            bool isOK = true;
            string nameChann, imgName = "";
            for (int ix = 1; ix < numImages; ix++)
            {
                imgName = "0" + ix.ToString();
                if (numImages == 2)
                {
                    nameChann = "Channel02";
                }
                else
                {
                    nameChann = "Channel" + imgName.Substring(0, 2);
                }
                try
                {       //通过这个循环，一个个输出获取的区域图像。
                    fy4MajorDat.GetaChannelAreaImage(ref nameChann, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep, out theBitmaps[ix]);
                    if (theBitmaps[ix] == null)
                    {
                        strCurStatus = "获取图像失败！！！\r\n";
                        txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                        isOK = false;
                        return isOK;
                    }
                    theBitmapStreams[ix] = new MemoryStream();
                    theimgSourCons[ix] = new ImageSourceConverter();
                    theBitmaps[ix].Save(theBitmapStreams[ix], System.Drawing.Imaging.ImageFormat.Png);
                    thebmpSources[ix] = (ImageSource)theimgSourCons[ix].ConvertFrom(theBitmapStreams[ix]);
                    switch (ix)
                    {           //输出获得的图像到界面。
                        case 1:
                            imgGrid01.Source = thebmpSources[ix];
                            break;
                        case 2:
                            imgGrid02.Source = thebmpSources[ix];
                            break;
                        case 3:
                            imgGrid03.Source = thebmpSources[ix];
                            break;
                        case 4:
                            imgGrid04.Source = thebmpSources[ix];
                            break;
                        case 5:
                            imgGrid05.Source = thebmpSources[ix];
                            break;
                        case 6:
                            imgGrid06.Source = thebmpSources[ix];
                            break;
                        case 7:
                            imgGrid07.Source = thebmpSources[ix];
                            break;
                        case 8:
                            imgGrid08.Source = thebmpSources[ix];
                            break;
                        case 9:
                            imgGrid09.Source = thebmpSources[ix];
                            break;
                        case 10:
                            imgGrid10.Source = thebmpSources[ix];
                            break;
                        case 11:
                            imgGrid11.Source = thebmpSources[ix];
                            break;
                        case 12:
                            imgGrid12.Source = thebmpSources[ix];
                            break;
                        case 13:
                            imgGrid13.Source = thebmpSources[ix];
                            break;
                        case 14:
                            imgGrid14.Source = thebmpSources[ix];
                            break;
                    }
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  显示区域图像失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    isOK = false;
                    return isOK;
                }           //意外处理。
            }

            return isOK;
        }

        /// <summary>双击1号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid00_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (theBitmaps == null) return;
                int imgNum = theBitmaps.Length;
                string fChannName = "";
                switch (imgNum)
                {
                    case 2:
                        fChannName = "Channel02"; //如果只包含一个通道，则选取这个通道的数据作为全区域显示。
                        break;
                    case 4:
                        fChannName = "Channel03"; //如果有3个通道，则选取3通道数据作为全区域显示。
                        break;
                    case 8:
                        fChannName = "Channel06"; //如果包含7个通道，则选取6通道数据作为全区域显示。
                        break;
                    case 15:
                        fChannName = "Channel14"; //如果是14个通道，则选取14通道数据作全区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int leftUpY, leftUpX, rightDownY, rightDownX = 0;
                int oneStep = 1;
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[0];
                string discribeStr = fChannName + "通道数据：\r\n";
                try
                {
                    fy4MajorDat.GetaChannelWholeDat(ref fChannName, out leftUpY, out leftUpX, out rightDownY, out rightDownX,
                        out viewGrayDats, out viewValueDats);
                    foreach (Fy4HDF5Dat.CbBoxContent attrTmp in fy4MajorDat.cbAttributeLst)
                    {
                        if ((attrTmp.cbname == "Satellite Name") || (attrTmp.cbname == "Sensor Name") || (attrTmp.cbname == "ProductID")
                            || (attrTmp.cbname == "Observing Ending Date") || (attrTmp.cbname == "Observing Ending Time"))
                        {
                            discribeStr += attrTmp.cbname + "：\r\n    " + attrTmp.cbvalue + "\r\n";
                        }
                    }
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道全数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                TransParamToSon toSonParam = new TransParamToSon(leftUpY, leftUpX, rightDownY, rightDownX, oneStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>双击2号图像框，弹出放大窗口，处理放大后图像的操作。</summary>
        private void imgGrid01_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                int imgNum = theBitmaps.Length;
                string fChannName = "";
                switch (imgNum)
                {
                    case 2:
                        fChannName = "Channel02"; //如果只包含一个通道，则选取这个通道的数据作为01框区域显示。
                        break;
                    case 4:
                        fChannName = "Channel01"; //如果有3个通道，则选取1通道数据作为01框区域显示。
                        break;
                    case 8:
                        fChannName = "Channel01"; //如果包含7个通道，则选取1通道数据作为01框区域显示。
                        break;
                    case 15:
                        fChannName = "Channel01"; //如果是14个通道，则选取14通道数据作为01框区域显示。
                        break;
                    default:
                        fChannName = "";
                        imgNum = 0;
                        break;
                }
                int[,] viewGrayDats;
                float[,] viewValueDats;
                Bitmap viewMap = theBitmaps[1];
                string discribeStr = fChannName + "通道数据：\r\n";
                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref fChannName, ref iLn, ref iLw, ref iLs, ref iLe, ref iStep,
                        out viewGrayDats, out viewValueDats);
                    foreach (Fy4HDF5Dat.CbBoxContent attrTmp in fy4MajorDat.cbAttributeLst)
                    {
                        if ((attrTmp.cbname == "Satellite Name") || (attrTmp.cbname == "Sensor Name") || (attrTmp.cbname == "ProductID")
                            || (attrTmp.cbname == "Observing Ending Date") || (attrTmp.cbname == "Observing Ending Time"))
                        {
                            discribeStr += attrTmp.cbname + "：\r\n   " + attrTmp.cbvalue + "\r\n";
                        }
                    }
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  调取" + fChannName + "通道全数据时意外失败！！！";
                    txtDisp.Text += "\r\n" + strCurStatus + "\r\n";
                    return;         //处理调用类方法或数据操作中的意外。
                }
                discribeStr += "纬度：" + iLs.ToString() + "---" + iLn.ToString() + "\r\n";
                discribeStr += "经度：" + iLw.ToString() + "---" + iLe.ToString() + "\r\n";
                discribeStr += "经纬步长：" + iStep.ToString() + "\r\n";
                TransParamToSon toSonParam = new TransParamToSon(iLn, iLw, iLs, iLe, iStep, viewGrayDats,
                    viewValueDats, viewMap, discribeStr, svDataPath, svBitmapPath);

                PopupImgView sonWin = new PopupImgView(toSonParam);
                sonWin.Owner = this;
                sonWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                sonWin.PassOKEvent += new PopupImgView.PassValuesHandler(GetSonRtn);  //订阅弹出窗口关闭事件

                sonWin.ShowDialog();         //操作窗口弹出。
            }
        }

        /// <summary>处理弹出辅助窗口返回事件</summary>
        private void GetSonRtn(object sender, PassValuesEventArgs e)
        {
            bool sonIsOk = e.theSonIsOk;
        }

        /// <summary>处理程序启动后初始参数的初始化，方法是读入与运行程序同目录下的一个初始化文本文件。</summary>
        private void getIni()
        {
            try
            {
                bool btmp = !File.Exists(AppDomain.CurrentDomain.BaseDirectory + "FY4A成像仪数据处理程序参数.ini");
                if (btmp)
                {
                    txtDisp.Text = "程序初始化文件有错误，请退出查找原因，不建议继续执行！！！\r\n";
                    return;
                }
                else
                {
                    string strLine = string.Empty;
                    List<string> iniFileContent = new List<string>();
                    using (StreamReader r_ini = new StreamReader("FY4A成像仪数据处理程序参数.ini"))
                    {
                        strLine = r_ini.ReadLine();
                        while (strLine != null)
                        {
                            iniFileContent.Add(strLine);
                            strLine = r_ini.ReadLine();
                        }
                    }
                    int iTmp = 0;
                    foreach (string strTmp in iniFileContent)
                    {
                        iTmp = strTmp.IndexOf("=") + 1;
                        if (strTmp.Contains("western boundary="))
                            txtWLon.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                        else if (strTmp.Contains("eastern boundary="))
                            txtELon.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                        else if (strTmp.Contains("northern boundary="))
                            txtNLat.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                        else if (strTmp.Contains("southern boundary="))
                            txtSLat.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                        else if (strTmp.Contains("latitude and longitude step="))
                            txtLLStep.Text = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                        else if (strTmp.Contains("data file path="))
                            svDataPath = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                        else if (strTmp.Contains("image file path="))
                            svBitmapPath = strTmp.Substring(iTmp, strTmp.Length - iTmp);
                        else if (strTmp.Contains("file retention="))
                        {
                            iFileRetained = Convert.ToInt32(strTmp.Substring(iTmp, strTmp.Length - iTmp));
                            iFileRetained = ((iFileRetained > 0) && (iFileRetained < 32)) ? iFileRetained : 5;        //默认数据保留5天。
                        }
                        else if (strTmp.Contains("regular file expression="))
                            regFileName = new Regex(strTmp.Substring(iTmp, strTmp.Length - iTmp));
                        else if (strTmp.Contains("multiresolution identifier="))
                            multiresolutionID = (strTmp.Substring(iTmp, strTmp.Length - iTmp) == "true") ? true : false;
                    }
                }

                btmp = int.TryParse(txtNLat.Text, out iLn);        //首先文本框的内容要正常转意为整形数字
                btmp = (btmp && int.TryParse(txtSLat.Text, out iLs));
                btmp = (btmp && int.TryParse(txtWLon.Text, out iLw));
                btmp = (btmp && int.TryParse(txtELon.Text, out iLe));
                btmp = (btmp && int.TryParse(txtLLStep.Text, out iStep));
                btmp = (btmp && (iLn > iLs) && (iLe > iLw));    //其次要保证北纬度数大于南纬，东经度数大于西经，从而构成区域。

                if (btmp)
                {
                    txtDisp.Text = "程序初始化文件有错误，请退出查找原因，不建议继续执行！！！\r\n";
                    return;
                }
            }
            catch (Exception excp)
            {
                txtDisp.Text = "程序初始化文件有错误，请退出查找原因，不建议继续执行！！！\r\n" + excp.Message + "\r\n";
            }
        }

        /// <summary>处理用户批量处理FY4A成像仪数据文件的请求。</summary>
        private void BatchFilesProc_Click(object sender, RoutedEventArgs e)
        {
            string[] tmpFileNames;
            System.Windows.Media.Color cSalmon = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("Salmon");
            System.Windows.Media.Color cLightGreen = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("LightGreen");
            bool btmp = int.TryParse(txtNLat.Text, out iLn);        //首先文本框的内容要正常转意为整形数字
            btmp = (btmp && int.TryParse(txtSLat.Text, out iLs));
            btmp = (btmp && int.TryParse(txtWLon.Text, out iLw));
            btmp = (btmp && int.TryParse(txtELon.Text, out iLe));
            btmp = (btmp && int.TryParse(txtLLStep.Text, out iStep));
            btmp = (btmp && (iLn > iLs) && (iLe > iLw));    //其次要保证北纬度数大于南纬，东经度数大于西经，从而构成区域。
            if (!btmp)
            {
                strCurStatus = "获取图像区域有问题！！！\r\n";
                txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                return;
            }       //处理调用中出现的意外。
            double dy1 = iLn / 1000.0;
            double dx1 = iLw / 1000.0;
            double dy2 = iLs / 1000.0;
            double dx2 = iLe / 1000.0;
            double dxStep = iStep / 1000.0;
            double dyStep = dxStep;
            int iLevel = 2;
            CommonGridDat.CommonGridDat theGridDats;

            Cursor tmpMouse = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
            btnSaveArea.IsEnabled = false;
            btnLoadFile.IsEnabled = false;
            BatchFilesProc.IsEnabled = false;
            NewFilesMonitor.IsEnabled = false;
            StopTheProc.IsEnabled = true;
            menuBat.Background = new SolidColorBrush(cSalmon);

            opfd.InitialDirectory = curDir;
            opfd.DefaultExt = "HDF";
            opfd.Filter = "HDF Files(*.HDF;*.hdf)|*.HDF;*.hdf|All Files(*.*)|*.*";
            opfd.Title = "这个程序装载过程需要选择一个或多个正规的Fy-4AHDF文件，它可以批量处理并形成选定区域等经纬投影的数据文件。";
            opfd.Multiselect = true;
            if (opfd.ShowDialog() == true)
            {
                tmpFileNames = new string[opfd.FileNames.Length];
                if (tmpFileNames.Length < 1)
                {
                    Mouse.OverrideCursor = tmpMouse;
                    btnSaveArea.IsEnabled = true;
                    btnLoadFile.IsEnabled = true;
                    BatchFilesProc.IsEnabled = true;
                    NewFilesMonitor.IsEnabled = true;
                    StopTheProc.IsEnabled = false;
                    menuBat.Background = new SolidColorBrush(cLightGreen);

                    strCurStatus = "文件选择不正确！\r\n";
                    txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                    return;
                }
                tmpFileNames = opfd.FileNames;
                curDir = System.IO.Path.GetDirectoryName(tmpFileNames[0]);      //注意！：这里把当前目录转成为数据目录了！并且这将成为自动侦测的默认目录。
                DispatcherHelper.DoEvents();        //由于卫星数据文件较大，读入处理时间较长，给时间完成前期工作后进入循环！

                foreach (string theFile in tmpFileNames)
                {
                    strCurStatus = "";          //准备读入新文件了，FY4数据类的状态清零以准备反映新的状态
                    satNameetc = "";            //卫星名称、仪器名称等也清零，准备装入新的.
                    NOMChannels = 0;            //新文件中包含的光谱通道数也预设为0
                    try
                    {   //以下这一句话实例化了FY4卫星数据类，是全部程序的核心。它屏蔽了FY4数据复杂的结构，使你可以集中精力在具体专业问题上。
                        fy4MajorDat = new Fy4HDF5Dat.Fy4HDF5Dat(theFile);
                        if (fy4MajorDat.strStatus.IndexOf(@"Invalid Filename") > 0)
                        {
                            Mouse.OverrideCursor = tmpMouse;
                            btnSaveArea.IsEnabled = true;
                            btnLoadFile.IsEnabled = true;
                            BatchFilesProc.IsEnabled = true;
                            NewFilesMonitor.IsEnabled = true;
                            StopTheProc.IsEnabled = false;
                            menuBat.Background = new SolidColorBrush(cLightGreen);

                            strCurStatus = "批处理文件因有问题而中断！\r\n";
                            txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                            return;
                        }       //如果实例化不成功，设置状态信息退出。
                    }
                    catch (Exception ecpt)
                    {
                        strCurStatus = ecpt.Message + "     批处理文件名非法！！！";
                    }
                    finally
                    {
                    }
                    if (strCurStatus != "")
                    {
                        Mouse.OverrideCursor = tmpMouse;
                        btnSaveArea.IsEnabled = true;
                        btnLoadFile.IsEnabled = true;
                        BatchFilesProc.IsEnabled = true;
                        NewFilesMonitor.IsEnabled = true;
                        StopTheProc.IsEnabled = false;
                        menuBat.Background = new SolidColorBrush(cLightGreen);

                        txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                        return;
                    }               //上面这几句话都是处理实例化意外不成功的情况。
                    for (int iy = 0; iy < fy4MajorDat.datSetsNames.Length; iy++)
                    {
                        txtDisp.Text = fy4MajorDat.datSetsNames[iy] + "\r\n   [" + iy.ToString() + "]----[" + fy4MajorDat.datRow[iy].ToString() + ","
                            + fy4MajorDat.datColumn[iy].ToString() + "]\r\n" + txtDisp.Text;
                    }       //这个循环将实例化后的FY4数据类的属性作为文件信息显示在承担信息输出的文本框里。

                    string strDTtmp = "";
                    foreach (Fy4HDF5Dat.CbBoxContent attrTmp in fy4MajorDat.cbAttributeLst)
                    {
                        if ((attrTmp.cbname == "Satellite Name") || (attrTmp.cbname == "Sensor Name") || (attrTmp.cbname == "ProductID"))
                        {
                            satNameetc += attrTmp.cbname.Replace("\0", " ") + "：\r\n   " + attrTmp.cbvalue.Replace("\0", " ") + "\r\n";
                        }
                        else if ((attrTmp.cbname == "Observing Ending Date") || (attrTmp.cbname == "Observing Ending Time"))
                        {
                            satNameetc += attrTmp.cbname.Replace("\0", " ") + "：\r\n   " + attrTmp.cbvalue.Replace("\0", " ") + "\r\n";
                            strDTtmp += attrTmp.cbvalue.Replace("\0", " ");
                        }
                        else if ((attrTmp.cbname == "dSteppingAngle"))
                        {
                            satNameetc += attrTmp.cbname.Replace("\0", " ") + "：\r\n" + attrTmp.cbvalue.Replace("\0", " ") + "\r\n";
                            steppingAngle = Convert.ToInt32(attrTmp.cbvalue);
                        }
                    }
                    dtObsEnd = Convert.ToDateTime(strDTtmp);

                    int imgNum = 0;
                    for (int ix = 0; ix < fy4MajorDat.datSetsNum; ix++)
                    {
                        if (fy4MajorDat.datSetsNames[ix].Contains("NOMChannel")) imgNum++;
                    }           //这个循环遍历了FY4数据文件中包含的数据集名称，并确定其中包含有几个光谱通道的数据。
                    NOMChannels = imgNum;
                    if (imgNum == 0)
                    {
                        Mouse.OverrideCursor = tmpMouse;
                        btnSaveArea.IsEnabled = true;
                        btnLoadFile.IsEnabled = true;
                        BatchFilesProc.IsEnabled = true;
                        NewFilesMonitor.IsEnabled = true;
                        StopTheProc.IsEnabled = false;
                        menuBat.Background = new SolidColorBrush(cLightGreen);

                        strCurStatus = "没有正确的选定文件。\r\n";
                        txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                        return;
                    }
                    string[] saveFilenames = new string[NOMChannels];
                    string[] bmpFilenames = new string[NOMChannels];
                    string strTmp;
                    int[,] viewGrayDats;
                    float[,] viewValueDats;
                    string thePathTmp = "";
                    string bmpPathTmp = "";

                    string thePath = svDataPath;                //存储数据的路径，等经纬数据将分类存此路径下。
                    if (!Directory.Exists(thePath))
                    {
                        Directory.CreateDirectory(thePath);
                    }
                    string bmpPath = svBitmapPath;           //存储图片的路径，bitmap图片将分类存此路径下。
                    if (!Directory.Exists(bmpPath))
                    {
                        Directory.CreateDirectory(bmpPath);
                    }

                    double theStepX = dxStep;
                    double theStepY = dyStep;
                    if (multiresolutionID)
                    {                       //成像仪步进视角与经纬步长取值换算为此。考虑到取值在整数度内取整，避过0.04。
                        theStepX = (double)steppingAngle / 2800.0;          //0.28 / steppingAngle;
                        theStepX = ((theStepX > 0.02) && (theStepX < 0.06)) ? 0.05 : theStepX;
                        if (NOMChannels > 10)
                            theStepX = 0.05;
                        else if (NOMChannels > 4)
                            theStepX = 0.02;
                        else if (NOMChannels > 1)
                            theStepX = 0.01;
                        else if (NOMChannels == 1)           //加这一段的原因是steppingAngle的定义竟然随意变动！卫星中心真是太不靠谱！
                            theStepX = 0.005;
                        else
                            theStepX = 0.05;
                        theStepY = theStepX;
                    }
                    int theStepTmp = (int)(theStepX * 1000);
                    string strStep = "0" + theStepTmp.ToString();        //设这个字符串是要在文件名里加入步长因素，有利于后期文件性质识别。
                    strStep = "-" + strStep.Substring(strStep.Length - 2);

                    theGridDats = new CommonGridDat.CommonGridDat(ref dy1, ref dy2, ref dx1, ref dx2, ref theStepX, ref theStepY, ref iLevel);
                    theGridDats.gDataDiscr[0].DYear = dtObsEnd.Year;
                    theGridDats.gDataDiscr[1].DYear = dtObsEnd.Year;
                    theGridDats.gDataDiscr[0].DMounth = dtObsEnd.Month;
                    theGridDats.gDataDiscr[1].DMounth = dtObsEnd.Month;
                    theGridDats.gDataDiscr[0].DDay = dtObsEnd.Day;
                    theGridDats.gDataDiscr[1].DDay = dtObsEnd.Day;
                    theGridDats.gDataDiscr[0].DTime = dtObsEnd.Hour;
                    theGridDats.gDataDiscr[1].DTime = dtObsEnd.Hour;
                    theGridDats.gDataDiscr[0].DMinute = dtObsEnd.Minute;
                    theGridDats.gDataDiscr[1].DMinute = dtObsEnd.Minute;
                    theGridDats.gDataDiscr[0].DSecond = dtObsEnd.Second;
                    theGridDats.gDataDiscr[1].DSecond = dtObsEnd.Second;

                    for (int iz = 0; iz < NOMChannels; iz++)
                    {
                        strTmp = (NOMChannels == 1) ? "02" : "0" + (iz + 1).ToString();
                        strTmp = strTmp.Substring(strTmp.Length - 2);
                        strTmp = "Channel" + strTmp;

                        thePathTmp = thePath + strTmp + "\\" + theStepTmp.ToString() + "\\";
                        bmpPathTmp = bmpPath + strTmp + "\\" + theStepTmp.ToString() + "\\";
                        if (!Directory.Exists(thePathTmp))
                        {
                            Directory.CreateDirectory(thePathTmp);
                        }                                           //确认了数据存储路径。
                        saveFilenames[iz] = thePathTmp + strTmp + dtObsEnd.ToString("yyyyMMddHHmmss") + strStep + ".Grd";
                        if (!Directory.Exists(bmpPathTmp))
                        {
                            Directory.CreateDirectory(bmpPathTmp);
                        }                                           //确认了图片存储路径。
                        bmpFilenames[iz] = bmpPathTmp + strTmp + dtObsEnd.ToString("yyyyMMddHHmmss") + strStep + ".Bmp";

                        theGridDats.gDataDiscr[0].DataDiscr = strTmp + "-GrayDatas";
                        theGridDats.gDataDiscr[1].DataDiscr = strTmp + "-ValueDatas";
                        try
                        {
                            fy4MajorDat.GetaChannelAreaData(ref strTmp, ref iLn, ref iLw, ref iLs, ref iLe, ref theStepTmp,
                                out viewGrayDats, out viewValueDats);
                        }
                        catch (Exception ecpt)
                        {
                            Mouse.OverrideCursor = tmpMouse;
                            btnSaveArea.IsEnabled = true;
                            btnLoadFile.IsEnabled = true;
                            BatchFilesProc.IsEnabled = true;
                            NewFilesMonitor.IsEnabled = true;
                            StopTheProc.IsEnabled = false;
                            menuBat.Background = new SolidColorBrush(cLightGreen);

                            strCurStatus = ecpt.Message + "  批量处理中调取" + strTmp + "通道数据时意外失败！！！";
                            txtDisp.Text = "\r\n" + strCurStatus + "\r\n" + txtDisp.Text;
                            return;         //处理调用类方法或数据操作中的意外。
                        }
                        for (int iy = 0; iy < theGridDats.iPixY; iy++)
                        {
                            for (int ix = 0; ix < theGridDats.iPixX; ix++)
                            {
                                theGridDats.iLLGrid[iy, ix, 0] = viewGrayDats[iy, ix];
                                theGridDats.iLLGrid[iy, ix, 1] = (int)(viewValueDats[iy, ix] * 1000 + 0.5);
                            }
                        }
                        theGridDats.LookForStatistic();
                        Bitmap bmpImageTmp;
                        int levelTmp = 0;
                        theGridDats.MakeTheLevelImage(ref levelTmp, out bmpImageTmp);

                        try
                        {
                            IFormatter theDatFormat = new BinaryFormatter();
                            Stream theDatstream = new FileStream(saveFilenames[iz], FileMode.Create, FileAccess.Write, FileShare.None);
                            theDatFormat.Serialize(theDatstream, theGridDats);
                            theDatstream.Close();       //保存当前数据文件。

                            bmpImageTmp.Save(bmpFilenames[iz], System.Drawing.Imaging.ImageFormat.Bmp);
                            bmpImageTmp.Dispose();      //对应上面的数据，保存了相对应的每一个经纬数据的图像。
                        }
                        catch (Exception ecpt)
                        {
                            Mouse.OverrideCursor = tmpMouse;
                            btnSaveArea.IsEnabled = true;
                            btnLoadFile.IsEnabled = true;
                            BatchFilesProc.IsEnabled = true;
                            NewFilesMonitor.IsEnabled = true;
                            StopTheProc.IsEnabled = false;
                            menuBat.Background = new SolidColorBrush(cLightGreen);

                            txtDisp.Text = "批量处理时，数据文件创建出错。错误码：" + ecpt.Message + "\r\n" + txtDisp.Text;
                            return;
                        }

                        DispatcherHelper.DoEvents();        //由于卫星数据文件较大，读入处理时间较长，在界面上给予提示并使窗口暂时失效以拒绝人机交互！
                    }

                    DispatcherHelper.DoEvents();        //由于卫星数据文件较大，读入处理时间较长，在界面上给予提示并使窗口暂时失效以拒绝人机交互！
                    txtDisp.Text = "批量数据处理中，完成一个时次、一个通道区域数据保存，成功！\r\n" + txtDisp.Text;
                    if (usrBreakBatchProc)
                    {
                        txtDisp.Text = "批量处理FY4成像仪数据文件尚未完成，被用户强制停止！\r\n" + txtDisp.Text;
                        break;
                    }

                }
                        //这里注意：后续的显示操作仅仅只针对上述批处理中的最后处理的一个文件，也就是只显示了最后一个文件中各通道的图像。
                usrBreakBatchProc = false;      //强行终止批处理后，将标志复原。
                string fImgName = "";
                switch (NOMChannels)
                {
                    case 1:
                        fImgName = "Channel02"; //如果只包含一个通道，则选取这个通道的数据作为全区域显示。
                        break;
                    case 3:
                        fImgName = "Channel03"; //如果有3个通道，则选取3通道数据作为全区域显示。
                        break;
                    case 7:
                        fImgName = "Channel06"; //如果包含7个通道，则选取6通道数据作为全区域显示。
                        break;
                    case 14:
                        fImgName = "Channel14"; //如果是14个通道，则选取14通道数据作全区域显示。
                        break;
                    default:
                        fImgName = "";
                        break;
                }
                int bmpNum = NOMChannels + 1;
                theBitmaps = new Bitmap[bmpNum];    //有几个通道则准备几个图像对象和内存池，以备用。
                theBitmapStreams = new MemoryStream[bmpNum];
                thebmpSources = new ImageSource[bmpNum];
                theimgSourCons = new ImageSourceConverter[bmpNum];

                try
                {   //调用FY4数据类的类方法获取其全区域图像，一句话解决问题！
                    fy4MajorDat.GetaChannelWholeImg(ref fImgName, out theBitmaps[0]);
                    if (theBitmaps[0] == null)
                    {
                        Mouse.OverrideCursor = tmpMouse;
                        btnSaveArea.IsEnabled = true;
                        btnLoadFile.IsEnabled = true;
                        BatchFilesProc.IsEnabled = true;
                        NewFilesMonitor.IsEnabled = true;
                        StopTheProc.IsEnabled = false;
                        menuBat.Background = new SolidColorBrush(cLightGreen);

                        strCurStatus = "批量处理中，其中一次获取全幅图像失败！！！\r\n";
                        txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                        return;
                    }
                    theBitmapStreams[0] = new MemoryStream();
                    theimgSourCons[0] = new ImageSourceConverter();
                    theBitmaps[0].Save(theBitmapStreams[0], System.Drawing.Imaging.ImageFormat.Png);
                    thebmpSources[0] = (ImageSource)theimgSourCons[0].ConvertFrom(theBitmapStreams[0]);
                    imgGrid00.Source = thebmpSources[0];    //在界面输出获得的图像。
                }
                catch (Exception ecpt)
                {
                    Mouse.OverrideCursor = tmpMouse;
                    btnSaveArea.IsEnabled = true;
                    btnLoadFile.IsEnabled = true;
                    BatchFilesProc.IsEnabled = true;
                    NewFilesMonitor.IsEnabled = true;
                    StopTheProc.IsEnabled = false;
                    menuBat.Background = new SolidColorBrush(cLightGreen);

                    strCurStatus = ecpt.Message + "  批量处理过程中，其中一次显示图像失败！！！";
                    txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
                    return;         //处理调用类方法和图像操作中的意外。
                }

                btmp = ViewRegionImages(bmpNum);

                Mouse.OverrideCursor = tmpMouse;
                btnSaveArea.IsEnabled = true;
                btnLoadFile.IsEnabled = true;
                BatchFilesProc.IsEnabled = true;
                NewFilesMonitor.IsEnabled = true;
                StopTheProc.IsEnabled = false;
                menuBat.Background = new SolidColorBrush(cLightGreen);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                strCurStatus = "本次批量处理过程中，按用户要求全部成功！！！";
                txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
            }
            else
            {
                strCurStatus = "本次批量处理文件选择不正确！";
                txtDisp.Text = strCurStatus + "\r\n" + txtDisp.Text;
            }

        }

        /// <summary>处理用户实时监视新Fy4数据文件生成并实时处理FY4A成像仪数据文件生成截取区域格点数据文件的请求。</summary>
        private void NewFilesMonitor_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.Color cSalmon = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("Salmon");
            fbd.SelectedPath = curDir;
            fbd.Description = "请指明新生成FY4数据文件的路径在哪儿？请选择！";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fsw.Path = fbd.SelectedPath;
                fsw.IncludeSubdirectories = true;
                fsw.Filter = "*.HDF";

                Mouse.OverrideCursor = Cursors.Wait;
                btnSaveArea.IsEnabled = false;
                btnLoadFile.IsEnabled = false;
                BatchFilesProc.IsEnabled = false;
                NewFilesMonitor.IsEnabled = false;
                StopTheProc.IsEnabled = true;
                menuBat.Background = new SolidColorBrush(cSalmon);
                mytimer.Start();            //启动新文件监视的同时，启动时钟中断，以便定时清理过期的本地存储的处理后数据文件。
                
                bAutoProcessState = true;
                fsw.EnableRaisingEvents = true;

            }
            else
            {
                txtDisp.Text = "没有选定FY4成像仪数据新生成文件的路径，退出！！！\r\n" + txtDisp.Text;
                return;
            }

        }

        /// <summary>处理用户中断正在进行的批量处理FY4A成像仪数据文件的请求或者是实时监视并处理新文件的请求。</summary>
        private void StopTheProc_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.Color cLightGreen = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("LightGreen");

            if (!usrBreakBatchProc)
            {
                usrBreakBatchProc = true;
                StopTheProc.IsEnabled = false;
            }

            if (bAutoProcessState)
            {
                fsw.EnableRaisingEvents = false;
                bAutoProcessState = false;
                mytimer.Stop();            //终止新文件监视的同时，终止时钟中断，无需清理过期的本地存储的处理后数据文件。

                Mouse.OverrideCursor = Cursors.Arrow;
                btnSaveArea.IsEnabled = true;
                btnLoadFile.IsEnabled = true;
                BatchFilesProc.IsEnabled = true;
                NewFilesMonitor.IsEnabled = true;
                StopTheProc.IsEnabled = false;
                menuBat.Background = new SolidColorBrush(cLightGreen);

                txtDisp.Text = "用户中断了已启动的实时新文件监视处理过程！\r\n" + txtDisp.Text;
                return;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>这是新文件生成触发了处理新文件的程序。该程序处理当前新产生的数据文件。</summary>
        private void fsw_Created(object sender, FileSystemEventArgs e)
        {
            Match matchFy4fg;        //定义文件名匹配
            bool bmatch = false;
            curSourceFile = e.FullPath;
            matchFy4fg = regFileName.Match(curSourceFile);
            bmatch = matchFy4fg.Success;

            if (bmatch)
            {
                theFile = (string)curSourceFile.Clone();
                string sFswFileName = theFile;

//                new Thread(() =>
//                {
//                    this.Dispatcher.BeginInvoke(new Action(() =>
//                    {

                        SubRoutineOfFileWatcher(sFswFileName);

//                    }));
//                }).Start();

            }
            else
            {
                strCurStatus = "侦测到新HDF文件，但文件名与实时处理要求不匹配！！！";
                dispStr.txtLongStr = strCurStatus + "\r\n" + dispStr.txtLongStr;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return;
        }

        /// <summary>
        /// 设置的内部子程序，减轻事件处理主程序的体量。
        /// </summary>
        /// <param name="sFswFileName">要处理的文件名的副本</param>
        private void SubRoutineOfFileWatcher(string sFswFileName)
        {
            strCurStatus = "";          //准备读入新文件了，FY4数据类的状态清零以准备反映新的状态
            satNameetc = "";            //卫星名称、仪器名称等也清零，准备装入新的.
            NOMChannels = 0;            //新文件中包含的光谱通道数也预设为0

            Thread.Sleep(2000);             //这里等一/半秒，从侦测到文件，到文件形成应该会有一定延迟，等一下，避免意外。
            try
            {   //以下这一句话实例化了FY4卫星数据类，是全部程序的核心。它屏蔽了FY4数据复杂的结构，使你可以集中精力在具体专业问题上。
                fy4MajorDat = new Fy4HDF5Dat.Fy4HDF5Dat(theFile);
                if (fy4MajorDat.strStatus.IndexOf(@"Invalid Filename") > 0)
                {
                    strCurStatus = "监视到的新文件因有问题未进行处理！！！\r\n";
                    new Thread(() =>
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            dispStr.txtLongStr = strCurStatus + "\r\n" + dispStr.txtLongStr;
                        }));
                    }).Start();
                    return;
                }       //如果实例化不成功，设置状态信息退出。
            }
            catch (Exception ecpt)
            {
                strCurStatus = ecpt.Message + "     监视到的新文件因有问题未进行处理！！！";
            }
            finally
            {
                
            }
            if (strCurStatus != "")
            {
                new Thread(() =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        dispStr.txtLongStr = strCurStatus + "\r\n" + dispStr.txtLongStr;
                    }));
                }).Start();
                return;
            }               //上面这几句话都是处理实例化意外不成功的情况。
            for (int iy = 0; iy < fy4MajorDat.datSetsNames.Length; iy++)
            {
                dispStr.txtLongStr = fy4MajorDat.datSetsNames[iy] + "\r\n   [" + iy.ToString() + "]----[" + fy4MajorDat.datRow[iy].ToString() + ","
                    + fy4MajorDat.datColumn[iy].ToString() + "]\r\n" + dispStr.txtLongStr;
            }       //这个循环将实例化后的FY4数据类的属性作为文件信息显示在承担信息输出的文本框里。

            string strDTtmp = "";
            foreach (Fy4HDF5Dat.CbBoxContent attrTmp in fy4MajorDat.cbAttributeLst)
            {
                if ((attrTmp.cbname == "Satellite Name") || (attrTmp.cbname == "Sensor Name") || (attrTmp.cbname == "ProductID"))
                {
                    satNameetc += attrTmp.cbname.Replace("\0", " ") + "：\r\n   " + attrTmp.cbvalue.Replace("\0", " ") + "\r\n";
                }
                else if ((attrTmp.cbname == "Observing Ending Date") || (attrTmp.cbname == "Observing Ending Time"))
                {
                    satNameetc += attrTmp.cbname.Replace("\0", " ") + "：\r\n   " + attrTmp.cbvalue.Replace("\0", " ") + "\r\n";
                    strDTtmp += attrTmp.cbvalue.Replace("\0", " ");
                }
                else if ((attrTmp.cbname == "dSteppingAngle"))
                {
                    satNameetc += attrTmp.cbname.Replace("\0", " ") + "：\r\n" + attrTmp.cbvalue.Replace("\0", " ") + "\r\n";
                    steppingAngle = Convert.ToInt32(attrTmp.cbvalue);
                }
            }
            dtObsEnd = Convert.ToDateTime(strDTtmp);

            int imgNum = 0;
            for (int ix = 0; ix < fy4MajorDat.datSetsNum; ix++)
            {
                if (fy4MajorDat.datSetsNames[ix].Contains("NOMChannel")) imgNum++;
            }           //这个循环遍历了FY4数据文件中包含的数据集名称，并确定其中包含有几个光谱通道的数据。
            NOMChannels = imgNum;
            if (imgNum == 0)
            {
                strCurStatus = "监视到的新文件里不包括FY4成像仪数据集,未作处理，退出！！！\r\n";
                new Thread(() =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        dispStr.txtLongStr = strCurStatus + "\r\n" + dispStr.txtLongStr;
                    }));
                }).Start();
                return;
            }
            string[] saveFilenames = new string[NOMChannels];
            string[] bmpFilenames = new string[NOMChannels];

            double dy1 = iLn / 1000.0;
            double dx1 = iLw / 1000.0;
            double dy2 = iLs / 1000.0;
            double dx2 = iLe / 1000.0;
            double dxStep = iStep / 1000.0;
            double dyStep = dxStep;
            int iLevel = 2;
            CommonGridDat.CommonGridDat theGridDats;
            string strTmp;
            int[,] viewGrayDats;
            float[,] viewValueDats;
            string thePathTmp = "";
            string bmpPathTmp = "";

            string thePath = svDataPath;           //存储数据的路径，等经纬数据将分类存此路径下。
            if (!Directory.Exists(thePath))
            {
                Directory.CreateDirectory(thePath);
            }
            string bmpPath = svBitmapPath;           //存储图片的路径，bitmap图片将分类存此路径下。
            if (!Directory.Exists(bmpPath))
            {
                Directory.CreateDirectory(bmpPath);
            }

            double theStepX = dxStep;
            double theStepY = dyStep;
            if (multiresolutionID)
            {                       //成像仪步进视角与经纬步长取值换算为此。考虑到取值在整数度内取整，避过0.04。
                theStepX = (double)steppingAngle / 2800.0;          //0.28 / steppingAngle;
                theStepX = ((theStepX > 0.02) && (theStepX < 0.06)) ? 0.05 : theStepX;
                if (NOMChannels > 10)
                    theStepX = 0.05;
                else if (NOMChannels > 4)
                    theStepX = 0.02;
                else if (NOMChannels > 1)
                    theStepX = 0.01;
                else if (NOMChannels == 1)           //加这一段的原因是steppingAngle的定义竟然随意变动！卫星中心真是太不靠谱！
                    theStepX = 0.005;
                else
                    theStepX = 0.05;
                theStepY = theStepX;
            }
            int theStepTmp = (int)(theStepX * 1000);
            string strStep = "0" + theStepTmp.ToString();        //设这个字符串是要在文件名里加入步长因素，有利于后期文件性质识别。
            strStep = "-" + strStep.Substring(strStep.Length - 2);

            theGridDats = new CommonGridDat.CommonGridDat(ref dy1, ref dy2, ref dx1, ref dx2, ref theStepX, ref theStepY, ref iLevel);
            theGridDats.gDataDiscr[0].DYear = dtObsEnd.Year;
            theGridDats.gDataDiscr[1].DYear = dtObsEnd.Year;
            theGridDats.gDataDiscr[0].DMounth = dtObsEnd.Month;
            theGridDats.gDataDiscr[1].DMounth = dtObsEnd.Month;
            theGridDats.gDataDiscr[0].DDay = dtObsEnd.Day;
            theGridDats.gDataDiscr[1].DDay = dtObsEnd.Day;
            theGridDats.gDataDiscr[0].DTime = dtObsEnd.Hour;
            theGridDats.gDataDiscr[1].DTime = dtObsEnd.Hour;
            theGridDats.gDataDiscr[0].DMinute = dtObsEnd.Minute;
            theGridDats.gDataDiscr[1].DMinute = dtObsEnd.Minute;
            theGridDats.gDataDiscr[0].DSecond = dtObsEnd.Second;
            theGridDats.gDataDiscr[1].DSecond = dtObsEnd.Second;

            int bmpNum = NOMChannels + 1;
            theBitmaps = new Bitmap[bmpNum];    //有几个通道则准备几个图像对象和内存池，以备用。
            theBitmapStreams = new MemoryStream[bmpNum];
            thebmpSources = new ImageSource[bmpNum];
            theimgSourCons = new ImageSourceConverter[bmpNum];

            string fImgName = "";
            switch (NOMChannels)
            {
                case 1:
                    fImgName = "Channel02"; //如果只包含一个通道，则选取这个通道的数据作为全区域显示。
                    break;
                case 3:
                    fImgName = "Channel03"; //如果有3个通道，则选取3通道数据作为全区域显示。
                    break;
                case 7:
                    fImgName = "Channel06"; //如果包含7个通道，则选取6通道数据作为全区域显示。
                    break;
                case 14:
                    fImgName = "Channel14"; //如果是14个通道，则选取14通道数据作全区域显示。
                    break;
                default:
                    fImgName = "";
                    break;
            }
            try
            {   //调用FY4数据类的类方法获取其全区域图像，一句话解决问题！
                fy4MajorDat.GetaChannelWholeImg(ref fImgName, out theBitmaps[0]);
                if (theBitmaps[0] == null)
                {
                    strCurStatus = "实时处理新到文件中，获取其全幅图像失败！！！\r\n";
                    dispStr.txtLongStr = strCurStatus + "\r\n" + dispStr.txtLongStr;
                    return;
                }
                theBitmapStreams[0] = new MemoryStream();
                theimgSourCons[0] = new ImageSourceConverter();
                theBitmaps[0].Save(theBitmapStreams[0], System.Drawing.Imaging.ImageFormat.Png);
                thebmpSources[0] = (ImageSource)theimgSourCons[0].ConvertFrom(theBitmapStreams[0]);
                new Thread(() =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        imgGrid00.Source = thebmpSources[0];    //在界面输出获得的图像。
                    }));
                }).Start();
            }
            catch (Exception ecpt)
            {
                strCurStatus = ecpt.Message + "  实时处理新到文件过程中，显示图像失败！！！";
                dispStr.txtLongStr = strCurStatus + "\r\n" + dispStr.txtLongStr;
                return;         //处理调用类方法和图像操作中的意外。
            }

            for (int iz = 0; iz < NOMChannels; iz++)
            {
                int izp1 = iz + 1;
                strTmp = (NOMChannels == 1) ? "02" : "0" + izp1.ToString();
                strTmp = strTmp.Substring(strTmp.Length - 2);
                strTmp = "Channel" + strTmp;

                thePathTmp = thePath + strTmp + "\\" + theStepTmp.ToString() + "\\";
                bmpPathTmp = bmpPath + strTmp + "\\" + theStepTmp.ToString() + "\\";
                if (!Directory.Exists(thePathTmp))
                {
                    Directory.CreateDirectory(thePathTmp);
                }                                           //确认了等经纬数据存储路径。
                saveFilenames[iz] = thePathTmp + strTmp + dtObsEnd.ToString("yyyyMMddHHmmss") + strStep + ".Grd";
                if (!Directory.Exists(bmpPathTmp))
                {
                    Directory.CreateDirectory(bmpPathTmp);
                }                                           //确认了图片文件存储路径。
                bmpFilenames[iz] = bmpPathTmp + strTmp + dtObsEnd.ToString("yyyyMMddHHmmss") + strStep + ".Bmp";

                theGridDats.gDataDiscr[0].DataDiscr = strTmp + "-GrayDatas";
                theGridDats.gDataDiscr[1].DataDiscr = strTmp + "-ValueDatas";

                try
                {
                    fy4MajorDat.GetaChannelAreaData(ref strTmp, ref iLn, ref iLw, ref iLs, ref iLe, ref theStepTmp,
                        out viewGrayDats, out viewValueDats);
                }
                catch (Exception ecpt)
                {
                    strCurStatus = ecpt.Message + "  实时处理新到文件中调取" + strTmp + "通道数据时意外失败！！！";
                    new Thread(() =>
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            dispStr.txtLongStr = "\r\n" + strCurStatus + "\r\n" + dispStr.txtLongStr;
                        }));
                    }).Start();
                    return;         //处理调用类方法或数据操作中的意外。
                }
                for (int iy = 0; iy < theGridDats.iPixY; iy++)
                {
                    for (int ix = 0; ix < theGridDats.iPixX; ix++)
                    {
                        theGridDats.iLLGrid[iy, ix, 0] = viewGrayDats[iy, ix];
                        theGridDats.iLLGrid[iy, ix, 1] = (int)(viewValueDats[iy, ix] * 1000 + 0.5);
                    }
                }
                theGridDats.LookForStatistic();
                int levelTmp = 0;
                theGridDats.MakeTheLevelImage(ref levelTmp, out theBitmaps[izp1]);

                try
                {
                    IFormatter theDatFormat = new BinaryFormatter();
                    Stream theDatstream = new FileStream(saveFilenames[iz], FileMode.Create, FileAccess.Write, FileShare.None);
                    theDatFormat.Serialize(theDatstream, theGridDats);
                    theDatstream.Close();       //保存当前数据文件。
                                                //对应上面的数据，保存了相对应的每一个经纬数据的图像到图像文件。
                    theBitmaps[izp1].Save(bmpFilenames[iz], System.Drawing.Imaging.ImageFormat.Bmp);
                                                //输出图像到界面，更新界面显示为最新通道数据。
                    theBitmapStreams[izp1] = new MemoryStream();
                    theimgSourCons[izp1] = new ImageSourceConverter();
                    theBitmaps[izp1].Save(theBitmapStreams[izp1], System.Drawing.Imaging.ImageFormat.Png);
                    thebmpSources[izp1] = (ImageSource)theimgSourCons[izp1].ConvertFrom(theBitmapStreams[izp1]);
                    switch (izp1)
                    {           //输出获得的图像到界面。
                        case 1:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid01.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 2:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid02.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 3:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid03.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 4:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid04.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 5:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid05.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 6:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid06.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 7:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid07.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 8:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid08.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 9:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid09.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 10:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid10.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 11:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid11.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 12:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid12.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 13:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid13.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                        case 14:
                            new Thread(() =>
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    imgGrid14.Source = thebmpSources[izp1];
                                }));
                            }).Start();
                            break;
                    }

                }
                catch (Exception ecpt)
                {
                    new Thread(() =>
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            dispStr.txtLongStr = "实时处理新到文件时，数据文件创建出错。错误码：" + ecpt.Message + "\r\n" + dispStr.txtLongStr;
                        }));
                    }).Start();
                    return;
                }

            }

            strCurStatus = "本次监控到的新文件处理过程中，按用户要求全部成功！！！";
            dispStr.txtLongStr = strCurStatus + "\r\n" + dispStr.txtLongStr;
            return;
        }

        /// <summary>
        /// 按照时钟触发执行过期文件识别并清除，以腾出存储空间。
        /// </summary>
        private void CheckFilesRetain (object sender, EventArgs e)
        {
            int theRemainDays = iFileRetained;

            string theFolderName = svDataPath;
            ScanFolder(theFolderName, theRemainDays);       //清除数据文件存储中的过期文件。

            theFolderName = svBitmapPath;
            ScanFolder(theFolderName, theRemainDays);       //清除图像文件存储中的过期文件。

            int lengthTxt = dispStr.txtLongStr.Length;
            if (lengthTxt > 50000) dispStr.txtLongStr = dispStr.txtLongStr.Remove(50000);   //顺便瘦身显示字符串，以免无限制扩张。
        }

        /// <summary>
        /// 这是一个递归遍历文件夹下所有文件，并检查其是否为过期文件，如果是则删除之，以维持合适的磁盘空间给新文件。
        /// </summary>
        /// <param name="nameFolder">被检查的文件夹名称</param>
        /// <param name="fileRetain">设定的过期文件的判别天数，即多少天以前文件为过期。</param>
        private void ScanFolder(string nameFolder, int fileRetain)
        {
            DirectoryInfo thefolder = new DirectoryInfo(nameFolder);
            foreach (DirectoryInfo thenextfolder in thefolder.GetDirectories())
            {
                this.ScanFolder(thenextfolder.FullName, fileRetain);
            }

            DateTime dtTmp = DateTime.Now;
            int delFilesNum = 0;
            foreach (FileInfo thefile in thefolder.GetFiles())
            {
                TimeSpan tsTmp = new TimeSpan(0, 0, 0, 0);
                tsTmp = dtTmp.Subtract(thefile.CreationTime);
                if (tsTmp.Days > fileRetain)
                {
                    try
                    {
                        thefile.Delete();
                        delFilesNum++;
                    }
                    catch (Exception ecpt)
                    {
                        dispStr.txtLongStr = "清除过期文件" + thefile.Name + "时发生意外：" + ecpt.Message + "\r\n" + dispStr.txtLongStr;
                    }
                }
            }
            if (delFilesNum > 0)
                dispStr.txtLongStr = "成功清除了过期文件" + delFilesNum.ToString() + "个！" + "\r\n" + dispStr.txtLongStr;

        }

    }

    /// <summary>
    /// 定义一个窗口之间传递参数的事件参数类，以便窗口之间传递参数。
    /// </summary>
    public class PassValuesEventArgs : EventArgs
    {
        public TransParamToSon viewParam { get; internal set; }
        public bool theSonIsOk { get; internal set; }
        public PassValuesEventArgs(TransParamToSon viewParam, bool isOK)
        {
            this.viewParam = viewParam;
            this.theSonIsOk = isOK;
        }
    }

    /// <summary>
    /// 我把主窗口要传递给子窗口的所有参数打成一个包，给这个包定义一个类，用时实例化之。
    /// </summary>
    public class TransParamToSon
    {
        public Bitmap viewMap;          //传给子窗口的图像
        public int[,] viewGrayDats;     //传给子窗口图像对应的真实灰度值
        public float[,] viewValueDats;  //传给子窗口图像对应的物理数据
        public int leftUpY;             //传给子窗口图像左上角点对应的坐标Y值(扫描线编号或纬度值)
        public int leftUpX;             //传给子窗口图像左上角点对应的坐标X值(扫描像素编号或经度值)
        public int rightDownY;          //传给子窗口图像右下角点对应的坐标Y值(扫描线编号或纬度值)
        public int rightDownX;          //传给子窗口图像右下角点对应的坐标X值(扫描像素编号或经度值)
        public int oneStep;             //传给子窗口图像像素间坐标格距(标称投影时为扫描线像素间隔固定为1，等经纬投影为经纬步长)
        public string discribeStr;      //描述传递给子窗口图像的相关参数。
        public string dataDir;          //数据文件存储路径传给子窗口，因为子窗口有存储文件操作。
        public string bitmapDir;        //图片文件存储路径传给子窗口，因为子窗口有存储图片文件的操作。
        public TransParamToSon(int leftUpY, int leftUpX, int rightDownY, int rightDownX, int oneStep, int[,] viewGrayDats, 
            float[,] viewValueDats, Bitmap viewMap, string discribeStr,string dataDir,string bitmapDir)
        {
            this.leftUpY = leftUpY;
            this.leftUpX = leftUpX;
            this.rightDownY = rightDownY;
            this.rightDownX = rightDownX;
            this.oneStep = oneStep;
            this.viewGrayDats = viewGrayDats;
            this.viewValueDats = viewValueDats;
            this.viewMap = viewMap;
            this.discribeStr = discribeStr;
            this.dataDir = dataDir;
            this.bitmapDir = bitmapDir;
        }
    }

    /// <summary>
    /// 设定一个字符串类的变更通知触发，用于与显示控件绑定。
    /// </summary>
    class ViewTxt : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string txtStr;

        public string txtLongStr
        {
            get { return txtStr; }
            set
            {
                txtStr = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("txtLongStr"));
                }
            }
        }
    }

    /// <summary>
    /// 这里设计了一个类似WindowsForm里的DoEvents的类，用于批量处理文件时避免界面假死机。
    /// </summary>
    public class DispatcherHelper
    {
        /// <summary>
        /// Simulate Application.DoEvents function of <see cref=" System.Windows.Forms.Application"/> class.
        /// </summary>
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrames), frame);

            try
            {
                Dispatcher.PushFrame(frame);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static object ExitFrames(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }
    }

}

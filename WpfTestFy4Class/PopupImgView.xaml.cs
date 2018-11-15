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
using System.Windows.Shapes;

using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace WpfTestFy4Class
{
    /// <summary>
    /// PopupImgView.xaml 的交互逻辑
    /// </summary>
    public partial class PopupImgView : Window
    {
        /// <summary>定义这个委托传回弹出窗返回主窗口的参数及事件</summary>
        public delegate void PassValuesHandler(object sender, PassValuesEventArgs e);
        /// <summary>传回主窗口事件</summary>
        public event PassValuesHandler PassOKEvent;
        /// <summary>接收主窗口传给弹出窗的参数，这个参数可是包罗万象的了，把重要的操作对象都传过来了，见主窗口的专门的对象定义。</summary>
        public TransParamToSon acquireParam;
        /// <summary>这个bool量记录本弹出窗工作是否正常，并将之传回主窗口，以便主窗口掌握弹出窗的是否安全。</summary>
        public bool rtnArg;
        /// <summary>为本弹出窗图像显示定义的流</summary>
        MemoryStream sonBitmapStream;
        /// <summary>为本弹出窗图像显示定义的图像资源</summary>
        ImageSource sonBitmapSource;
        /// <summary>为本弹出窗图像显示定义的图像资源</summary>
        ImageSourceConverter sonImgSourCon;
        /// <summary>记录界面鼠标是否按下，平移图像时用</summary>
        private bool mouseDown;
        /// <summary>记录鼠标左键按下时的坐标</summary>
        private System.Windows.Point mouseXY;
        /// <summary>记录鼠标右键按下时的坐标</summary>
        private System.Windows.Point msRightXY;
        /// <summary>定义一个与Image ToolTip绑定的文本。</summary>
        ViewTxt strImgTips;
        /// <summary>定义一个与Image ToolTip的绑定</summary>
        System.Windows.Data.Binding imgTipBindings;
        /// <summary>用于选择存储数据文件的路径</summary>
        public static Microsoft.Win32.SaveFileDialog svfd = new Microsoft.Win32.SaveFileDialog();
        /// <summary>记录当前环境目录</summary>
        private string curDir;

        /// <summary>
        /// 接收主窗体传来的参数并弹出这个辅助窗口进行针对选定图像更详细图像操作
        /// </summary>
        /// <param name="passParam">主窗口传递过来的参数</param>
        public PopupImgView(TransParamToSon passParam)
        {
            acquireParam = passParam;       //用一个传递参数的类实例变量先把这个传递过来的参数接下来，以便于操作。
            InitializeComponent();

            sonBitmapStream = new MemoryStream();
            sonImgSourCon = new ImageSourceConverter();
            acquireParam.viewMap.Save(sonBitmapStream, System.Drawing.Imaging.ImageFormat.Png);     //将首传过来的图像显示到这个弹出辅助窗口中
            sonBitmapSource = (ImageSource)sonImgSourCon.ConvertFrom(sonBitmapStream);
            popImg0.Source = sonBitmapSource;
            txtDiscribe.Text = acquireParam.discribeStr;

            strImgTips = new ViewTxt();
            imgTipBindings = new System.Windows.Data.Binding();
            imgTipBindings.Source = strImgTips;                     //定义好这个窗口图像控件的ToolTip字符串，以便后用。
            imgTipBindings.Path = new PropertyPath("txtLongStr");
            BindingOperations.SetBinding(this.BackFrame, System.Windows.Controls.Image.ToolTipProperty, imgTipBindings);

            if (acquireParam.oneStep == 1)
            {
                btnSaveTo.IsEnabled = false;
            }
            else
            {
                btnSaveTo.IsEnabled = true; ;
            }

            curDir = Environment.CurrentDirectory;
        }

        /// <summary>
        /// 给显示图像加上网格线，主要是为了显示经纬线，对数据域图是标称投影网格线。为了避免容器与图像坐标的换算麻烦，直接加到了图像上，似不好。先这样吧。
        /// </summary>
        private void grdLine_Click(object sender, RoutedEventArgs e)
        {
            bool bChoice = (bool)grdLine.IsChecked;
            System.Drawing.Color coPix;
            byte bpix;
            int ixBegin = acquireParam.leftUpX;
            int iyBegin = acquireParam.leftUpY;
            int ixEnd = acquireParam.rightDownX;
            int iyEnd = acquireParam.rightDownY;
            int bmpHeight = acquireParam.viewMap.Height;
            int bmpWidth = acquireParam.viewMap.Width;
            int iTmpX = 0;
            int iTmpY = 0;
            bool bGridPt = false;
            int coeffLine = (acquireParam.oneStep > 5) ? 1000 : 200;
            for (int iy = 0; iy < bmpHeight; iy += 2)
            {
                iTmpY = iyBegin + iy * acquireParam.oneStep;
                for (int ix = 0; ix < bmpWidth; ix +=2)
                {
                    iTmpX = ixBegin + ix * acquireParam.oneStep;
                    bGridPt = ((iTmpY / coeffLine) * coeffLine == iTmpY) || ((iTmpX / coeffLine) * coeffLine == iTmpX);
                    if (bGridPt)
                    {
                        coPix = acquireParam.viewMap.GetPixel(ix, iy);
                        bpix = coPix.R;
                        if (bChoice)
                            coPix = System.Drawing.Color.FromArgb(bpix, 255 - bpix, 0);
                        else
                            coPix = System.Drawing.Color.FromArgb(bpix, bpix, bpix);
                        acquireParam.viewMap.SetPixel(ix, iy, coPix);
                    }
                }
            }

            sonBitmapStream = new MemoryStream();
            sonImgSourCon = new ImageSourceConverter();
            acquireParam.viewMap.Save(sonBitmapStream, System.Drawing.Imaging.ImageFormat.Png);
            sonBitmapSource = (ImageSource)sonImgSourCon.ConvertFrom(sonBitmapStream);
            popImg0.Source = sonBitmapSource;

        }

        /// <summary>
        /// 关闭这个弹出的辅助窗口。
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            rtnArg = true;
            PassValuesEventArgs args = new PassValuesEventArgs(acquireParam,rtnArg);
            PassOKEvent(this, args);

        }

        /// <summary>
        /// 图像容器的左击鼠标事件，用于处理图像在容器中的移动始。
        /// </summary>
        private void BackFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Shapes.Rectangle img = sender as System.Windows.Shapes.Rectangle;
            if (img == null)
            {
                return;
            }
            img.CaptureMouse();
            mouseDown = true;
            mouseXY = e.GetPosition(img);
        }

        /// <summary>
        /// 图像容器的左击鼠标事件，用于处理图像在容器中的移动终。
        /// </summary>
        private void BackFrame_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Shapes.Rectangle img = sender as System.Windows.Shapes.Rectangle;
            if (img == null)
            {
                return;
            }
            img.ReleaseMouseCapture();
            mouseDown = false;

            popImgLL.Visibility = System.Windows.Visibility.Hidden;     //如果左键鼠标启动，则停止右键提取显示数据的操作。
            strImgTips.txtLongStr = "";
        }

        /// <summary>
        /// 处理用于处理图像在容器中的移动。
        /// </summary>
        private void BackFrame_MouseMove(object sender, MouseEventArgs e)
        {
            System.Windows.Shapes.Rectangle img = sender as System.Windows.Shapes.Rectangle;
            if (img == null)
            {
                return;
            }
            if (mouseDown)
            {
                Domousemove(img, e);
            }
        }

        /// <summary>
        /// 是处理图像在容器中的移动的子程序，即：专门配合BackFrame_MouseMove(.....)
        /// </summary>
        /// <param name="img">传递过来的容器控件</param>
        /// <param name="e">传递过来的鼠标参数</param>
        private void Domousemove(System.Windows.Shapes.Rectangle img, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            TransformGroup group = popGrid0.FindResource("ImgMagTrans") as TransformGroup;
            //Debug.Assert(group != null);
            TranslateTransform transform = group.Children[1] as TranslateTransform;
            System.Windows.Point position = e.GetPosition(img);
            ScaleTransform transform0 = group.Children[0] as ScaleTransform;
            double xScale = transform0.ScaleX;
            double parentH = img.ActualHeight;
            double parentW = img.ActualWidth;
            double yCount = parentH * xScale;
            double xCount = parentW * xScale;
            double xRight = parentW - xCount;
            double yBottom = parentH - yCount;
            double diffX = position.X - mouseXY.X;
            double diffY = position.Y - mouseXY.Y;
            if (((transform.X + diffX) < xRight) || ((transform.X + diffX) > 0))
                transform.X += 0;
            else 
                transform.X += diffX;              // m_PreviousMousePoint.X;

            if (((transform.Y + diffY) < yBottom) || ((transform.Y + diffY) > 0))
                transform.Y += 0;
            else
                transform.Y += diffY;              // m_PreviousMousePoint.Y;
            mouseXY = position;

        }

        /// <summary>
        /// 图像容器的鼠标滚轴事件，用于处理图像在容器中的放大和缩小。
        /// </summary>
        /// <param name="sender">图像容器控件</param>
        /// <param name="e">图像容器鼠标事件参数</param>
        private void BackFrame_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Windows.Shapes.Rectangle img = sender as System.Windows.Shapes.Rectangle;
            if (img == null)
            {
                return;
            }
            System.Windows.Point point = e.GetPosition(img);
            TransformGroup group = popGrid0.FindResource("ImgMagTrans") as TransformGroup;      //放大缩小函数
            double delta = e.Delta * 0.001;        //传递过来的鼠标滚轴系数，由于太大，缩小其千分之一为放大倍数。
            DowheelZoom(group, point, delta);   //具体缩放操作，调用子程序。

            DoCorrectZoom(img, e);      //这个子程序主要是修正因放大缩小过程造成图像收进容器以内的情况，由于限定图像缩小最小为1，也即初始填充容器的尺度。
        }

        /// <summary>
        /// 用于处理图像在容器中的放大和缩小的具体操作的子程序，即：与BackFrame_MouseWheel协同的子程序。
        /// </summary>
        private void DowheelZoom(TransformGroup group, System.Windows.Point point, double delta)
        {
            System.Windows.Point pointToContent = group.Inverse.Transform(point);
            ScaleTransform transform = group.Children[0] as ScaleTransform;
            if ((transform.ScaleX + delta < 1) || (transform.ScaleX + delta > 10)) return;
            transform.ScaleX += delta;
            transform.ScaleY += delta;
            TranslateTransform transform1 = group.Children[1] as TranslateTransform;
            transform1.X = -1 * ((pointToContent.X * transform.ScaleX) - point.X);
            transform1.Y = -1 * ((pointToContent.Y * transform.ScaleY) - point.Y);
        }

        /// <summary>
        /// 用于处理图像在放大和缩小后，图像与容器可能出现的偏差的子程序，即：与BackFrame_MouseWheel协同的子程序。
        /// </summary>
        private void DoCorrectZoom(System.Windows.Shapes.Rectangle img, MouseEventArgs e)
        {
            TransformGroup group = popGrid0.FindResource("ImgMagTrans") as TransformGroup;
            TranslateTransform transform = group.Children[1] as TranslateTransform;
            var transform0 = group.Children[0] as ScaleTransform;
            double xScale = transform0.ScaleX;
            double parentH = img.ActualHeight;
            double parentW = img.ActualWidth;       //容器的实际尺寸。
            double yCount = parentH * xScale;
            double xCount = parentW * xScale;       //图像缩放后的尺寸。
            double xRight = parentW - xCount;
            double yBottom = parentH - yCount;      //图像、容器右、下边界重合时的最大坐标差。左、上的最大坐标差当然是0。
            double diffX = transform.X;
            double diffY = transform.Y;
            //以下判断图像与容器是否出现了左、上边界移入容器内，或者右、下边界移入容器内的情况，如有，则移动使边界合一。基本依据是图像缩小系数最小为1。
            if (diffX > 0)
                transform.X -= diffX;
            else if (diffX < xRight)
                transform.X -= (diffX - xRight);
            if (diffY > 0)
                transform.Y -= diffY;
            else if (diffY < yBottom)
                transform.Y -= (diffY - yBottom);
        }

        /// <summary>
        /// 图像容器的右击鼠标事件，用于显示所选点的具体参数值。
        /// </summary>
        private void BackFrame_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Shapes.Rectangle img = sender as System.Windows.Shapes.Rectangle;
            msRightXY = e.GetPosition(img);
            datEllPt.Center = new System.Windows.Point(msRightXY.X, msRightXY.Y);
            popImgLL.Visibility = System.Windows.Visibility.Visible;

            TransformGroup group = popGrid0.FindResource("ImgMagTrans") as TransformGroup;      //放大缩小函数
            TranslateTransform transform = group.Children[1] as TranslateTransform;
            ScaleTransform transform0 = group.Children[0] as ScaleTransform;
            double xScale = transform0.ScaleX;
            double parentH = img.ActualHeight;
            double parentW = img.ActualWidth;
            double originalH = acquireParam.viewMap.Height;
            double originalW = acquireParam.viewMap.Width;
            System.Windows.Point origPoint = group.Inverse.Transform(msRightXY);
            double coeffH = originalH / parentH;
            double coeffW = originalW / parentW;
            int colNum = (int)(origPoint.X * coeffW);
            double lonTmp = (acquireParam.oneStep > 1) ? (colNum * acquireParam.oneStep + acquireParam.leftUpX) / 1000.0 : colNum;
            int rowNum = (int)(origPoint.Y * coeffH );
            double latTmp = (acquireParam.oneStep > 1) ? (acquireParam.leftUpY - rowNum * acquireParam.oneStep) / 1000.0 : rowNum;
            strImgTips.txtLongStr = "纬/行：" + latTmp.ToString() + "  经/列：" + lonTmp.ToString() + "  灰度：" + acquireParam.viewGrayDats[rowNum, colNum].ToString()
                + "   参数：" + acquireParam.viewValueDats[rowNum, colNum].ToString();

        }

        /// <summary>
        /// 结束返回弹出辅助窗口的操作。
        /// </summary>
        private void btnReturn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 将当前显示操作的数据以经纬格点数据类格式文件存盘。
        /// </summary>
        private void btnSaveTo_Click(object sender, RoutedEventArgs e)
        {
            double dy1 = acquireParam.leftUpY / 1000.0;
            double dx1 = acquireParam.leftUpX / 1000.0;
            double dy2 = acquireParam.rightDownY / 1000.0;
            double dx2 = acquireParam.rightDownX / 1000.0;
            double dxStep = acquireParam.oneStep / 1000.0;
            double dyStep = dxStep;
            int iLevel = 2;
            CommonGridDat.CommonGridDat theGridDats = new CommonGridDat.CommonGridDat(ref dy1, ref dy2, ref dx1, ref dx2, ref dxStep, ref dyStep, ref iLevel);

            Regex regTmp = new Regex(@"(Channel)\d{2}");
            string strTmp = regTmp.Match(acquireParam.discribeStr).Value;
            theGridDats.gDataDiscr[0].DataDiscr = strTmp + "-GrayDatas";
            theGridDats.gDataDiscr[1].DataDiscr = strTmp + "-ValueDatas";
            string thePath = acquireParam.dataDir + strTmp + "\\" + acquireParam.oneStep.ToString() + "\\";
            string strStep = "0" + acquireParam.oneStep.ToString();        //设这个字符串是要在文件名里加入步长因素，有利于后期文件性质识别。
            strStep = "-" + strStep.Substring(strStep.Length - 2);
            string theFile = strTmp;
            regTmp = new Regex(@"\d{4}-\d{2}-\d{2}");
            strTmp = regTmp.Match(acquireParam.discribeStr).Value;
            regTmp = new Regex(@"\d{2}:\d{2}:\d{2}");
            strTmp += " " + regTmp.Match(acquireParam.discribeStr).Value;
            DateTime dtTmp = Convert.ToDateTime(strTmp);
            theGridDats.gDataDiscr[0].DYear = dtTmp.Year;
            theGridDats.gDataDiscr[1].DYear = dtTmp.Year;
            theGridDats.gDataDiscr[0].DMounth = dtTmp.Month;
            theGridDats.gDataDiscr[1].DMounth = dtTmp.Month;
            theGridDats.gDataDiscr[0].DDay = dtTmp.Day;
            theGridDats.gDataDiscr[1].DDay = dtTmp.Day;
            theGridDats.gDataDiscr[0].DTime = dtTmp.Hour;
            theGridDats.gDataDiscr[1].DTime = dtTmp.Hour;
            theGridDats.gDataDiscr[0].DMinute = dtTmp.Minute;
            theGridDats.gDataDiscr[1].DMinute = dtTmp.Minute;
            theGridDats.gDataDiscr[0].DSecond = dtTmp.Second;
            theGridDats.gDataDiscr[1].DSecond = dtTmp.Second;
            theFile += dtTmp.ToString("yyyyMMddHHmmss") + strStep;

            svfd.InitialDirectory = curDir;
            svfd.DefaultExt = "Grd";
            svfd.Filter = "Grid Files(*.GRD;*.grd)|*.GRD;*.grd|All Files(*.*)|*.*";
            svfd.Title = "这个过程需要一个经纬格点Grd文件名，它将显示图像及其对应数据存储为/创建为一个Grd格式的文件";
            if (!Directory.Exists(thePath))
            {
                Directory.CreateDirectory(thePath);
            }
            svfd.FileName = thePath + theFile;
            if (svfd.ShowDialog() == true)
            {
                theFile = svfd.FileName;
                if (theFile != "")
                {
                    for (int iy = 0; iy < theGridDats.iPixY; iy++)
                    {
                        for (int ix = 0; ix < theGridDats.iPixX; ix++)
                        {
                            theGridDats.iLLGrid[iy, ix, 0] = acquireParam.viewGrayDats[iy, ix];
                            theGridDats.iLLGrid[iy, ix, 1] = (int)(acquireParam.viewValueDats[iy, ix] * 1000 + 0.5);
                        }
                    }
                    theGridDats.LookForStatistic();
                }

                try
                {
                    IFormatter theDatFormat = new BinaryFormatter();
                    Stream theDatstream = new FileStream(theFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    theDatFormat.Serialize(theDatstream, theGridDats);
                    theDatstream.Close();       //保存当前数据文件。
                }
                catch (Exception ecpt)
                {
                    txtDiscribe.Text  += "数据文件创建出错。错误码：" + ecpt.Message + "\r\n";
                    return;
                }

            }

        }

        /// <summary>
        /// 双击图像框触发将图像框中的图像以BMP图片文件形式存储起来。
        /// </summary>
        private void BackFrame_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                Regex regTmp = new Regex(@"(Channel)\d{2}");
                string strTmp = regTmp.Match(acquireParam.discribeStr).Value;
                string thePath = acquireParam.bitmapDir + strTmp + "\\" + acquireParam.oneStep.ToString() + "\\";
                string strStep = "0" + acquireParam.oneStep.ToString();        //设这个字符串是要在文件名里加入步长因素，有利于后期文件性质识别。
                strStep = "-" + strStep.Substring(strStep.Length - 2);
                string theFile = strTmp;
                regTmp = new Regex(@"\d{4}-\d{2}-\d{2}");
                strTmp = regTmp.Match(acquireParam.discribeStr).Value;
                regTmp = new Regex(@"\d{2}:\d{2}:\d{2}");
                strTmp += " " + regTmp.Match(acquireParam.discribeStr).Value;
                DateTime dtTmp = Convert.ToDateTime(strTmp);
                theFile += dtTmp.ToString("yyyyMMddHHmmss") + strStep;

                svfd.Title = "将当前图像以BMP格式图片文件另存为......";
                svfd.Filter = "Image Files (*.bmp)|*.bmp | All Files | *.*";
                svfd.DefaultExt = "Bmp";
                if (!Directory.Exists(thePath))
                {
                    Directory.CreateDirectory(thePath);
                }
                svfd.RestoreDirectory = true;            //保存对话框是否记忆上次打开的目录
                svfd.FileName = thePath + theFile;
                if (svfd.ShowDialog() == true)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)this.popImg0.Source));
                    using (FileStream stream = new FileStream(svfd.FileName, FileMode.Create)) encoder.Save(stream);
                }
            }
        }

    }
}
